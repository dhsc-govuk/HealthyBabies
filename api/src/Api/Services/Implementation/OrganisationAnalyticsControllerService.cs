using Api.Services.Abstract;
using Application.Common;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Organisations;
using Application.Organisations.Dtos;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.Organisations;

namespace Api.Services.Implementation;

public class OrganisationAnalyticsControllerService(
    IOrganisationFinancialDataQueries financialDataQueries,
    IDataCollectionFormModuleQueries formModuleQueries,
    IFormSubmissionRepository formSubmissionRepository,
    IDataCollectionSubmissionQueries dataCollectionSubmissionQueries) : IOrganisationAnalyticsControllerService
{
    private const string OnTrack = "on_track";
    private const string OffTrack = "off_track";
    private const string StatusStarted = "started";
    private const string StatusDelayed = "delayed";
    private const string StatusCompleted = "completed";

    public async Task<FinancialSummaryDto> GetFinancialSummary(
        Guid organisationId,
        string yearRange,
        CancellationToken cancellationToken)
    {
        var entityId = new OrganisationId(organisationId);
        var submissionsByStrand = (await LoadStrandActivitiesAsync(entityId, cancellationToken)).ByStrand;
        var allocations = await financialDataQueries.GetAllByOrganisationIdAsync(entityId, cancellationToken);

        decimal AllocationFor(string? module, string year) =>
            module is null ? 0m : allocations.Where(a => a.Module.Value == module && a.YearRange == year).Sum(a => a.Amount);

        var allocationYears = new[] { DeliveryPlanStrandFields.Year2627, DeliveryPlanStrandFields.Year2728, DeliveryPlanStrandFields.Year2829 };

        // Two deliberately different figures, despite similar names:
        //   Total grant allocation  — the whole 2026-29 grant, every strand across all three years.
        //   Total annual allocation — the seven strands for the selected year only.
        // The data map's dashboard sheet says "Total grant allocation = CFD22", but CFD22 is defined
        // there as "Total Funding 26/27" — a single year. That wording is a defect in the map, not
        // in this calculation: the dashboard card is labelled 2026-29 and means the full grant.
        var totalGrantAllocation = DeliveryPlanStrandFields.AllocationStrands
            .Sum(strand => allocationYears.Sum(year => AllocationFor(strand.AllocationModule, year)));
        var totalAnnualAllocation = DeliveryPlanStrandFields.AllocationStrands
            .Sum(strand => AllocationFor(strand.AllocationModule, yearRange));

        decimal forecasted = 0m, actual = 0m, committed = 0m;
        var forecastByStrand = new List<StrandForecastDto>();

        foreach (var strand in DeliveryPlanStrandFields.AllocationStrands)
        {
            var activities = submissionsByStrand.GetValueOrDefault(strand.ModuleCode) ?? [];
            var forecastField = strand.ForecastFieldsByYear.GetValueOrDefault(yearRange);

            var strandForecast = forecastField is null ? 0m : activities.Sum(a => a.Money(forecastField));
            // NOTE: kept in step with DeliveryPlanSpendReconciler.SumForecast — both must read the
            // same field for the dashboard and the ±10% validation to agree.
            forecasted += strandForecast;
            actual += activities.Sum(a => a.Money(strand.ActualSpendField));
            committed += activities.Sum(a => a.Money(strand.CommittedField));

            forecastByStrand.Add(new StrandForecastDto(
                strand.ModuleCode,
                strand.DisplayName,
                strandForecast,
                AllocationFor(strand.AllocationModule, yearRange)));
        }

        var actualAndCommitted = actual + committed;
        var planned = forecasted - actualAndCommitted;
        var unplanned = totalAnnualAllocation - (actualAndCommitted + planned);

        // Every percentage is against total annual allocation except forecasted, which is against total grant allocation.
        SpendFigureDto Annual(decimal amount) => new(amount, Percentage(amount, totalAnnualAllocation));

        var rollover = BuildRolloverSummary(submissionsByStrand, AllocationFor);

        return new FinancialSummaryDto(
            YearRange: yearRange,
            TotalGrantAllocation: totalGrantAllocation,
            TotalAnnualAllocation: totalAnnualAllocation,
            ForecastedSpend: new SpendFigureDto(forecasted, Percentage(forecasted, totalGrantAllocation)),
            CommittedSpend: Annual(committed),
            ActualSpend: Annual(actual),
            ActualAndCommittedSpend: Annual(actualAndCommitted),
            PlannedSpend: Annual(planned),
            UnplannedSpend: Annual(unplanned),
            Rollover: rollover,
            ForecastByStrand: forecastByStrand);
    }

    private static RolloverSummaryDto BuildRolloverSummary(
        IReadOnlyDictionary<string, IReadOnlyList<ActivityAnswers>> submissionsByStrand,
        Func<string?, string, decimal> allocationFor)
    {
        var rolloverConfig = DeliveryPlanStrandFields.RolloverSpend;
        var activities = submissionsByStrand.GetValueOrDefault(rolloverConfig.ModuleCode) ?? [];

        var forecastField = rolloverConfig.ForecastFieldsByYear[DeliveryPlanStrandFields.RolloverYear];
        var forecast = activities.Sum(a => a.Money(forecastField));
        var actual = activities.Sum(a => a.Money(rolloverConfig.ActualSpendField));
        var committed = activities.Sum(a => a.Money(rolloverConfig.CommittedField));
        var planned = activities.Sum(a => a.Money(rolloverConfig.PlannedField));

        // CFD23: total rollover spend is the rollover-spend allocation for 2025-26.
        var total = allocationFor(rolloverConfig.AllocationModule, DeliveryPlanStrandFields.RolloverYear);
        var unplanned = total - (actual + committed + planned);

        return new RolloverSummaryDto(total, forecast, actual, committed, planned, unplanned);
    }

    public async Task<DeliverySummaryDto> GetDeliverySummary(
        Guid organisationId,
        string? strand,
        CancellationToken cancellationToken)
    {
        var entityId = new OrganisationId(organisationId);
        var strandActivities = await LoadStrandActivitiesAsync(entityId, cancellationToken);
        var submissionsByStrand = strandActivities.ByStrand;

        var hasStrandFilter = !string.IsNullOrWhiteSpace(strand) && !string.Equals(strand, "all", StringComparison.OrdinalIgnoreCase);
        var strandsInScope = DeliveryPlanStrandFields.All
            .Where(s => !hasStrandFilter || s.ModuleCode == strand)
            .ToList();

        int onTrackCount = 0, offTrackCount = 0;
        decimal onTrackSpend = 0m, offTrackSpend = 0m;
        int notStarted = 0, started = 0, delayed = 0, completed = 0;
        var offTrackActivities = new List<OffTrackActivityDto>();

        foreach (var config in strandsInScope)
        {
            var activities = submissionsByStrand.GetValueOrDefault(config.ModuleCode) ?? [];
            foreach (var activity in activities)
            {
                var deliveryStatus = activity.Text(config.DeliveryStatusField);
                var actualSpend = activity.Money(config.ActualSpendField);

                if (deliveryStatus == OnTrack)
                {
                    onTrackCount++;
                    onTrackSpend += actualSpend;
                }
                else if (deliveryStatus == OffTrack)
                {
                    offTrackCount++;
                    offTrackSpend += actualSpend;
                    offTrackActivities.Add(new OffTrackActivityDto(
                        config.ModuleCode,
                        config.DisplayName,
                        activity.Text(config.ActivityNameField) ?? config.DisplayName,
                        actualSpend,
                        activity.DataCollectionId,
                        activity.FormModuleId,
                        activity.ActivityId));
                }

                // Not started / Started / Delayed / Completed are read from the "Select the activity
                // status" question the local authority answers on the form, not derived from the
                // FormSubmission's own workflow status. Rollover Spend has no such question — its
                // ActivityStatusFieldKey is null — so its activities don't contribute to this grid.
                var activityStatusFieldKey = DeliveryPlanActivityStatusFields.ForModule(config.ModuleCode)?.ActivityStatusFieldKey;
                if (activityStatusFieldKey is null)
                {
                    continue;
                }

                switch (activity.Text(activityStatusFieldKey))
                {
                    case StatusStarted:
                        started++;
                        break;
                    case StatusDelayed:
                        delayed++;
                        break;
                    case StatusCompleted:
                        completed++;
                        break;
                    default:
                        notStarted++;
                        break;
                }
            }
        }

        // A specific strand links to that strand's own activities list; "all strands" has no single
        // list to open, so it links to the submission summary, which lists every section.
        //
        // The round and the section have to be chosen together. Pairing the newest round overall with
        // the selected strand's module can name a section that round never contained — the review
        // screens build their section list from the round's own module assignments and answer with
        // "Failed to load this section" — so a strand filter links to the newest round that actually
        // holds that strand's activities, and falls back to the summary when it holds none.
        DeliveryActivitiesLinkDto? activitiesLink = null;
        if (hasStrandFilter
            && strandActivities.LatestRoundByModuleCode.TryGetValue(strand!, out var strandRound)
            && strandActivities.ModuleIdsByCode.TryGetValue(strand!, out var strandModuleId))
        {
            activitiesLink = new DeliveryActivitiesLinkDto(strandRound, strandModuleId);
        }
        else if (strandActivities.LatestDataCollectionId is { } latestDataCollectionId)
        {
            activitiesLink = new DeliveryActivitiesLinkDto(latestDataCollectionId, null);
        }

        return new DeliverySummaryDto(
            Strand: hasStrandFilter ? strand! : "all",
            OnTrack: new DeliveryStatusGroupDto(onTrackCount, onTrackSpend),
            OffTrack: new DeliveryStatusGroupDto(offTrackCount, offTrackSpend),
            NotStarted: notStarted,
            Started: started,
            Delayed: delayed,
            Completed: completed,
            OffTrackActivities: offTrackActivities,
            ActivitiesLink: activitiesLink);
    }

    /// <summary>
    /// Loads every delivery-plan submission for the organisation and indexes them by strand,
    /// resolving each submission's raw <see cref="FormFieldValue"/> rows to a field-key lookup.
    /// </summary>
    private async Task<StrandActivities> LoadStrandActivitiesAsync(
        OrganisationId organisationId,
        CancellationToken cancellationToken)
    {
        var modules = await formModuleQueries.GetByTypeWithFieldsAsync(DataCollectionTypeConstants.DeliveryPlan, cancellationToken);

        var moduleCodeById = modules.ToDictionary(m => m.Id, m => m.Code);
        var fieldKeyById = modules
            .SelectMany(m => m.Fields)
            .ToDictionary(f => f.Id, f => f.FieldKey);

        // Indexer assignment rather than ToDictionary: a duplicate code would throw, and a strand
        // that somehow appears twice should not take the whole Delivery data tab down.
        var moduleIdsByCode = new Dictionary<string, Guid>();
        foreach (var module in modules)
        {
            moduleIdsByCode[module.Code] = module.Id.Value;
        }

        // Only a module holding an Activities list can be addressed by activity on the review
        // screens — DeliveryPlanReviewService.GetActivity gates on exactly this flag.
        var activityListModuleIds = modules
            .Where(m => m.AllowsMultipleActivities)
            .Select(m => m.Id)
            .ToHashSet();

        // Rollover Spend (unlike the other strands) is a single-form module, not an Activities list —
        // its one submission per round is saved with EntityType = null, so it must be fetched separately
        // from the Activity-tagged submissions the other strands use.
        var strandModuleCodes = DeliveryPlanStrandFields.All.Select(s => s.ModuleCode).ToHashSet();
        var activityModuleIds = activityListModuleIds.ToList();
        var singleFormStrandModuleIds = modules
            .Where(m => !m.AllowsMultipleActivities && strandModuleCodes.Contains(m.Code))
            .Select(m => m.Id)
            .ToList();

        var activitySubmissions = await formSubmissionRepository.GetActivitiesByOrganisationAndModulesAsync(
            organisationId, activityModuleIds, cancellationToken);
        var singleFormSubmissions = singleFormStrandModuleIds.Count == 0
            ? []
            : await formSubmissionRepository.GetByOrganisationAndModulesAsync(
                organisationId, singleFormStrandModuleIds, cancellationToken);

        var submissions = activitySubmissions.Concat(singleFormSubmissions).ToList();

        // The activities query above has no concept of "which Delivery Plan round" — it returns
        // every Activity the organisation has ever submitted for these modules, across every round
        // it's ever taken part in. Restrict to rounds the organisation has actually submitted (not
        // still in progress/never finished), so leftover activities from an abandoned or superseded
        // round don't get silently added on top of the current one.
        var allSubmissions = await dataCollectionSubmissionQueries.GetByOrganisationIdAsync(organisationId, cancellationToken);
        var submittedRounds = allSubmissions
            .Where(s => s.Status is DataCollectionSubmissionStatus.Submitted or DataCollectionSubmissionStatus.Approved)
            .ToList();
        var submittedDataCollectionIds = submittedRounds
            .Select(s => s.DataCollectionId.Value)
            .ToHashSet();

        var submittedAtByRound = new Dictionary<Guid, DateTime>();
        foreach (var round in submittedRounds)
        {
            var submittedAt = round.SubmittedAt ?? DateTime.MinValue;
            if (!submittedAtByRound.TryGetValue(round.DataCollectionId.Value, out var existing) || submittedAt > existing)
            {
                submittedAtByRound[round.DataCollectionId.Value] = submittedAt;
            }
        }

        // The rounds that actually produced delivery-plan submissions, so the "View activities" link
        // targets a Delivery Plan collection. Picking the organisation's latest submitted collection
        // outright could land on a management information one, which the review screens 404 on.
        var roundsWithActivities = new HashSet<Guid>();

        var byStrand = new Dictionary<string, List<ActivityAnswers>>();
        foreach (var submission in submissions)
        {
            if (!moduleCodeById.TryGetValue(submission.FormModuleId, out var code))
            {
                continue;
            }

            if (submission.DataCollectionId is null || !submittedDataCollectionIds.Contains(submission.DataCollectionId.Value))
            {
                continue;
            }

            // Defensive: field values saved before the WAF-encode-on-save fix (or via any path that
            // missed it) are still stored as "b64:..." — decode here too so this service self-heals
            // on old data without requiring every historic activity to be re-saved. HtmlDecodeNullableField
            // is a no-op on values that were already saved as plain text going forward.
            var answers = submission.FieldValues
                .Where(v => fieldKeyById.ContainsKey(v.FormFieldId))
                .ToDictionary(v => fieldKeyById[v.FormFieldId], v => Utility.HtmlDecodeNullableField(v.Value));

            if (!byStrand.TryGetValue(code, out var list))
            {
                list = [];
                byStrand[code] = list;
            }

            roundsWithActivities.Add(submission.DataCollectionId.Value);

            list.Add(new ActivityAnswers(
                answers,
                submission.DataCollectionId.Value,
                submission.FormModuleId.Value,
                activityListModuleIds.Contains(submission.FormModuleId) ? submission.Id.Value : null));
        }

        // Most recently submitted first, so the tab links into the round the reviewer most likely
        // means. Ordering by id as a tiebreak keeps the choice stable across requests.
        Guid? LatestRound(IEnumerable<Guid> rounds) => rounds
            .Distinct()
            .OrderByDescending(id => submittedAtByRound.GetValueOrDefault(id))
            .ThenByDescending(id => id)
            .Select(id => (Guid?)id)
            .FirstOrDefault();

        // Per strand as well as overall: a strand's section only exists in the rounds that strand was
        // actually part of, so the two must be chosen together rather than independently.
        var latestRoundByModuleCode = new Dictionary<string, Guid>();
        foreach (var (code, activities) in byStrand)
        {
            if (LatestRound(activities.Select(a => a.DataCollectionId)) is { } strandRound)
            {
                latestRoundByModuleCode[code] = strandRound;
            }
        }

        return new StrandActivities(
            byStrand.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<ActivityAnswers>)kv.Value),
            moduleIdsByCode,
            latestRoundByModuleCode,
            LatestRound(roundsWithActivities));
    }

    private static decimal Percentage(decimal value, decimal total) =>
        total == 0 ? 0 : Math.Round(value / total * 100, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Every delivery-plan submission for one organisation, indexed by strand, together with what
    /// the Delivery data tab needs to link out to the review screens.
    /// </summary>
    private sealed record StrandActivities(
        IReadOnlyDictionary<string, IReadOnlyList<ActivityAnswers>> ByStrand,
        IReadOnlyDictionary<string, Guid> ModuleIdsByCode,
        IReadOnlyDictionary<string, Guid> LatestRoundByModuleCode,
        Guid? LatestDataCollectionId);

    /// <summary>A single activity submission, addressed by field key.</summary>
    private sealed class ActivityAnswers(
        IReadOnlyDictionary<string, string?> values,
        Guid dataCollectionId,
        Guid formModuleId,
        Guid? activityId)
    {
        public Guid DataCollectionId => dataCollectionId;

        public Guid FormModuleId => formModuleId;

        /// <summary>
        /// The FormSubmission id, carried so the Delivery data tab can link an off-track activity to
        /// its review screen. Null for a submission saved against a module that holds no Activities
        /// list — the review screens address those by section, not by activity.
        /// </summary>
        public Guid? ActivityId => activityId;

        public string? Text(string fieldKey) => values.GetValueOrDefault(fieldKey);

        // Shared with the reconciler so the dashboard and the ±10% validation parse money identically.
        public decimal Money(string fieldKey) => DeliveryPlanSpendReconciler.Money(values.GetValueOrDefault(fieldKey));

        public IReadOnlyDictionary<string, string?> Values => values;
    }
}