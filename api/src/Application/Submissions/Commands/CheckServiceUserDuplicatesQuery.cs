using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.DataCollections.Forms.ValueObjects;
using LanguageExt;
using MediatR;

namespace Application.Submissions.Commands;

public record CheckServiceUserDuplicatesQuery : IRequest<Either<string, List<ServiceUserDuplicateCheckDto>>>
{
    public Guid SubmissionId { get; init; }
    public Guid ModuleId { get; init; }
    public IReadOnlyList<ServiceUserRowCheckDto> Rows { get; init; } = Array.Empty<ServiceUserRowCheckDto>();
}

public class CheckServiceUserDuplicatesQueryHandler(
    PermissionsService permissionsService,
    IServiceQueries serviceQueries,
    IFormSubmissionRepository formSubmissionRepository)
    : IRequestHandler<CheckServiceUserDuplicatesQuery, Either<string, List<ServiceUserDuplicateCheckDto>>>
{
    public async Task<Either<string, List<ServiceUserDuplicateCheckDto>>> Handle(
        CheckServiceUserDuplicatesQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();

        return await permissions.MatchAsync(
            async p => await CheckDuplicates(p, request, cancellationToken),
            e => (Either<string, List<ServiceUserDuplicateCheckDto>>)e.Message);
    }

    private async Task<Either<string, List<ServiceUserDuplicateCheckDto>>> CheckDuplicates(
        Permission permission,
        CheckServiceUserDuplicatesQuery request,
        CancellationToken cancellationToken)
    {
        var orgId = permission.OrganisationId.Match(id => id, () => throw new InvalidOperationException("Organisation ID required"));
        var dcId = new DataCollectionId(request.SubmissionId);
        var modId = new DataCollectionFormModuleId(request.ModuleId);

        var allServices = await serviceQueries.GetByOrganisationIdAsync(orgId, cancellationToken);
        var serviceNameToId = allServices
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id.Value, StringComparer.OrdinalIgnoreCase);

        var results = new List<ServiceUserDuplicateCheckDto>();
        foreach (var row in request.Rows)
        {
            if (!serviceNameToId.TryGetValue(row.ServiceName, out var serviceId))
            {
                results.Add(new ServiceUserDuplicateCheckDto(row.RowIndex, row.ServiceName, false));
                continue;
            }

            var existing = await formSubmissionRepository.GetByFormModuleDataCollectionAndServiceAsync(
                modId, dcId, serviceId, cancellationToken);

            var hasExisting = existing.IsSome;
            results.Add(new ServiceUserDuplicateCheckDto(row.RowIndex, row.ServiceName, hasExisting));
        }

        return results;
    }
}