using System.Globalization;
using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Organisations;
using Application.Organisations.Dtos;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Organisations;

namespace Api.Services.Implementation;

public class OrganisationAnalyticsControllerService(
    IOrganisationFinancialDataQueries financialDataQueries,
    IDataCollectionFormModuleQueries formModuleQueries,
    IFormSubmissionRepository formSubmissionRepository) : IOrganisationAnalyticsControllerService
{
    private const string OnTrack = "on_track";
    private const string OffTrack = "off_track";
    private const string StatusNotStarted = "not_started";
    private const string StatusStarted = "started";
    private const string StatusDelayed = "delayed";
    private const string StatusCompleted = "completed";

    public async Task<FinancialSummaryDto> GetFinancialSummary(
        Guid organisationId,
        string yearRange,
        CancellationToken cancellationToken)
    {
        var entityId = new OrganisationId(organisationId);
        var (submissionsByStrand, _) = await LoadStrandActivitiesAsync(entityId, cancellationToken);
        var allocations = await financialDataQueries.GetAllByOrganisationIdAsync(entityId, cancellationToken);

        decimal AllocationFor(string? module, string year) =>
            module is null ? 0m : allocations.Where(a => a.Module.Value == module && a.YearRange == year).Sum(a => a.Amount);

        var allocationYears = new[] { DeliveryPlanStrandFields.Year2627, DeliveryPlanStrandFields.Year2728, DeliveryPlanStrandFields.Year2829 };

        // CFD22: total grant allocation spans every allocation year across the seven strands.
        var totalGrantAllocation = DeliveryPlanStrandFields.AllocationStrands
            .Sum(strand => allocationYears.Sum(year => AllocationFor(strand.AllocationModule, year)));
        // Total annual allocation is the seven strands' allocation for the selected year.
        var totalAnnualAllocation = DeliveryPlanStrandFields.AllocationStrands
            .Sum(strand => AllocationFor(strand.AllocationModule, yearRange));

        decimal forecasted = 0m, actual = 0m, committed = 0m;
        var forecastByStrand = new List<StrandForecastDto>();

        foreach (var strand in DeliveryPlanStrandFields.AllocationStrands)
        {
            var activities = submissionsByStrand.GetValueOrDefault(strand.ModuleCode) ?? [];
            var forecastField = strand.ForecastFieldsByYear.GetValueOrDefault(yearRange);

            var strandForecast = forecastField is null ? 0m : activities.Sum(a => a.Money(forecastField));
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
        var (submissionsByStrand, _) = await LoadStrandActivitiesAsync(entityId, cancellationToken);

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
                        actualSpend));
                }

                switch (activity.Text(config.ActivityStatusField))
                {
                    case StatusNotStarted: notStarted++; break;
                    case StatusStarted: started++; break;
                    case StatusDelayed: delayed++; break;
                    case StatusCompleted: completed++; break;
                }
            }
        }

        return new DeliverySummaryDto(
            Strand: hasStrandFilter ? strand! : "all",
            OnTrack: new DeliveryStatusGroupDto(onTrackCount, onTrackSpend),
            OffTrack: new DeliveryStatusGroupDto(offTrackCount, offTrackSpend),
            NotStarted: notStarted,
            Started: started,
            Delayed: delayed,
            Completed: completed,
            OffTrackActivities: offTrackActivities);
    }

    /// <summary>
    /// Loads every delivery-plan submission for the organisation and indexes them by strand,
    /// resolving each submission's raw <see cref="FormFieldValue"/> rows to a field-key lookup.
    /// </summary>
    private async Task<(IReadOnlyDictionary<string, IReadOnlyList<ActivityAnswers>> ByStrand, int Count)> LoadStrandActivitiesAsync(
        OrganisationId organisationId,
        CancellationToken cancellationToken)
    {
        var modules = await formModuleQueries.GetByTypeWithFieldsAsync(DataCollectionTypeConstants.DeliveryPlan, cancellationToken);

        var moduleCodeById = modules.ToDictionary(m => m.Id, m => m.Code);
        var fieldKeyById = modules
            .SelectMany(m => m.Fields)
            .ToDictionary(f => f.Id, f => f.FieldKey);

        var submissions = await formSubmissionRepository.GetActivitiesByOrganisationAndModulesAsync(
            organisationId, moduleCodeById.Keys.ToList(), cancellationToken);

        var byStrand = new Dictionary<string, List<ActivityAnswers>>();
        foreach (var submission in submissions)
        {
            if (!moduleCodeById.TryGetValue(submission.FormModuleId, out var code))
            {
                continue;
            }

            var answers = submission.FieldValues
                .Where(v => fieldKeyById.ContainsKey(v.FormFieldId))
                .ToDictionary(v => fieldKeyById[v.FormFieldId], v => v.Value);

            if (!byStrand.TryGetValue(code, out var list))
            {
                list = [];
                byStrand[code] = list;
            }

            list.Add(new ActivityAnswers(answers));
        }

        return (byStrand.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<ActivityAnswers>)kv.Value), submissions.Count);
    }

    private static decimal Percentage(decimal value, decimal total) =>
        total == 0 ? 0 : Math.Round(value / total * 100, 2, MidpointRounding.AwayFromZero);

    /// <summary>A single activity submission, addressed by field key.</summary>
    private sealed class ActivityAnswers(IReadOnlyDictionary<string, string?> values)
    {
        public string? Text(string fieldKey) => values.GetValueOrDefault(fieldKey);

        public decimal Money(string fieldKey)
        {
            var raw = values.GetValueOrDefault(fieldKey);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return 0m;
            }

            var cleaned = new string(raw.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
            return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;
        }
    }
}