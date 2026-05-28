using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using Domain.Organisations;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record UpdateDataCollectionLocalAuthoritiesCommand : IRequest<Either<DataCollectionException, DataCollection>>
{
    public Guid DataCollectionId { get; init; }
    public IReadOnlyList<Guid> LocalAuthorityIds { get; init; } = [];
}

public class UpdateDataCollectionLocalAuthoritiesCommandHandler(
    PermissionsService permissionsService,
    IDataCollectionRepository repository)
    : IRequestHandler<UpdateDataCollectionLocalAuthoritiesCommand, Either<DataCollectionException, DataCollection>>
{
    public async Task<Either<DataCollectionException, DataCollection>> Handle(
        UpdateDataCollectionLocalAuthoritiesCommand request,
        CancellationToken cancellationToken)
    {
        var dataCollectionId = new DataCollectionId(request.DataCollectionId);
        var userIdResult = permissionsService.GetUserId();

        return await userIdResult.MatchAsync(
            userId => UpdateLocalAuthorities(dataCollectionId, request.LocalAuthorityIds, userId, cancellationToken),
            e => new DataCollectionUnknownException(dataCollectionId, new Exception(e.Message)));
    }

    private async Task<Either<DataCollectionException, DataCollection>> UpdateLocalAuthorities(
        DataCollectionId dataCollectionId,
        IReadOnlyList<Guid> localAuthorityIds,
        Domain.Users.UserId userId,
        CancellationToken cancellationToken)
    {
        var dataCollection = await repository.GetByIdWithLocalAuthoritiesAsync(dataCollectionId, cancellationToken);

        return await dataCollection.MatchAsync(
            dc => ApplyLocalAuthorityChanges(dc, localAuthorityIds, userId, cancellationToken),
            () => new DataCollectionDoesNotExistException(dataCollectionId));
    }

    private async Task<Either<DataCollectionException, DataCollection>> ApplyLocalAuthorityChanges(
        DataCollection dataCollection,
        IReadOnlyList<Guid> localAuthorityIds,
        Domain.Users.UserId userId,
        CancellationToken cancellationToken)
    {
        try
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

            return await repository.UpdateAsync(dataCollection, cancellationToken);
        }
        catch (Exception exception)
        {
            return new DataCollectionUnknownException(dataCollection.Id, exception);
        }
    }
}