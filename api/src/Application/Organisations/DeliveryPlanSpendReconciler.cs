using System.Globalization;
using Domain.DataCollections;

namespace Application.Organisations;

/// <summary>
/// The outcome of comparing one strand's forecast spend for one financial year against the CFD
/// allocation that governs it.
/// </summary>
/// <param name="IsCap">
/// True for Rollover Spend, where CFD23 is an upper limit rather than a tolerance band — spending
/// under the approved rollover is fine, spending over it is not.
/// </param>
public sealed record StrandTolerance(
    string ModuleCode,
    string DisplayName,
    string YearRange,
    string ForecastFieldKey,
    decimal Spend,
    decimal Allocation,
    decimal LowerBound,
    decimal UpperBound,
    bool IsCap)
{
    public bool IsWithinTolerance => !ExceedsUpperBound && !BelowLowerBound;

    /// <summary>Over the allocation (+10%, or the cap for rollover). Always an error, at any point.</summary>
    public bool ExceedsUpperBound => Spend > UpperBound;

    /// <summary>
    /// Under the allocation by more than 10%. Only meaningful once the strand is finished — a
    /// half-entered strand is legitimately below its allocation.
    /// </summary>
    public bool BelowLowerBound => !IsCap && Spend < LowerBound;

    public decimal RemainingToUpperBound => Math.Max(0m, UpperBound - Spend);
}

/// <summary>
/// Cross-validates delivery-plan spend against the organisation's CFD allocations.
///
/// The data map defines 21 tolerance rules — three financial years across each of the seven
/// allocation strands (CFD01–CFD21) — plus a hard cap on Rollover Spend against CFD23. Each rule
/// compares the <em>sum of every activity in the strand</em> against the allocation, not a single
/// activity, so validation always needs the whole strand.
///
/// Field keys are resolved through <see cref="DeliveryPlanStrandFields"/> rather than written
/// literally, so a future renumbering of the delivery-plan schema is a one-file change.
/// </summary>
public static class DeliveryPlanSpendReconciler
{
    /// <summary>±10% of the allocation, per the data map's "must be +/- 10% of this value".</summary>
    public const decimal SpendTolerance = 0.10m;

    /// <summary>
    /// Parses a stored currency answer. Values reach us with assorted decoration ("£1,000.00",
    /// stray spaces), and an unparseable value counts as zero rather than throwing — a malformed
    /// figure must not stop a whole strand being reconciled.
    /// </summary>
    public static decimal Money(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return 0m;
        }

        var cleaned = new string(raw.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;
    }

    /// <summary>Which Rollover Spend field CFD23's cap is measured against.</summary>
    public enum RolloverCapField
    {
        /// <summary>RO10, Forecasted Spend 2025-26 — the analogue of every other strand's forecast field.</summary>
        ForecastedSpend,

        /// <summary>RO12, Committed Funding 2025-26 — the field the data map and BLUM-7110 name literally.</summary>
        CommittedFunding,
    }

    /// <summary>
    /// CFD23's cap is currently measured on <b>RO10</b>.
    ///
    /// The source material is inconsistent and this is the one place that resolves it. The data map
    /// says CFD23 "Governs RO12" and BLUM-7110 repeats that — but RO12 is Committed Funding, the
    /// same map's dashboard sheet defines CFD23 as <i>total rollover spend</i>, and the ticket's own
    /// reproduction figures (£30,000 forecast, £5,000 actual, £2,000 committed, "£23,000") are the
    /// value of RO13, Planned Spend. The spec names one field, describes a second and tested a third.
    ///
    /// RO10 is the reading that matches CFD23's own definition. Pending confirmation from the data
    /// map owner; flip this to <see cref="RolloverCapField.CommittedFunding"/> to follow the literal
    /// wording instead. Nothing else needs to change.
    /// </summary>
    public static readonly RolloverCapField RolloverCapMeasuredOn = RolloverCapField.ForecastedSpend;

    /// <summary>
    /// The field a strand's tolerance rule is measured on. The seven allocation strands use the
    /// year's Grant Forecast field, per the map's "Sum of all P12 must be +/- 10% of CFD04".
    ///
    /// Kept separate from ForecastFieldsByYear so that switching the rollover cap never disturbs the
    /// dashboard, whose rollover forecast must keep reading RO10 as the map's dashboard sheet requires.
    /// </summary>
    public static string? ResolveValidatedField(DeliveryPlanStrandFields strand, string yearRange)
    {
        if (strand.ModuleCode == DataCollectionFormModuleCodes.RolloverSpend
            && RolloverCapMeasuredOn == RolloverCapField.CommittedFunding)
        {
            return strand.ForecastFieldsByYear.ContainsKey(yearRange) ? strand.CommittedField : null;
        }

        return strand.ForecastFieldsByYear.GetValueOrDefault(yearRange);
    }

    /// <summary>Total validated spend for a strand in one financial year, across every activity.</summary>
    public static decimal SumForecast(
        DeliveryPlanStrandFields strand,
        string yearRange,
        IEnumerable<IReadOnlyDictionary<string, string?>> activities)
    {
        var field = ResolveValidatedField(strand, yearRange);
        return field is null
            ? 0m
            : activities.Sum(a => Money(a.GetValueOrDefault(field)));
    }

    /// <summary>
    /// Reconciles one strand-year. Returns null when the strand has no forecast field for that year
    /// (rollover only runs for 2025-26) or when no allocation has been set — DHSC/DfE enter the CFD
    /// figures, and an LA must not be blocked because that hasn't happened yet.
    /// </summary>
    public static StrandTolerance? Reconcile(
        DeliveryPlanStrandFields strand,
        string yearRange,
        IEnumerable<IReadOnlyDictionary<string, string?>> activities,
        decimal allocation)
    {
        var forecastField = ResolveValidatedField(strand, yearRange);
        if (forecastField is null)
        {
            return null;
        }

        if (allocation <= 0m)
        {
            return null;
        }

        var isCap = strand.ModuleCode == DataCollectionFormModuleCodes.RolloverSpend;
        var spend = SumForecast(strand, yearRange, activities);

        return new StrandTolerance(
            strand.ModuleCode,
            strand.DisplayName,
            yearRange,
            forecastField,
            spend,
            allocation,
            isCap ? 0m : allocation * (1m - SpendTolerance),
            isCap ? allocation : allocation * (1m + SpendTolerance),
            isCap);
    }

    /// <summary>Every financial year a strand has a forecast field for, reconciled.</summary>
    public static IReadOnlyList<StrandTolerance> Summarise(
        DeliveryPlanStrandFields strand,
        IEnumerable<IReadOnlyDictionary<string, string?>> activities,
        Func<string?, string, decimal> allocationFor)
    {
        var materialised = activities.ToList();

        return strand.ForecastFieldsByYear.Keys
            .OrderBy(year => year, StringComparer.Ordinal)
            .Select(year => Reconcile(strand, year, materialised, allocationFor(strand.AllocationModule, year)))
            .Where(result => result is not null)
            .Select(result => result!)
            .ToList();
    }

    /// <summary>
    /// Breaches only. <paramref name="upperBoundOnly"/> restricts this to over-allocation, which is
    /// the only half that can be judged while a strand is still being filled in — a strand with one
    /// activity entered so far is legitimately under its allocation, and blocking that would stop
    /// the LA entering the rest. The lower bound is checked when the strand is marked complete.
    /// </summary>
    public static IReadOnlyList<StrandTolerance> FindBreaches(
        DeliveryPlanStrandFields strand,
        IEnumerable<IReadOnlyDictionary<string, string?>> activities,
        Func<string?, string, decimal> allocationFor,
        bool upperBoundOnly = false)
    {
        return Summarise(strand, activities, allocationFor)
            .Where(result => upperBoundOnly ? result.ExceedsUpperBound : !result.IsWithinTolerance)
            .ToList();
    }

    /// <summary>
    /// A message naming the strand, the entered total, the allocation and the permitted range —
    /// everything the LA User needs to correct it without opening the guidance.
    /// </summary>
    public static string Describe(StrandTolerance breach)
    {
        var spend = Currency(breach.Spend);
        var allocation = Currency(breach.Allocation);

        if (breach.IsCap)
        {
            return $"Total {breach.DisplayName} for {breach.YearRange} across all activities ({spend}) exceeds the approved rollover amount of {allocation}. Reduce the total to {allocation} or less.";
        }

        return breach.ExceedsUpperBound
            ? $"Total {breach.DisplayName} forecast for {breach.YearRange} across all activities ({spend}) exceeds the allocation of {allocation} by more than 10%. The total must be no more than {Currency(breach.UpperBound)}."
            : $"Total {breach.DisplayName} forecast for {breach.YearRange} across all activities ({spend}) is more than 10% below the allocation of {allocation}. The total must be at least {Currency(breach.LowerBound)}.";
    }

    private static string Currency(decimal value) =>
        value.ToString("C2", CultureInfo.GetCultureInfo("en-GB"));
}