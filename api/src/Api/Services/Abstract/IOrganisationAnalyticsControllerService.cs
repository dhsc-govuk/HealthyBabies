using Application.Organisations.Dtos;

namespace Api.Services.Abstract;

public interface IOrganisationAnalyticsControllerService
{
    Task<FinancialSummaryDto> GetFinancialSummary(
        Guid organisationId,
        string yearRange,
        CancellationToken cancellationToken);

    Task<DeliverySummaryDto> GetDeliverySummary(
        Guid organisationId,
        string? strand,
        CancellationToken cancellationToken);
}