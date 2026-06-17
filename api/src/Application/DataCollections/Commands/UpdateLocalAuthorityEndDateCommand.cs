using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using Domain.Organisations;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record UpdateLocalAuthorityEndDateCommand : IRequest<Either<DataCollectionException, DataCollection>>
{
    public Guid DataCollectionId { get; init; }
    public Guid LocalAuthorityId { get; init; }
    public DateTime? EndDate { get; init; }
}

public class UpdateLocalAuthorityEndDateCommandHandler(
    PermissionsService permissionsService,
    IDataCollectionRepository repository)
    : IRequestHandler<UpdateLocalAuthorityEndDateCommand, Either<DataCollectionException, DataCollection>>
{
    public async Task<Either<DataCollectionException, DataCollection>> Handle(
        UpdateLocalAuthorityEndDateCommand request,
        CancellationToken cancellationToken)
    {
        var dataCollectionId = new DataCollectionId(request.DataCollectionId);
        var localAuthorityId = new OrganisationId(request.LocalAuthorityId);

        var dataCollection = await repository.GetByIdWithLocalAuthoritiesAsync(dataCollectionId, cancellationToken);

        return await dataCollection.MatchAsync(
            dc => UpdateEndDate(dc, localAuthorityId, request.EndDate, cancellationToken),
            () => new DataCollectionDoesNotExistException(dataCollectionId));
    }

    private async Task<Either<DataCollectionException, DataCollection>> UpdateEndDate(
        DataCollection dataCollection,
        OrganisationId localAuthorityId,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        try
        {
            dataCollection.UpdateLocalAuthorityEndDate(localAuthorityId, endDate);
            return await repository.UpdateAsync(dataCollection, cancellationToken);
        }
        catch (Exception exception)
        {
            return new DataCollectionUnknownException(dataCollection.Id, exception);
        }
    }
}