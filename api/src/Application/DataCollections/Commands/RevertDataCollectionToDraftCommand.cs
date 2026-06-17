using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record RevertDataCollectionToDraftCommand : IRequest<Either<DataCollectionException, DataCollection>>
{
    public Guid Id { get; init; }
}

public class RevertDataCollectionToDraftCommandHandler(IDataCollectionRepository repository)
    : IRequestHandler<RevertDataCollectionToDraftCommand, Either<DataCollectionException, DataCollection>>
{
    public async Task<Either<DataCollectionException, DataCollection>> Handle(
        RevertDataCollectionToDraftCommand request,
        CancellationToken cancellationToken)
    {
        var dataCollectionId = new DataCollectionId(request.Id);
        var dataCollection = await repository.GetByIdAsync(dataCollectionId, cancellationToken);

        return await dataCollection.MatchAsync(
            dc => RevertToDraft(dc, cancellationToken),
            () => new DataCollectionDoesNotExistException(dataCollectionId));
    }

    private async Task<Either<DataCollectionException, DataCollection>> RevertToDraft(
        DataCollection dataCollection,
        CancellationToken cancellationToken)
    {
        try
        {
            dataCollection.RevertToDraft();
            return await repository.UpdateAsync(dataCollection, cancellationToken);
        }
        catch (Exception exception)
        {
            return new DataCollectionUnknownException(dataCollection.Id, exception);
        }
    }
}