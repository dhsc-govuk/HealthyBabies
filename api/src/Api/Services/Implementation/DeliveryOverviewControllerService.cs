using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Organisations;
using Application.Organisations.Dtos;
using Domain.DataCollections;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Services.Implementation;

/// <summary>
/// Department-wide delivery overview (BLUM-7524) — the cross-organisation counterpart to
/// <see cref="OrganisationAnalyticsControllerService"/>. Aggregates across every local authority
/// via <see cref="IDeliveryOverviewQueries"/> (SQL-level GROUP BY/SUM/COUNT), rather than loading
/// every organisation's field values into memory the way the per-LA service does.
/// </summary>
public class DeliveryOverviewControllerService(
    IDataCollectionFormModuleQueries formModuleQueries,
    IServiceScopeFactory serviceScopeFactory) : IDeliveryOverviewControllerService
{
    private const string OnTrack = "on_track";
    private const string OffTrack = "off_track";
    private const string StatusStarted = "started";
    private const string StatusDelayed = "delayed";
    private const string StatusCompleted = "completed";

    /// <summary>An LA's budget variance across the strand(s) in scope must exceed this to count as off-track. Hardcoded per product decision — not admin-configurable.</summary>
    private const decimal BudgetOffTrackThresholdPercent = 20m;

    /// <summary>An LA's budget variance must exceed this, in either direction, to count as under/overspend. Hardcoded per product decision — not admin-configurable.</summary>
    private const decimal SpendVarianceThresholdPercent = 10m;

    public async Task<DeliveryOverviewSummaryDto> GetSummary(string yearRange, string? strand, CancellationToken cancellationToken)
    {
        var moduleIdByCode = await ResolveModuleIdsAsync(cancellationToken);

        // Rollover Spend is excluded from the budget/variance stats — it has no allocation or
        // tolerance-band semantics (DeliveryPlanSpendReconciler treats it as a hard cap, not a %),
        // unlike Delivery/Activity status below, which include it like GetDeliverySummary does.
        var strandsInScope = ResolveStrandsInScope(strand, DeliveryPlanStrandFields.AllocationStrands);

        var spendWork = new List<(DeliveryPlanStrandFields Config, DataCollectionFormModuleId ModuleId, string ForecastField)>();
        foreach (var config in strandsInScope)
        {
            if (!moduleIdByCode.TryGetValue(config.ModuleCode, out var moduleId))
            {
                continue;
            }

            var forecastField = config.ForecastFieldsByYear.GetValueOrDefault(yearRange);
            if (forecastField is null)
            {
                continue;
            }

            spendWork.Add((config, moduleId, forecastField));
        }

        // Each strand's query runs in its own DI scope (and therefore its own ApplicationDbContext) —
        // a DbContext isn't thread-safe, so running these concurrently against the single
        // request-scoped instance would throw. A fresh scope per parallel query avoids that while
        // still bounding overall latency to the slowest single strand query instead of their sum.
        var spendResults = await Task.WhenAll(spendWork.Select(async w =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var queries = scope.ServiceProvider.GetRequiredService<IDeliveryOverviewQueries>();
            var rows = await queries.GetStrandSpendByOrganisationAsync(w.ModuleId, w.ForecastField, w.Config.ActualSpendField, cancellationToken);
            return (w.Config, Rows: rows);
        }));

        var totalsByOrg = new Dictionary<Guid, (string Name, decimal Forecast, decimal Actual)>();
        var strandVariances = new List<StrandVarianceDto>();

        foreach (var (config, rows) in spendResults)
        {
            decimal strandForecast = 0m, strandActual = 0m;
            foreach (var row in rows)
            {
                strandForecast += row.Forecast;
                strandActual += row.Actual;

                var existing = totalsByOrg.GetValueOrDefault(row.OrganisationId, (Name: row.OrganisationName, Forecast: 0m, Actual: 0m));
                totalsByOrg[row.OrganisationId] = (row.OrganisationName, existing.Forecast + row.Forecast, existing.Actual + row.Actual);
            }

            // The strand's chart figure is the variance of the summed totals across every LA, not an
            // average of each LA's own variance — otherwise a handful of small LAs would skew it as
            // much as one large one.
            strandVariances.Add(new StrandVarianceDto(config.ModuleCode, config.DisplayName, strandForecast, strandActual, Variance(strandForecast, strandActual)));
        }

        // Off-track activity counts per LA, for the "Show local authorities" breakdown table — reuses
        // the same per-activity delivery-status check as GetDeliverySummary's off_track bucket.
        var deliveryStatusWork = new List<(DataCollectionFormModuleId ModuleId, DeliveryPlanStrandFields Config)>();
        foreach (var config in strandsInScope)
        {
            if (moduleIdByCode.TryGetValue(config.ModuleCode, out var moduleId))
            {
                deliveryStatusWork.Add((moduleId, config));
            }
        }

        var deliveryStatusResults = await Task.WhenAll(deliveryStatusWork.Select(async w =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var queries = scope.ServiceProvider.GetRequiredService<IDeliveryOverviewQueries>();
            return await queries.GetDeliveryStatusByOrganisationAsync(w.ModuleId, w.Config.DeliveryStatusField, w.Config.ActualSpendField, cancellationToken);
        }));

        var offTrackCountByOrg = new Dictionary<Guid, int>();
        var totalActivityCountByOrg = new Dictionary<Guid, int>();

        foreach (var statusRows in deliveryStatusResults)
        {
            foreach (var row in statusRows)
            {
                totalActivityCountByOrg[row.OrganisationId] = totalActivityCountByOrg.GetValueOrDefault(row.OrganisationId) + row.ActivityCount;
                if (row.Status == OffTrack)
                {
                    offTrackCountByOrg[row.OrganisationId] = offTrackCountByOrg.GetValueOrDefault(row.OrganisationId) + row.ActivityCount;
                }
            }
        }

        var localAuthorities = totalsByOrg
            .Select(kv =>
            {
                var variance = Variance(kv.Value.Forecast, kv.Value.Actual);
                var offTrackCount = offTrackCountByOrg.GetValueOrDefault(kv.Key);
                var totalCount = totalActivityCountByOrg.GetValueOrDefault(kv.Key);

                return new LocalAuthorityBudgetVarianceDto(
                    kv.Key,
                    kv.Value.Name,
                    kv.Value.Forecast,
                    kv.Value.Actual,
                    variance,
                    Math.Abs(variance) > BudgetOffTrackThresholdPercent,
                    variance < -SpendVarianceThresholdPercent,
                    variance > SpendVarianceThresholdPercent,
                    offTrackCount,
                    totalCount,
                    Percentage(offTrackCount, totalCount));
            })
            .OrderByDescending(la => la.OffTrackActivityPercentage)
            .ToList();

        // Counts and the IsUnderspend/IsOverspend flags above are computed from the same
        // SpendVarianceThresholdPercent constant, so the stat-card totals and the "Show local
        // authorities" table rows (frontend filters on the flags, not on the raw percentage) can
        // never drift apart even if the threshold changes.
        return new DeliveryOverviewSummaryDto(
            yearRange,
            NormalizeStrand(strand),
            localAuthorities.Count(la => la.IsBudgetOffTrack),
            localAuthorities.Count(la => la.IsUnderspend),
            localAuthorities.Count(la => la.IsOverspend),
            localAuthorities,
            strandVariances);
    }

    public async Task<DeliveryOverviewDeliveryStatusDto> GetDeliveryStatus(string yearRange, string? strand, CancellationToken cancellationToken)
    {
        var moduleIdByCode = await ResolveModuleIdsAsync(cancellationToken);
        var strandsInScope = ResolveStrandsInScope(strand, DeliveryPlanStrandFields.All);

        var work = new List<(DataCollectionFormModuleId ModuleId, DeliveryPlanStrandFields Config)>();
        foreach (var config in strandsInScope)
        {
            if (moduleIdByCode.TryGetValue(config.ModuleCode, out var moduleId))
            {
                work.Add((moduleId, config));
            }
        }

        var results = await Task.WhenAll(work.Select(async w =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var queries = scope.ServiceProvider.GetRequiredService<IDeliveryOverviewQueries>();
            return await queries.GetDeliveryStatusByOrganisationAsync(w.ModuleId, w.Config.DeliveryStatusField, w.Config.ActualSpendField, cancellationToken);
        }));

        int onTrackCount = 0, offTrackCount = 0, totalCount = 0;
        decimal onTrackSpend = 0m, offTrackSpend = 0m, totalSpend = 0m;

        foreach (var rows in results)
        {
            foreach (var row in rows)
            {
                totalCount += row.ActivityCount;
                totalSpend += row.ActualSpend;

                if (row.Status == OnTrack)
                {
                    onTrackCount += row.ActivityCount;
                    onTrackSpend += row.ActualSpend;
                }
                else if (row.Status == OffTrack)
                {
                    offTrackCount += row.ActivityCount;
                    offTrackSpend += row.ActualSpend;
                }
            }
        }

        return new DeliveryOverviewDeliveryStatusDto(
            yearRange,
            NormalizeStrand(strand),
            new DeliveryOverviewStatusGroupDto(onTrackCount, Percentage(onTrackCount, totalCount), onTrackSpend, Percentage(onTrackSpend, totalSpend)),
            new DeliveryOverviewStatusGroupDto(offTrackCount, Percentage(offTrackCount, totalCount), offTrackSpend, Percentage(offTrackSpend, totalSpend)));
    }

    public async Task<DeliveryOverviewActivityStatusDto> GetActivityStatus(string yearRange, string? strand, CancellationToken cancellationToken)
    {
        var moduleIdByCode = await ResolveModuleIdsAsync(cancellationToken);
        var strandsInScope = ResolveStrandsInScope(strand, DeliveryPlanStrandFields.All);

        var work = new List<(DataCollectionFormModuleId ModuleId, DeliveryPlanStrandFields Config, string ActivityStatusFieldKey)>();
        foreach (var config in strandsInScope)
        {
            // Rollover Spend has no activity-status question (its ActivityStatusFieldKey is null) —
            // its activities don't contribute here, matching GetDeliverySummary's existing skip.
            var activityStatusFieldKey = DeliveryPlanActivityStatusFields.ForModule(config.ModuleCode)?.ActivityStatusFieldKey;
            if (activityStatusFieldKey is null || !moduleIdByCode.TryGetValue(config.ModuleCode, out var moduleId))
            {
                continue;
            }

            work.Add((moduleId, config, activityStatusFieldKey));
        }

        var results = await Task.WhenAll(work.Select(async w =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var queries = scope.ServiceProvider.GetRequiredService<IDeliveryOverviewQueries>();
            return await queries.GetActivityStatusByOrganisationAsync(w.ModuleId, w.ActivityStatusFieldKey, w.Config.ActualSpendField, cancellationToken);
        }));

        int notStarted = 0, started = 0, delayed = 0, completed = 0, totalCount = 0;
        decimal notStartedSpend = 0m, startedSpend = 0m, delayedSpend = 0m, completedSpend = 0m, totalSpend = 0m;

        foreach (var rows in results)
        {
            foreach (var row in rows)
            {
                totalCount += row.ActivityCount;
                totalSpend += row.ActualSpend;

                switch (row.Status)
                {
                    case StatusStarted:
                        started += row.ActivityCount;
                        startedSpend += row.ActualSpend;
                        break;
                    case StatusDelayed:
                        delayed += row.ActivityCount;
                        delayedSpend += row.ActualSpend;
                        break;
                    case StatusCompleted:
                        completed += row.ActivityCount;
                        completedSpend += row.ActualSpend;
                        break;
                    default:
                        notStarted += row.ActivityCount;
                        notStartedSpend += row.ActualSpend;
                        break;
                }
            }
        }

        return new DeliveryOverviewActivityStatusDto(
            yearRange,
            NormalizeStrand(strand),
            new DeliveryOverviewStatusGroupDto(notStarted, Percentage(notStarted, totalCount), notStartedSpend, Percentage(notStartedSpend, totalSpend)),
            new DeliveryOverviewStatusGroupDto(started, Percentage(started, totalCount), startedSpend, Percentage(startedSpend, totalSpend)),
            new DeliveryOverviewStatusGroupDto(delayed, Percentage(delayed, totalCount), delayedSpend, Percentage(delayedSpend, totalSpend)),
            new DeliveryOverviewStatusGroupDto(completed, Percentage(completed, totalCount), completedSpend, Percentage(completedSpend, totalSpend)));
    }

    private async Task<IReadOnlyDictionary<string, DataCollectionFormModuleId>> ResolveModuleIdsAsync(CancellationToken cancellationToken)
    {
        var modules = await formModuleQueries.GetByTypeWithFieldsAsync(DataCollectionTypeConstants.DeliveryPlan, cancellationToken);

        var moduleIdByCode = new Dictionary<string, DataCollectionFormModuleId>();
        foreach (var module in modules)
        {
            moduleIdByCode[module.Code] = module.Id;
        }

        return moduleIdByCode;
    }

    private static IReadOnlyList<DeliveryPlanStrandFields> ResolveStrandsInScope(string? strand, IReadOnlyList<DeliveryPlanStrandFields> allStrands)
    {
        var hasStrandFilter = !string.IsNullOrWhiteSpace(strand) && !string.Equals(strand, "all", StringComparison.OrdinalIgnoreCase);
        return allStrands.Where(s => !hasStrandFilter || s.ModuleCode == strand).ToList();
    }

    private static string NormalizeStrand(string? strand) =>
        !string.IsNullOrWhiteSpace(strand) && !string.Equals(strand, "all", StringComparison.OrdinalIgnoreCase) ? strand : "all";

    private static decimal Variance(decimal forecast, decimal actual) =>
        forecast == 0 ? 0 : Math.Round((actual - forecast) / forecast * 100, 2, MidpointRounding.AwayFromZero);

    private static decimal Percentage(decimal value, decimal total) =>
        total == 0 ? 0 : Math.Round(value / total * 100, 2, MidpointRounding.AwayFromZero);
}