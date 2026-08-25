using Application.Common.Interfaces.Queries;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="IDeliveryOverviewQueries" />
public class DeliveryOverviewQueries(ApplicationDbContext dbContext) : IDeliveryOverviewQueries
{
    private const string ActivityEntityType = "Activity";

    public async Task<IReadOnlyList<OrganisationStrandSpendRow>> GetStrandSpendByOrganisationAsync(
        DataCollectionFormModuleId strandModuleId,
        string forecastFieldKey,
        string actualSpendFieldKey,
        CancellationToken cancellationToken = default)
    {
        // Stage 1: one row per qualifying activity submission, with its two money fields already
        // parsed to numeric via the SQL-native MoneySql function (kept as a flat per-submission
        // projection, not nested inside GroupBy, so EF Core translates the correlated subqueries
        // over FieldValues as scalar subqueries rather than falling back to client evaluation).
        var perActivity = SubmittedActivities(strandModuleId)
            .Select(fs => new
            {
                fs.OrganisationId,
                OrganisationName = dbContext.Organisations
                    .Where(o => o.Id == fs.OrganisationId)
                    .Select(o => o.Name)
                    .FirstOrDefault(),
                Forecast = DbFunctionsExtensions.MoneySql(
                    fs.FieldValues.Where(v => v.FormField!.FieldKey == forecastFieldKey).Select(v => v.Value).FirstOrDefault()),
                Actual = DbFunctionsExtensions.MoneySql(
                    fs.FieldValues.Where(v => v.FormField!.FieldKey == actualSpendFieldKey).Select(v => v.Value).FirstOrDefault()),
            });

        // Stage 2: sum the already-numeric columns, grouped by organisation.
        var result = await perActivity
            .GroupBy(x => new { x.OrganisationId, x.OrganisationName })
            .Select(g => new OrganisationStrandSpendRow(
                g.Key.OrganisationId!.Value,
                g.Key.OrganisationName ?? string.Empty,
                g.Sum(x => x.Forecast),
                g.Sum(x => x.Actual)))
            .ToListAsync(cancellationToken);

        return result;
    }

    public async Task<IReadOnlyList<OrganisationStatusRow>> GetDeliveryStatusByOrganisationAsync(
        DataCollectionFormModuleId strandModuleId,
        string deliveryStatusFieldKey,
        string actualSpendFieldKey,
        CancellationToken cancellationToken = default)
    {
        var perActivity = SubmittedActivities(strandModuleId)
            .Select(fs => new
            {
                fs.OrganisationId,
                Status = fs.FieldValues.Where(v => v.FormField!.FieldKey == deliveryStatusFieldKey).Select(v => v.Value).FirstOrDefault(),
                Actual = DbFunctionsExtensions.MoneySql(
                    fs.FieldValues.Where(v => v.FormField!.FieldKey == actualSpendFieldKey).Select(v => v.Value).FirstOrDefault()),
            });

        var result = await perActivity
            .GroupBy(x => new { x.OrganisationId, x.Status })
            .Select(g => new OrganisationStatusRow(
                g.Key.OrganisationId!.Value,
                g.Key.Status,
                g.Count(),
                g.Sum(x => x.Actual)))
            .ToListAsync(cancellationToken);

        return result;
    }

    public async Task<IReadOnlyList<OrganisationStatusRow>> GetActivityStatusByOrganisationAsync(
        DataCollectionFormModuleId strandModuleId,
        string activityStatusFieldKey,
        string actualSpendFieldKey,
        CancellationToken cancellationToken = default)
    {
        // Same shape as delivery status, just grouped on the activity-status field instead.
        var perActivity = SubmittedActivities(strandModuleId)
            .Select(fs => new
            {
                fs.OrganisationId,
                Status = fs.FieldValues.Where(v => v.FormField!.FieldKey == activityStatusFieldKey).Select(v => v.Value).FirstOrDefault(),
                Actual = DbFunctionsExtensions.MoneySql(
                    fs.FieldValues.Where(v => v.FormField!.FieldKey == actualSpendFieldKey).Select(v => v.Value).FirstOrDefault()),
            });

        var result = await perActivity
            .GroupBy(x => new { x.OrganisationId, x.Status })
            .Select(g => new OrganisationStatusRow(
                g.Key.OrganisationId!.Value,
                g.Key.Status,
                g.Count(),
                g.Sum(x => x.Actual)))
            .ToListAsync(cancellationToken);

        return result;
    }

    /// <summary>
    /// Every Activity-type submission for the given strand module, restricted to organisations'
    /// Submitted/Approved rounds only — mirrors OrganisationAnalyticsControllerService's own
    /// "submittedDataCollectionIds" filter exactly (every submitted/approved round counts, not just
    /// the latest one), so cross-LA totals agree with what each LA's own Delivery data tab shows.
    /// </summary>
    private IQueryable<FormSubmission> SubmittedActivities(DataCollectionFormModuleId strandModuleId) =>
        dbContext.FormSubmissions
            .Where(fs =>
                fs.FormModuleId == strandModuleId
                && fs.EntityType == ActivityEntityType
                && fs.OrganisationId != null
                && fs.DataCollectionId != null
                && dbContext.DataCollectionSubmissions.Any(d =>
                    d.OrganisationId == fs.OrganisationId
                    && d.DataCollectionId == fs.DataCollectionId
                    && (d.Status == DataCollectionSubmissionStatus.Submitted || d.Status == DataCollectionSubmissionStatus.Approved)));
}