using Application.Organisations.Dtos;

namespace Api.Services.Abstract;

public interface IDeliveryOverviewControllerService
{
    Task<DeliveryOverviewSummaryDto> GetSummary(string yearRange, string? strand, CancellationToken cancellationToken);

    Task<DeliveryOverviewDeliveryStatusDto> GetDeliveryStatus(string yearRange, string? strand, CancellationToken cancellationToken);

    Task<DeliveryOverviewActivityStatusDto> GetActivityStatus(string yearRange, string? strand, CancellationToken cancellationToken);
}