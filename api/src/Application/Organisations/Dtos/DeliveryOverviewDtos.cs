namespace Application.Organisations.Dtos;

/// <summary>
/// One local authority's budget variance against its forecast, for the strand(s) in scope.
/// Variance = (actual - forecast) / forecast, as a percentage (forecast = 0 => variance = 0).
/// </summary>
public record LocalAuthorityBudgetVarianceDto(
    Guid OrganisationId,
    string OrganisationName,
    decimal Forecast,
    decimal Actual,
    decimal VariancePercentage,
    bool IsBudgetOffTrack,
    bool IsUnderspend,
    bool IsOverspend,
    int OffTrackActivityCount,
    int TotalActivityCount,
    decimal OffTrackActivityPercentage);

/// <summary>Aggregate over/under-spend % for one strand, summed across every local authority.</summary>
public record StrandVarianceDto(
    string Module,
    string DisplayName,
    decimal TotalForecast,
    decimal TotalActual,
    decimal VariancePercentage);

/// <summary>
/// Department-wide budget summary for the Summary tab: which local authorities are off-track on
/// budget or under/overspending, and the aggregate variance per strand.
/// </summary>
public record DeliveryOverviewSummaryDto(
    string YearRange,
    string Strand,
    int OffTrackBudgetLaCount,
    int UnderspendLaCount,
    int OverspendLaCount,
    IReadOnlyList<LocalAuthorityBudgetVarianceDto> LocalAuthorities,
    IReadOnlyList<StrandVarianceDto> VarianceByStrand);

/// <summary>Count + spend for one delivery-status or activity-status bucket, across every local authority.</summary>
public record DeliveryOverviewStatusGroupDto(
    int ActivityCount,
    decimal ActivityPercentage,
    decimal TotalActualSpend,
    decimal SpendPercentage);

/// <summary>Department-wide on-track/off-track delivery status, for the Delivery status tab.</summary>
public record DeliveryOverviewDeliveryStatusDto(
    string YearRange,
    string Strand,
    DeliveryOverviewStatusGroupDto OnTrack,
    DeliveryOverviewStatusGroupDto OffTrack);

/// <summary>Department-wide activity status breakdown, for the Activity status tab.</summary>
public record DeliveryOverviewActivityStatusDto(
    string YearRange,
    string Strand,
    DeliveryOverviewStatusGroupDto NotStarted,
    DeliveryOverviewStatusGroupDto Started,
    DeliveryOverviewStatusGroupDto Delayed,
    DeliveryOverviewStatusGroupDto Completed);