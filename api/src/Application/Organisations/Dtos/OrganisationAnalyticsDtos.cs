namespace Application.Organisations.Dtos;

/// <summary>
/// A single money figure on the Financial data tab, with its displayed percentage.
/// The denominator depends on the figure (see data.md): forecasted spend is a share
/// of total grant allocation, everything else is a share of total annual allocation.
/// </summary>
public record SpendFigureDto(
    decimal Amount,
    decimal Percentage);

/// <summary>
/// Forecasted spend versus allocation for one strand, powering the
/// "Forecasted vs allocated spend per strand" bar chart and its data table.
/// </summary>
public record StrandForecastDto(
    string Module,
    string DisplayName,
    decimal ForecastedSpend,
    decimal Allocation);

/// <summary>
/// Rollover spend (2025-26) figures, summed from the rollover-spend strand's activity
/// fields (RO10-RO13). Total rollover spend is CFD23 (the rollover-spend allocation).
/// </summary>
public record RolloverSummaryDto(
    decimal TotalRolloverSpend,
    decimal Forecast,
    decimal Actual,
    decimal Committed,
    decimal Planned,
    decimal Unplanned);

/// <summary>
/// Spend-against-allocation summary for a Local Authority for a single financial year.
/// Forecasted spend's percentage is against <see cref="TotalGrantAllocation"/>; every
/// other percentage is against <see cref="TotalAnnualAllocation"/> (see data.md).
/// </summary>
public record FinancialSummaryDto(
    string YearRange,
    decimal TotalGrantAllocation,
    decimal TotalAnnualAllocation,
    SpendFigureDto ForecastedSpend,
    SpendFigureDto CommittedSpend,
    SpendFigureDto ActualSpend,
    SpendFigureDto ActualAndCommittedSpend,
    SpendFigureDto PlannedSpend,
    SpendFigureDto UnplannedSpend,
    RolloverSummaryDto Rollover,
    IReadOnlyList<StrandForecastDto> ForecastByStrand);

/// <summary>
/// A single off-track activity listed under the Activity status section. The three identifiers
/// address the admin review screens for this activity. <see cref="ActivityId"/> is the
/// FormSubmission id, and is null only for a legacy single-form strand submission — one saved
/// against a module that does not hold an Activities list, which the review screens address by
/// section rather than by activity.
/// </summary>
public record OffTrackActivityDto(
    string Module,
    string StrandDisplayName,
    string ActivityName,
    decimal ActualSpend,
    Guid DataCollectionId,
    Guid FormModuleId,
    Guid? ActivityId);

/// <summary>
/// Where the Delivery data tab's "View activities" link points. <see cref="FormModuleId"/> is set
/// only when a single strand is selected — for "all strands" the caller links to the submission
/// summary, which lists every section. The whole record is null when the organisation has no
/// submitted Delivery Plan round to link into.
/// </summary>
public record DeliveryActivitiesLinkDto(
    Guid DataCollectionId,
    Guid? FormModuleId);

/// <summary>Count + total actual spend for a delivery-status grouping (on/off track).</summary>
public record DeliveryStatusGroupDto(
    int ActivityCount,
    decimal TotalActualSpend);

/// <summary>
/// Delivery analytics for a Local Authority, optionally filtered to one strand.
/// </summary>
public record DeliverySummaryDto(
    string Strand,
    DeliveryStatusGroupDto OnTrack,
    DeliveryStatusGroupDto OffTrack,
    int NotStarted,
    int Started,
    int Delayed,
    int Completed,
    IReadOnlyList<OffTrackActivityDto> OffTrackActivities,
    DeliveryActivitiesLinkDto? ActivitiesLink);