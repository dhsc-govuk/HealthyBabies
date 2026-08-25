using Application.Organisations.Dtos;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Organisations;
using Domain.Users;
using Domain.ValueObjects;
using FluentAssertions;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Organisations;

/// <summary>
/// Cross-organisation coverage for the department-wide Delivery overview dashboard (BLUM-7524).
/// Seeds two organisations with distinct Delivery Grant figures so assertions can verify summing
/// happens across every LA (not just within one), threshold bucketing, and strand filtering.
///
/// NOTE: this suite runs against a real Postgres via Testcontainers. It was written and
/// type-checked in an environment without Docker available, so it could not be executed here —
/// run it locally/in CI before merging, especially to confirm the SQL-pushed-down LINQ in
/// DeliveryOverviewQueries actually translates (a translation failure throws at query time).
/// </summary>
public class DeliveryOverviewControllerTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    private readonly Organisation _organisationOne = OrganisationsData.MainOrganisation;
    private readonly Organisation _organisationTwo = OrganisationsData.SecondOrganisation;

    private static string SummaryRoute(string year, string? strand = null) =>
        $"admin/trends/delivery-overview/summary?yearRange={year}" + (strand is null ? string.Empty : $"&strand={strand}");

    private static string DeliveryStatusRoute(string year, string? strand = null) =>
        $"admin/trends/delivery-overview/delivery-status?yearRange={year}" + (strand is null ? string.Empty : $"&strand={strand}");

    private static string ActivityStatusRoute(string year, string? strand = null) =>
        $"admin/trends/delivery-overview/activity-status?yearRange={year}" + (strand is null ? string.Empty : $"&strand={strand}");

    [Fact]
    public async Task ShouldAggregateSummaryAcrossOrganisations()
    {
        // Act
        var response = await Client.GetAsync(SummaryRoute("2026-27", "delivery-grant"));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var summary = await response.ToResponseModel<DeliveryOverviewSummaryDto>();

        // Org one: forecast 15000, actual 4000 -> variance -73.33% -> off-track budget AND underspend.
        // Org two: forecast 8000, actual 9000 -> variance +12.5% -> not off-track budget, but overspend.
        summary.OffTrackBudgetLaCount.Should().Be(1);
        summary.UnderspendLaCount.Should().Be(1);
        summary.OverspendLaCount.Should().Be(1);

        summary.LocalAuthorities.Should().HaveCount(2);
        var orgOne = summary.LocalAuthorities.Single(la => la.OrganisationId == _organisationOne.Id.Value);
        orgOne.Forecast.Should().Be(15000m);
        orgOne.Actual.Should().Be(4000m);
        orgOne.IsBudgetOffTrack.Should().BeTrue();
        orgOne.OffTrackActivityCount.Should().Be(1);
        orgOne.TotalActivityCount.Should().Be(2);
        orgOne.OffTrackActivityPercentage.Should().Be(50m);

        var orgTwo = summary.LocalAuthorities.Single(la => la.OrganisationId == _organisationTwo.Id.Value);
        orgTwo.Forecast.Should().Be(8000m);
        orgTwo.Actual.Should().Be(9000m);
        orgTwo.IsBudgetOffTrack.Should().BeFalse();
        orgTwo.OffTrackActivityCount.Should().Be(0);
        orgTwo.TotalActivityCount.Should().Be(1);

        // Strand variance is on the summed totals across both LAs (23000 forecast, 13000 actual),
        // not an average of each LA's own percentage.
        var strandVariance = summary.VarianceByStrand.Should().ContainSingle().Subject;
        strandVariance.Module.Should().Be("delivery-grant");
        strandVariance.TotalForecast.Should().Be(23000m);
        strandVariance.TotalActual.Should().Be(13000m);
    }

    [Fact]
    public async Task ShouldAggregateDeliveryStatusAcrossOrganisations()
    {
        // Act
        var response = await Client.GetAsync(DeliveryStatusRoute("2026-27", "delivery-grant"));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var status = await response.ToResponseModel<DeliveryOverviewDeliveryStatusDto>();

        // On track: Org one's Activity A (3000) + Org two's Activity C (9000).
        status.OnTrack.ActivityCount.Should().Be(2);
        status.OnTrack.TotalActualSpend.Should().Be(12000m);

        // Off track: Org one's Activity B only.
        status.OffTrack.ActivityCount.Should().Be(1);
        status.OffTrack.TotalActualSpend.Should().Be(1000m);
    }

    [Fact]
    public async Task ShouldAggregateActivityStatusAcrossOrganisations()
    {
        // Act
        var response = await Client.GetAsync(ActivityStatusRoute("2026-27", "delivery-grant"));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var status = await response.ToResponseModel<DeliveryOverviewActivityStatusDto>();

        status.Started.ActivityCount.Should().Be(1); // Org one, Activity A
        status.Started.TotalActualSpend.Should().Be(3000m);
        status.Delayed.ActivityCount.Should().Be(1); // Org one, Activity B
        status.Delayed.TotalActualSpend.Should().Be(1000m);
        status.Completed.ActivityCount.Should().Be(1); // Org two, Activity C
        status.Completed.TotalActualSpend.Should().Be(9000m);
        status.NotStarted.ActivityCount.Should().Be(0);
    }

    [Fact]
    public async Task ShouldExcludeOrganisationWithNoSubmittedRound()
    {
        // Arrange — org two's round is still in progress, so it must not contribute to any total.
        var inProgress = Context.DataCollectionSubmissions.Single(s => s.OrganisationId == _organisationTwo.Id);
        Context.DataCollectionSubmissions.Remove(inProgress);
        await SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(SummaryRoute("2026-27", "delivery-grant"));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var summary = await response.ToResponseModel<DeliveryOverviewSummaryDto>();

        summary.LocalAuthorities.Should().ContainSingle(la => la.OrganisationId == _organisationOne.Id.Value);
    }

    public async Task InitializeAsync()
    {
        await ClearAllTablesAsync();
        Context.Organisations.Add(_organisationOne);
        Context.Organisations.Add(_organisationTwo);

        var user = User.New(
            id: UserId.New(),
            name: new Name("Test", "User"),
            email: "test.user@example.com",
            subId: new SubId(Guid.NewGuid()),
            isActive: true,
            role: UserRole.Admin);
        Context.Users.Add(user);

        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(), DataCollectionFormModuleCodes.DeliveryGrant, 2, "Delivery Grant");
        module.SetAllowsMultipleActivities(true);
        var name = module.AddField("DG04", "Activity name", FieldType.Textarea, 4);
        var activityStatus = module.AddField("DG06", "Activity status", FieldType.Select, 6);
        var deliveryStatus = module.AddField("DG07", "Delivery status", FieldType.Select, 7);
        var forecast = module.AddField("DG11", "Forecast 2026-27", FieldType.Currency, 11);
        var actual = module.AddField("DG13", "Actual", FieldType.Currency, 13);
        var committed = module.AddField("DG14", "Committed", FieldType.Currency, 14);
        Context.DataCollectionFormModules.Add(module);

        SeedOrganisation(
            _organisationOne,
            module,
            name,
            activityStatus,
            deliveryStatus,
            forecast,
            actual,
            committed,
            user.Id,
            [
                ("Activity A", "started", "on_track", "10000", "3000", "2000"),
                ("Activity B", "delayed", "off_track", "5000", "1000", "500"),
            ]);

        // Org two's activity uses deliberately decorated currency values ("£8,000.00", " 9000 ") to
        // prove the SQL-native MoneySql function strips the same characters
        // DeliveryPlanSpendReconciler.Money does — its totals still need to sum to exactly 8000/9000
        // in the assertions below.
        SeedOrganisation(
            _organisationTwo,
            module,
            name,
            activityStatus,
            deliveryStatus,
            forecast,
            actual,
            committed,
            user.Id,
            [
                ("Activity C", "completed", "on_track", "£8,000.00", " 9000 ", "0"),
            ]);

        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }

    private void SeedOrganisation(
        Organisation organisation,
        DataCollectionFormModule module,
        FormField name,
        FormField activityStatus,
        FormField deliveryStatus,
        FormField forecast,
        FormField actual,
        FormField committed,
        UserId userId,
        (string Name, string ActivityStatus, string DeliveryStatus, string Forecast, string Actual, string Committed)[] activities)
    {
        var dataCollection = DataCollection.New(
            id: DataCollectionId.New(),
            name: $"Delivery Plan Q1 2026-27 ({organisation.Name})",
            description: null,
            startDate: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2027, 3, 31, 23, 59, 59, DateTimeKind.Utc));
        Context.DataCollections.Add(dataCollection);

        var dataCollectionSubmission = DataCollectionSubmission.Create(dataCollection.Id, organisation.Id);
        dataCollectionSubmission.Submit(userId);
        Context.DataCollectionSubmissions.Add(dataCollectionSubmission);

        foreach (var activity in activities)
        {
            var submission = FormSubmission.Create(module.Id, organisation.Id, dataCollection.Id, "Activity");
            submission.SetFieldValue(name.Id, activity.Name);
            submission.SetFieldValue(activityStatus.Id, activity.ActivityStatus);
            submission.SetFieldValue(deliveryStatus.Id, activity.DeliveryStatus);
            submission.SetFieldValue(forecast.Id, activity.Forecast);
            submission.SetFieldValue(actual.Id, activity.Actual);
            submission.SetFieldValue(committed.Id, activity.Committed);
            Context.FormSubmissions.Add(submission);
        }
    }
}