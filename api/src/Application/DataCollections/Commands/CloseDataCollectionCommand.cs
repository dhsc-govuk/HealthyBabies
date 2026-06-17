using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record CloseDataCollectionCommand : IRequest<Either<DataCollectionException, DataCollection>>
{
    public Guid Id { get; init; }
}

public class CloseDataCollectionCommandHandler(IDataCollectionRepository repository)
    : IRequestHandler<CloseDataCollectionCommand, Either<DataCollectionException, DataCollection>>
{
    public async Task<Either<DataCollectionException, DataCollection>> Handle(
        CloseDataCollectionCommand request,
        CancellationToken cancellationToken)
    {
        var dataCollectionId = new DataCollectionId(request.Id);
        var dataCollection = await repository.GetByIdAsync(dataCollectionId, cancellationToken);

        return await dataCollection.MatchAsync(
            dc => CloseDataCollection(dc, cancellationToken),
            () => new DataCollectionDoesNotExistException(dataCollectionId));
    }

    private async Task<Either<DataCollectionException, DataCollection>> CloseDataCollection(
        DataCollection dataCollection,
        CancellationToken cancellationToken)
    {
        try
        {
            dataCollection.Close();
            return await repository.UpdateAsync(dataCollection, cancellationToken);
        }
        catch (Exception exception)
        {
            return new DataCollectionUnknownException(dataCollection.Id, exception);
        }
    }
}