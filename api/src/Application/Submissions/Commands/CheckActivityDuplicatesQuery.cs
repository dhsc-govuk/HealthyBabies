using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.DataCollections.Forms.ValueObjects;
using LanguageExt;
using MediatR;

namespace Application.Submissions.Commands;

public record CheckActivityDuplicatesQuery : IRequest<Either<string, List<ActivityDuplicateCheckDto>>>
{
    public Guid SubmissionId { get; init; }
    public Guid ModuleId { get; init; }
    public IReadOnlyList<ActivityRowCheckDto> Rows { get; init; } = Array.Empty<ActivityRowCheckDto>();
}

public class CheckActivityDuplicatesQueryHandler(
    PermissionsService permissionsService,
    IFormSubmissionRepository formSubmissionRepository)
    : IRequestHandler<CheckActivityDuplicatesQuery, Either<string, List<ActivityDuplicateCheckDto>>>
{
    public async Task<Either<string, List<ActivityDuplicateCheckDto>>> Handle(
        CheckActivityDuplicatesQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();

        return await permissions.MatchAsync(
            async p => await CheckDuplicates(p, request, cancellationToken),
            e => (Either<string, List<ActivityDuplicateCheckDto>>)e.Message);
    }

    private async Task<Either<string, List<ActivityDuplicateCheckDto>>> CheckDuplicates(
        Permission permission,
        CheckActivityDuplicatesQuery request,
        CancellationToken cancellationToken)
    {
        var orgId = permission.OrganisationId.Match(id => id, () => throw new InvalidOperationException("Organisation ID required"));
        var dcId = new DataCollectionId(request.SubmissionId);
        var modId = new DataCollectionFormModuleId(request.ModuleId);

        var existingActivities = await formSubmissionRepository.GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
            modId, orgId, dcId, "Activity", cancellationToken);

        var existingNames = existingActivities
            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .Select(s => s.Name!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var results = request.Rows
            .Select(row => new ActivityDuplicateCheckDto(row.RowIndex, row.ActivityName, existingNames.Contains(row.ActivityName.Trim())))
            .ToList();

        return results;
    }
}