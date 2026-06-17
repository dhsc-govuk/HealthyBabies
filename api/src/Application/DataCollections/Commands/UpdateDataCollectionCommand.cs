using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using Domain.Organisations;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record UpdateDataCollectionCommand : IRequest<Either<DataCollectionException, DataCollection>>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsSubmittedByAllLocalAuthorities { get; init; }
    public bool SaveAsDraft { get; init; } = false;
    public IReadOnlyList<Guid> LocalAuthorityIds { get; init; } = [];
    public IReadOnlyList<Guid> FormModuleIds { get; init; } = [];
}

public class UpdateDataCollectionCommandHandler(
    PermissionsService permissionsService,
    IDataCollectionRepository repository)
    : IRequestHandler<UpdateDataCollectionCommand, Either<DataCollectionException, DataCollection>>
{
    public async Task<Either<DataCollectionException, DataCollection>> Handle(
        UpdateDataCollectionCommand request,
        CancellationToken cancellationToken)
    {
        var dataCollectionId = new DataCollectionId(request.Id);
        var dataCollectionResult = await GetDataCollection(dataCollectionId, cancellationToken);
        return await dataCollectionResult
            .BindAsync(dc => FindDuplicate(request, dataCollectionId, dc, cancellationToken));
    }

    private async Task<Either<DataCollectionException, DataCollection>> FindDuplicate(
        UpdateDataCollectionCommand request,
        DataCollectionId dataCollectionId,
        DataCollection dataCollection,
        CancellationToken cancellationToken)
    {
        var dataCollectionWithName = await repository.FindDuplicateAsync(request.Name, dataCollectionId, cancellationToken);
        return await dataCollectionWithName.MatchAsync(
            dc => new DataCollectionAlreadyExistsException(dc.Id, request.Name),
            () => UpdateDataCollection(request, dataCollection, cancellationToken));
    }

    private async Task<Either<DataCollectionException, DataCollection>> UpdateDataCollection(
        UpdateDataCollectionCommand request,
        DataCollection dataCollection,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.SaveAsDraft)
            {
                dataCollection.UpdateAndSaveAsDraft(request.Name, request.Description?.Trim(), request.StartDate, request.EndDate, request.IsSubmittedByAllLocalAuthorities);
            }
            else
            {
                dataCollection.UpdateAndPublish(request.Name, request.Description?.Trim(), request.StartDate, request.EndDate, request.IsSubmittedByAllLocalAuthorities);
            }

            // Update local authority assignments
            if (request.IsSubmittedByAllLocalAuthorities)
            {
                dataCollection.ClearLocalAuthorities();
            }
            else
            {
                var userIdResult = permissionsService.GetUserId();
                userIdResult.Match(
                    userId => { ApplyLocalAuthorityChanges(dataCollection, request.LocalAuthorityIds, userId); return LanguageExt.Unit.Default; },
                    e => LanguageExt.Unit.Default);
            }

            var result = await repository.UpdateAsync(dataCollection, cancellationToken);

            // Update form module assignments separately via repository
            await repository.UpdateFormModuleAssignmentsAsync(dataCollection.Id, request.FormModuleIds, cancellationToken);

            return result;
        }
        catch (Exception exception)
        {
            return new DataCollectionUnknownException(dataCollection.Id, exception);
        }
    }

    private void ApplyLocalAuthorityChanges(
        DataCollection dataCollection,
        IReadOnlyList<Guid> localAuthorityIds,
        Domain.Users.UserId userId)
    {
        var currentLaIds = dataCollection.LocalAuthorities
            .Select(la => la.LocalAuthorityId.Value)
            .ToHashSet();

        var newLaIds = localAuthorityIds.ToHashSet();

        var toRemove = currentLaIds.Except(newLaIds).ToList();
        var toAdd = newLaIds.Except(currentLaIds).ToList();

        foreach (var laId in toRemove)
        {
            dataCollection.RemoveLocalAuthority(new OrganisationId(laId));
        }

        foreach (var laId in toAdd)
        {
            dataCollection.AssignLocalAuthority(new OrganisationId(laId), userId);
        }
    }

    private async Task<Either<DataCollectionException, DataCollection>> GetDataCollection(
        DataCollectionId id,
        CancellationToken cancellationToken)
    {
        // Use GetByIdWithLocalAuthoritiesAsync to ensure FormModuleAssignments are loaded
        var dataCollection = await repository.GetByIdWithLocalAuthoritiesAsync(id, cancellationToken);
        return dataCollection.Match<Either<DataCollectionException, DataCollection>>(
            dc => dc,
            new DataCollectionDoesNotExistException(id));
    }
}