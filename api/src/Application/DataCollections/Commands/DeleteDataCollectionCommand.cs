using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record DeleteDataCollectionCommand : IRequest<Either<DataCollectionException, DataCollection>>
{
    public Guid Id { get; init; }
}

public class DeleteDataCollectionCommandHandler(
    PermissionsService permissionsService,
    IDataCollectionRepository repository)
    : IRequestHandler<DeleteDataCollectionCommand, Either<DataCollectionException, DataCollection>>
{
    public async Task<Either<DataCollectionException, DataCollection>> Handle(
        DeleteDataCollectionCommand request,
        CancellationToken cancellationToken)
    {
        var dataCollectionId = new DataCollectionId(request.Id);
        var deletedByResult = permissionsService.GetUserId();

        return await deletedByResult.MatchAsync(
            deletedById => GetAndDeleteDataCollection(dataCollectionId, deletedById, cancellationToken),
            e => new DataCollectionUnknownException(dataCollectionId, new Exception(e.Message)));
    }

    private async Task<Either<DataCollectionException, DataCollection>> GetAndDeleteDataCollection(
        DataCollectionId dataCollectionId,
        Domain.Users.UserId deletedById,
        CancellationToken cancellationToken)
    {
        var dataCollection = await repository.GetByIdAsync(dataCollectionId, cancellationToken);
        return await dataCollection.MatchAsync(
            dc => DeleteDataCollection(dc, deletedById, cancellationToken),
            () => new DataCollectionDoesNotExistException(dataCollectionId));
    }

    private async Task<Either<DataCollectionException, DataCollection>> DeleteDataCollection(
        DataCollection dataCollection,
        Domain.Users.UserId deletedById,
        CancellationToken cancellationToken)
    {
        try
        {
            dataCollection.Delete(deletedById);
            await repository.UpdateAsync(dataCollection, cancellationToken);
            return dataCollection;
        }
        catch (Exception exception)
        {
            return new DataCollectionUnknownException(dataCollection.Id, exception);
        }
    }
}