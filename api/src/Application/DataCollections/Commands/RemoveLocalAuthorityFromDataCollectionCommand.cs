using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using Domain.Organisations;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record RemoveLocalAuthorityFromDataCollectionCommand : IRequest<Either<DataCollectionException, DataCollection>>
{
    public Guid DataCollectionId { get; init; }
    public Guid LocalAuthorityId { get; init; }
}

public class RemoveLocalAuthorityFromDataCollectionCommandHandler(IDataCollectionRepository repository)
    : IRequestHandler<RemoveLocalAuthorityFromDataCollectionCommand, Either<DataCollectionException, DataCollection>>
{
    public async Task<Either<DataCollectionException, DataCollection>> Handle(
        RemoveLocalAuthorityFromDataCollectionCommand request,
        CancellationToken cancellationToken)
    {
        var dataCollectionId = new DataCollectionId(request.DataCollectionId);
        var dataCollection = await repository.GetByIdWithLocalAuthoritiesAsync(dataCollectionId, cancellationToken);

        return await dataCollection.MatchAsync(
            dc => RemoveLocalAuthority(dc, request.LocalAuthorityId, cancellationToken),
            () => new DataCollectionDoesNotExistException(dataCollectionId));
    }

    private async Task<Either<DataCollectionException, DataCollection>> RemoveLocalAuthority(
        DataCollection dataCollection,
        Guid localAuthorityId,
        CancellationToken cancellationToken)
    {
        try
        {
            dataCollection.RemoveLocalAuthority(new OrganisationId(localAuthorityId));
            return await repository.UpdateAsync(dataCollection, cancellationToken);
        }
        catch (Exception exception)
        {
            return new DataCollectionUnknownException(dataCollection.Id, exception);
        }
    }
}