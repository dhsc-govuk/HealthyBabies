using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Organisations;
using LanguageExt;
using MediatR;

namespace Application.Submissions.Commands;

public record CheckOutcomeScoreDuplicatesQuery : IRequest<Either<string, List<OutcomeScoreDuplicateCheckDto>>>
{
    public Guid SubmissionId { get; init; }
    public Guid ModuleId { get; init; }
    public IReadOnlyList<OutcomeScoreRowCheckDto> Rows { get; init; } = Array.Empty<OutcomeScoreRowCheckDto>();
}

public class CheckOutcomeScoreDuplicatesQueryHandler(
    PermissionsService permissionsService,
    IDataCollectionFormModuleQueries formModuleQueries,
    IServiceQueries serviceQueries,
    IFormSubmissionRepository formSubmissionRepository)
    : IRequestHandler<CheckOutcomeScoreDuplicatesQuery, Either<string, List<OutcomeScoreDuplicateCheckDto>>>
{
    public async Task<Either<string, List<OutcomeScoreDuplicateCheckDto>>> Handle(
        CheckOutcomeScoreDuplicatesQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();

        return await permissions.MatchAsync(
            async p => await CheckDuplicates(p, request, cancellationToken),
            e => (Either<string, List<OutcomeScoreDuplicateCheckDto>>)e.Message);
    }

    private async Task<Either<string, List<OutcomeScoreDuplicateCheckDto>>> CheckDuplicates(
        Permission permission,
        CheckOutcomeScoreDuplicatesQuery request,
        CancellationToken cancellationToken)
    {
        var orgId = permission.OrganisationId.Match(id => id, () => throw new InvalidOperationException("Organisation ID required"));
        var dcId = new DataCollectionId(request.SubmissionId);
        var modId = new DataCollectionFormModuleId(request.ModuleId);

        // Load outcome scores module with its fields so we can normalise values for hashing.
        var outcomeScoresModuleOption = await formModuleQueries.GetByIdWithFieldsAsync(modId, cancellationToken);
        if (outcomeScoresModuleOption.IsNone)
        {
            return request.Rows
                .Select(r => new OutcomeScoreDuplicateCheckDto(r.RowIndex, r.ServiceName, false))
                .ToList();
        }

        var outcomeScoresModule = outcomeScoresModuleOption.Match(m => m, () => null!);
        var fields = outcomeScoresModule.Fields.ToList();

        // Resolve service names → IDs using only QSU13-eligible services.
        var eligibleServices = await GetEligibleServicesAsync(orgId, dcId, cancellationToken);
        var serviceNameToId = eligibleServices
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id.Value, StringComparer.OrdinalIgnoreCase);

        // Load existing outcome score records for all matched services.
        var matchedServiceIds = request.Rows
            .Select(r => r.ServiceName)
            .Where(name => serviceNameToId.ContainsKey(name))
            .Select(name => serviceNameToId[name])
            .Distinct()
            .ToList();

        // Build a hash-set of existing record hashes per service.
        var existingHashesByService = new Dictionary<Guid, System.Collections.Generic.HashSet<string>>();
        foreach (var serviceId in matchedServiceIds)
        {
            var existing = await formSubmissionRepository.GetAllOutcomeScoresByServiceAsync(
                modId, dcId, serviceId, cancellationToken);

            var hashes = existing
                .Select(s => ComputeSubmissionHash(s, fields))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            existingHashesByService[serviceId] = hashes;
        }

        // Per-row: compute the incoming row's hash and check whether it already exists.
        return request.Rows
            .Select(row =>
            {
                if (!serviceNameToId.TryGetValue(row.ServiceName, out var serviceId))
                {
                    return new OutcomeScoreDuplicateCheckDto(row.RowIndex, row.ServiceName, false);
                }

                if (!existingHashesByService.TryGetValue(serviceId, out var existingHashes))
                {
                    return new OutcomeScoreDuplicateCheckDto(row.RowIndex, row.ServiceName, false);
                }

                var rowData = row.FieldValues.ToDictionary(fv => fv.FieldKey, fv => fv.Value, StringComparer.OrdinalIgnoreCase);
                var rowHash = ComputeRowHash(rowData, fields);
                var isDuplicate = existingHashes.Contains(rowHash);

                return new OutcomeScoreDuplicateCheckDto(row.RowIndex, row.ServiceName, isDuplicate);
            })
            .ToList();
    }

    private async Task<IReadOnlyList<Domain.Services.Service>> GetEligibleServicesAsync(
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        CancellationToken cancellationToken)
    {
        var allServices = await serviceQueries.GetByOrganisationIdAsync(organisationId, cancellationToken);

        var serviceUsersModuleOption = await formModuleQueries.GetByCodeWithFieldsAsync(
            DataCollectionFormModuleCodes.ServiceUsers, cancellationToken);

        if (serviceUsersModuleOption.IsNone)
        {
            return Array.Empty<Domain.Services.Service>();
        }

        var serviceUsersModule = serviceUsersModuleOption.Match(m => m, () => null!);
        var qsu13Field = serviceUsersModule.Fields.FirstOrDefault(f => f.FieldKey == "QSU13");

        if (qsu13Field == null)
        {
            return Array.Empty<Domain.Services.Service>();
        }

        var serviceSubmissions = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            serviceUsersModule.Id, organisationId, dataCollectionId, "Service", cancellationToken);

        var eligibleIds = serviceSubmissions
            .Where(s => s.EntityId.HasValue && s.FieldValues.Any(fv =>
                fv.FormFieldId == qsu13Field.Id &&
                string.Equals(fv.Value, "yes", StringComparison.OrdinalIgnoreCase)))
            .Select(s => s.EntityId!.Value)
            .ToHashSet();

        return allServices.Where(s => eligibleIds.Contains(s.Id.Value)).ToList();
    }

    private static string ComputeRowHash(Dictionary<string, string> rowData, IReadOnlyList<FormField> fields)
    {
        var canonical = string.Join("|", fields
            .Where(f => f.FieldKey != "PPS01")
            .OrderBy(f => f.FieldKey, StringComparer.OrdinalIgnoreCase)
            .Select(f =>
            {
                var raw = rowData.GetValueOrDefault(f.FieldKey, "").Trim();
                var normalized = NormalizeFieldValue(raw, f) ?? "";
                return $"{f.FieldKey.ToLowerInvariant()}={normalized.ToLowerInvariant()}";
            }));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string ComputeSubmissionHash(FormSubmission submission, IReadOnlyList<FormField> fields)
    {
        var canonical = string.Join("|", fields
            .Where(f => f.FieldKey != "PPS01")
            .OrderBy(f => f.FieldKey, StringComparer.OrdinalIgnoreCase)
            .Select(f =>
            {
                var fieldValue = submission.FieldValues.FirstOrDefault(fv => fv.FormFieldId == f.Id);
                var value = fieldValue?.Value?.Trim() ?? "";
                return $"{f.FieldKey.ToLowerInvariant()}={value.ToLowerInvariant()}";
            }));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string? NormalizeFieldValue(string value, FormField field)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var fieldTypeValue = field.FieldType.ToString();

        if (fieldTypeValue == FieldType.Radio.ToString() || fieldTypeValue == FieldType.Select.ToString())
        {
            var matchingOption = field.Options.FirstOrDefault(o =>
                o.Label.Equals(value, StringComparison.OrdinalIgnoreCase) ||
                o.Value.Equals(value, StringComparison.OrdinalIgnoreCase));
            return matchingOption?.Value ?? value;
        }

        if (fieldTypeValue == FieldType.Checkbox.ToString())
        {
            var values = value.Split(',').Select(v => v.Trim()).ToList();
            var normalizedValues = values.Select(v =>
            {
                var option = field.Options.FirstOrDefault(o =>
                    o.Label.Equals(v, StringComparison.OrdinalIgnoreCase) ||
                    o.Value.Equals(v, StringComparison.OrdinalIgnoreCase));
                return option?.Value ?? v;
            });
            return string.Join(",", normalizedValues);
        }

        return value;
    }
}