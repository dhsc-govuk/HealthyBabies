using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record UpdateDataCollectionDatesCommand : IRequest<Either<DataCollectionException, DataCollection>>
{
    public Guid Id { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
}

public class UpdateDataCollectionDatesCommandHandler(
    IDataCollectionRepository repository)
    : IRequestHandler<UpdateDataCollectionDatesCommand, Either<DataCollectionException, DataCollection>>
{
    public async Task<Either<DataCollectionException, DataCollection>> Handle(
        UpdateDataCollectionDatesCommand request,
        CancellationToken cancellationToken)
    {
        var dataCollectionId = new DataCollectionId(request.Id);

        // Local authorities must be loaded so UpdateDates can cascade the end date onto their assignments
        var dataCollection = await repository.GetByIdWithLocalAuthoritiesAsync(dataCollectionId, cancellationToken);

        return await dataCollection.MatchAsync(
            dc => UpdateDates(dc, request.StartDate, request.EndDate, cancellationToken),
            () => new DataCollectionDoesNotExistException(dataCollectionId));
    }

    private async Task<Either<DataCollectionException, DataCollection>> UpdateDates(
        DataCollection dataCollection,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        try
        {
            dataCollection.UpdateDates(startDate, endDate);
            return await repository.UpdateAsync(dataCollection, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return new DataCollectionInvalidDateRangeException(dataCollection.Id, exception.Message);
        }
        catch (Exception exception)
        {
            return new DataCollectionUnknownException(dataCollection.Id, exception);
        }
    }
}