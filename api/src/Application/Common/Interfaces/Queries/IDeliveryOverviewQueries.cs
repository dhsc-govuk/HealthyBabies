using Domain.DataCollections;

namespace Application.Common.Interfaces.Queries;

/// <summary>One organisation's forecast/actual totals for a single strand and year.</summary>
public record OrganisationStrandSpendRow(Guid OrganisationId, string OrganisationName, decimal Forecast, decimal Actual);

/// <summary>One organisation's activity count and spend for a single delivery-status/activity-status value, for one strand.</summary>
public record OrganisationStatusRow(Guid OrganisationId, string? Status, int ActivityCount, decimal ActualSpend);

/// <summary>
/// Cross-organisation aggregation queries backing the department-wide Delivery overview dashboard
/// (BLUM-7524). Every method aggregates in SQL (GROUP BY/SUM/COUNT) across every organisation with a
/// submitted or approved delivery-plan round for the strand in question — unlike
/// <see cref="Application.Organisations.DeliveryPlanStrandFields"/>'s existing per-LA consumer
/// (OrganisationAnalyticsControllerService), which loads one organisation's field values into memory.
/// </summary>
public interface IDeliveryOverviewQueries
{
    /// <summary>Forecast/actual sums per organisation, for one strand module and financial year.</summary>
    Task<IReadOnlyList<OrganisationStrandSpendRow>> GetStrandSpendByOrganisationAsync(
        DataCollectionFormModuleId strandModuleId,
        string forecastFieldKey,
        string actualSpendFieldKey,
        CancellationToken cancellationToken = default);

    /// <summary>Delivery-status (on_track/off_track/etc) activity counts + spend per organisation, for one strand module.</summary>
    Task<IReadOnlyList<OrganisationStatusRow>> GetDeliveryStatusByOrganisationAsync(
        DataCollectionFormModuleId strandModuleId,
        string deliveryStatusFieldKey,
        string actualSpendFieldKey,
        CancellationToken cancellationToken = default);

    /// <summary>Activity-status (started/delayed/completed/etc) activity counts + spend per organisation, for one strand module.</summary>
    Task<IReadOnlyList<OrganisationStatusRow>> GetActivityStatusByOrganisationAsync(
        DataCollectionFormModuleId strandModuleId,
        string activityStatusFieldKey,
        string actualSpendFieldKey,
        CancellationToken cancellationToken = default);
}