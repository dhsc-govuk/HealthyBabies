using System.Net.Http.Json;
using Application.Organisations.Dtos;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Organisations;
using Domain.Users;
using Domain.ValueObjects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Organisations;

public class OrganisationAnalyticsControllerTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    private readonly Organisation _organisation = OrganisationsData.MainOrganisation;
    private DataCollectionId _dataCollectionId = null!;
    private DataCollectionFormModuleId _deliveryGrantModuleId = null!;
    private DataCollectionFormModuleId _parentingModuleId = null!;
    private FormSubmissionId _offTrackActivityId = null!;
    private UserId _userId = null!;

    private string FinancialSummaryRoute(string year) =>
        $"organisations/{_organisation.Id.Value}/financial-summary?yearRange={year}";

    private string DeliverySummaryRoute(string? strand = null) =>
        $"organisations/{_organisation.Id.Value}/delivery-summary" + (strand is null ? string.Empty : $"?strand={strand}");

    [Fact]
    public async Task ShouldAggregateFinancialSummaryAcrossStrands()
    {
        // Act
        var response = await Client.GetAsync(FinancialSummaryRoute("2026-27"));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var summary = await response.ToResponseModel<FinancialSummaryDto>();

        summary.TotalGrantAllocation.Should().Be(40000m);
        summary.TotalAnnualAllocation.Should().Be(32000m);

        summary.ForecastedSpend.Amount.Should().Be(23000m);
        summary.ForecastedSpend.Percentage.Should().Be(57.50m); // 23000 / 40000 grant total
        summary.CommittedSpend.Amount.Should().Be(3500m);
        summary.CommittedSpend.Percentage.Should().Be(10.94m); // 3500 / 32000 annual
        summary.ActualSpend.Amount.Should().Be(6000m);
        summary.ActualSpend.Percentage.Should().Be(18.75m);
        summary.ActualAndCommittedSpend.Amount.Should().Be(9500m);
        summary.PlannedSpend.Amount.Should().Be(13500m);
        summary.UnplannedSpend.Amount.Should().Be(9000m);

        summary.Rollover.TotalRolloverSpend.Should().Be(15000m);
        summary.Rollover.Forecast.Should().Be(5000m);
        summary.Rollover.Actual.Should().Be(2000m);
        summary.Rollover.Committed.Should().Be(1000m);
        summary.Rollover.Planned.Should().Be(1500m);
        summary.Rollover.Unplanned.Should().Be(10500m); // 15000 - (2000 + 1000 + 1500)

        summary.ForecastByStrand.Should().HaveCount(7);
        var deliveryGrant = summary.ForecastByStrand.Single(s => s.Module == "delivery-grant");
        deliveryGrant.ForecastedSpend.Should().Be(15000m);
        deliveryGrant.Allocation.Should().Be(20000m);
        var parenting = summary.ForecastByStrand.Single(s => s.Module == "parenting");
        parenting.ForecastedSpend.Should().Be(8000m);
        parenting.Allocation.Should().Be(12000m);
    }

    [Fact]
    public async Task ShouldAggregateDeliverySummaryForAllStrands()
    {
        // Act
        var response = await Client.GetAsync(DeliverySummaryRoute());

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var summary = await response.ToResponseModel<DeliverySummaryDto>();

        summary.OnTrack.ActivityCount.Should().Be(2);
        summary.OnTrack.TotalActualSpend.Should().Be(5000m);
        summary.OffTrack.ActivityCount.Should().Be(1);
        summary.OffTrack.TotalActualSpend.Should().Be(1000m);
        summary.Started.Should().Be(1); // Activity A answers DG04 "started"
        summary.Delayed.Should().Be(1); // Activity B answers DG04 "delayed"
        summary.Completed.Should().Be(1); // P Activity answers P05 "completed"
        summary.NotStarted.Should().Be(1); // Activity C, unanswered DG04 — the Rollover activity is excluded, it has no activity-status question at all

        // The three identifiers the Delivery data tab needs to link the row to its review page.
        var offTrack = summary.OffTrackActivities
            .Should().ContainSingle(a => a.Module == "delivery-grant" && a.ActualSpend == 1000m).Subject;
        offTrack.DataCollectionId.Should().Be(_dataCollectionId.Value);
        offTrack.FormModuleId.Should().Be(_deliveryGrantModuleId.Value);
        offTrack.ActivityId.Should().Be(_offTrackActivityId.Value);

        // All strands has no single section to open, so the tab links to the submission summary.
        summary.ActivitiesLink.Should().NotBeNull();
        summary.ActivitiesLink!.DataCollectionId.Should().Be(_dataCollectionId.Value);
        summary.ActivitiesLink.FormModuleId.Should().BeNull();
    }

    [Fact]
    public async Task ShouldFilterDeliverySummaryByStrand()
    {
        // Act
        var response = await Client.GetAsync(DeliverySummaryRoute("delivery-grant"));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var summary = await response.ToResponseModel<DeliverySummaryDto>();

        summary.Strand.Should().Be("delivery-grant");
        summary.OnTrack.ActivityCount.Should().Be(1);
        summary.OnTrack.TotalActualSpend.Should().Be(3000m);
        summary.OffTrack.ActivityCount.Should().Be(1);
        summary.Started.Should().Be(1);
        summary.Delayed.Should().Be(1);
        summary.Completed.Should().Be(0);
        summary.NotStarted.Should().Be(1);

        // A single strand links straight to that strand's own activities list.
        summary.ActivitiesLink.Should().NotBeNull();
        summary.ActivitiesLink!.DataCollectionId.Should().Be(_dataCollectionId.Value);
        summary.ActivitiesLink.FormModuleId.Should().Be(_deliveryGrantModuleId.Value);
    }

    [Fact]
    public async Task ShouldNotReturnActivitiesLinkWhenNoRoundHasBeenSubmitted()
    {
        // Arrange — the organisation's only round is still in progress, so there is nothing to link into.
        var inProgress = await Context.DataCollectionSubmissions.SingleAsync(s => s.DataCollectionId == _dataCollectionId);
        Context.DataCollectionSubmissions.Remove(inProgress);
        Context.DataCollectionSubmissions.Add(DataCollectionSubmission.Create(_dataCollectionId, _organisation.Id));
        await SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(DeliverySummaryRoute());

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var summary = await response.ToResponseModel<DeliverySummaryDto>();

        summary.ActivitiesLink.Should().BeNull();
        summary.OffTrackActivities.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldPointActivitiesLinkAtMostRecentlySubmittedRound()
    {
        // Arrange — a second, later round carrying one more delivery grant activity.
        var laterCollection = DataCollection.New(
            id: DataCollectionId.New(),
            name: "Delivery Plan Q2 2026-27",
            description: null,
            startDate: new DateTime(2027, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2028, 3, 31, 23, 59, 59, DateTimeKind.Utc));
        Context.DataCollections.Add(laterCollection);

        var laterSubmission = DataCollectionSubmission.Create(laterCollection.Id, _organisation.Id);
        laterSubmission.Submit(_userId);
        Context.DataCollectionSubmissions.Add(laterSubmission);

        // Both rounds are submitted within the same test, so stamp the newer one explicitly rather
        // than relying on two UtcNow calls landing on different ticks.
        Context.Entry(laterSubmission).Property(s => s.SubmittedAt).CurrentValue = DateTime.UtcNow.AddDays(1);

        var module = await Context.DataCollectionFormModules
            .Include(m => m.Fields)
            .SingleAsync(m => m.Id == _deliveryGrantModuleId);
        var nameField = module.Fields.Single(f => f.FieldKey == "DG04");

        var laterActivity = FormSubmission.Create(module.Id, _organisation.Id, laterCollection.Id, "Activity");
        laterActivity.SetFieldValue(nameField.Id, "Activity D");
        Context.FormSubmissions.Add(laterActivity);
        await SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(DeliverySummaryRoute());

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var summary = await response.ToResponseModel<DeliverySummaryDto>();

        summary.ActivitiesLink.Should().NotBeNull();
        summary.ActivitiesLink!.DataCollectionId.Should().Be(laterCollection.Id.Value);

        // Rows keep their own round, which is what makes each View link resolve.
        summary.OffTrackActivities.Should().OnlyContain(a => a.DataCollectionId == _dataCollectionId.Value);
    }

    [Fact]
    public async Task ShouldPointStrandActivitiesLinkAtRoundHoldingThatStrand()
    {
        // Arrange — a newer round the organisation submitted for Parenting only. The review screens
        // build their section list from the round's own modules, so naming the Delivery Grant section
        // against this round would 404 even though it is the most recent one.
        var laterCollection = DataCollection.New(
            id: DataCollectionId.New(),
            name: "Delivery Plan Q2 2026-27",
            description: null,
            startDate: new DateTime(2027, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2028, 3, 31, 23, 59, 59, DateTimeKind.Utc));
        Context.DataCollections.Add(laterCollection);

        var laterSubmission = DataCollectionSubmission.Create(laterCollection.Id, _organisation.Id);
        laterSubmission.Submit(_userId);
        Context.DataCollectionSubmissions.Add(laterSubmission);
        Context.Entry(laterSubmission).Property(s => s.SubmittedAt).CurrentValue = DateTime.UtcNow.AddDays(1);

        var parenting = await Context.DataCollectionFormModules
            .Include(m => m.Fields)
            .SingleAsync(m => m.Id == _parentingModuleId);

        var laterActivity = FormSubmission.Create(parenting.Id, _organisation.Id, laterCollection.Id, "Activity");
        laterActivity.SetFieldValue(parenting.Fields.Single(f => f.FieldKey == "P04").Id, "Later P Activity");
        Context.FormSubmissions.Add(laterActivity);
        await SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(DeliverySummaryRoute("delivery-grant"));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var summary = await response.ToResponseModel<DeliverySummaryDto>();

        // The earlier round, because that is the one holding Delivery Grant activities.
        summary.ActivitiesLink.Should().NotBeNull();
        summary.ActivitiesLink!.DataCollectionId.Should().Be(_dataCollectionId.Value);
        summary.ActivitiesLink.FormModuleId.Should().Be(_deliveryGrantModuleId.Value);
    }

    [Fact]
    public async Task ShouldFallBackToSubmissionSummaryWhenStrandHasNoActivities()
    {
        // Act — a strand the organisation has never submitted anything for.
        var response = await Client.GetAsync(DeliverySummaryRoute("send"));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var summary = await response.ToResponseModel<DeliverySummaryDto>();

        // No section to name, so the link degrades to the round's section list rather than a 404.
        summary.ActivitiesLink.Should().NotBeNull();
        summary.ActivitiesLink!.DataCollectionId.Should().Be(_dataCollectionId.Value);
        summary.ActivitiesLink.FormModuleId.Should().BeNull();
    }

    public async Task InitializeAsync()
    {
        await ClearAllTablesAsync();
        Context.Organisations.Add(_organisation);

        // The analytics service only sums activities belonging to a data collection the
        // organisation has actually submitted (or had approved) — so the fixture needs a real
        // DataCollection + a Submitted DataCollectionSubmission, not just bare FormSubmissions.
        var user = User.New(
            id: UserId.New(),
            name: new Name("Test", "User"),
            email: "test.user@example.com",
            subId: new SubId(Guid.NewGuid()),
            isActive: true,
            role: UserRole.Admin);
        Context.Users.Add(user);
        _userId = user.Id;

        var dataCollection = DataCollection.New(
            id: DataCollectionId.New(),
            name: "Delivery Plan Q1 2026-27",
            description: null,
            startDate: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate: new DateTime(2027, 3, 31, 23, 59, 59, DateTimeKind.Utc));
        Context.DataCollections.Add(dataCollection);
        _dataCollectionId = dataCollection.Id;

        var dataCollectionSubmission = DataCollectionSubmission.Create(dataCollection.Id, _organisation.Id);
        dataCollectionSubmission.Submit(user.Id);
        Context.DataCollectionSubmissions.Add(dataCollectionSubmission);

        SeedDeliveryGrant();
        SeedParenting();
        SeedRollover();

        Context.OrganisationFinancialData.AddRange(
            Allocation(FinancialDataModule.DeliveryGrant, "2026-27", 20000m),
            Allocation(FinancialDataModule.DeliveryGrant, "2027-28", 8000m),
            Allocation(FinancialDataModule.Parenting, "2026-27", 12000m),
            Allocation(FinancialDataModule.RolloverSpend, "2025-26", 15000m));

        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }

    private OrganisationFinancialData Allocation(FinancialDataModule module, string year, decimal amount) =>
        OrganisationFinancialData.New(OrganisationFinancialDataId.New(), _organisation.Id, module, year, amount);

    private void SeedDeliveryGrant()
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(), DataCollectionFormModuleCodes.DeliveryGrant, 2, "Delivery Grant");
        module.SetAllowsMultipleActivities(true);
        _deliveryGrantModuleId = module.Id;

        var name = module.AddField("DG04", "Activity name", FieldType.Textarea, 4);
        var activityStatus = module.AddField("DG06", "Activity status", FieldType.Select, 6);
        var deliveryStatus = module.AddField("DG07", "Delivery status", FieldType.Select, 7);
        var forecast = module.AddField("DG11", "Forecast 2026-27", FieldType.Currency, 11);
        var actual = module.AddField("DG13", "Actual", FieldType.Currency, 13);
        var committed = module.AddField("DG14", "Committed", FieldType.Currency, 14);
        Context.DataCollectionFormModules.Add(module);

        var activityA = FormSubmission.Create(module.Id, _organisation.Id, _dataCollectionId, "Activity");
        activityA.SetFieldValue(name.Id, "Activity A");
        activityA.SetFieldValue(activityStatus.Id, "started");
        activityA.SetFieldValue(deliveryStatus.Id, "on_track");
        activityA.SetFieldValue(forecast.Id, "10000");
        activityA.SetFieldValue(actual.Id, "3000");
        activityA.SetFieldValue(committed.Id, "2000");

        var activityB = FormSubmission.Create(module.Id, _organisation.Id, _dataCollectionId, "Activity");
        activityB.SetFieldValue(name.Id, "Activity B");
        activityB.SetFieldValue(activityStatus.Id, "delayed");
        activityB.SetFieldValue(deliveryStatus.Id, "off_track");
        activityB.SetFieldValue(forecast.Id, "5000");
        activityB.SetFieldValue(actual.Id, "1000");
        activityB.SetFieldValue(committed.Id, "500");
        _offTrackActivityId = activityB.Id;

        // Represents an LA clicking "Add activity" and leaving it untouched — no field values at all.
        var activityC = FormSubmission.Create(module.Id, _organisation.Id, _dataCollectionId, "Activity");

        Context.FormSubmissions.AddRange(activityA, activityB, activityC);
    }

    private void SeedParenting()
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(), DataCollectionFormModuleCodes.Parenting, 3, "Parenting Support");
        module.SetAllowsMultipleActivities(true);
        _parentingModuleId = module.Id;

        var name = module.AddField("P04", "Activity name", FieldType.Textarea, 4);
        var activityStatus = module.AddField("P07", "Activity status", FieldType.Select, 7);
        var deliveryStatus = module.AddField("P08", "Delivery status", FieldType.Select, 8);
        var forecast = module.AddField("P12", "Forecast 2026-27", FieldType.Currency, 12);
        var actual = module.AddField("P14", "Actual", FieldType.Currency, 14);
        var committed = module.AddField("P15", "Committed", FieldType.Currency, 15);
        Context.DataCollectionFormModules.Add(module);

        var activity = FormSubmission.Create(module.Id, _organisation.Id, _dataCollectionId, "Activity");
        activity.SetFieldValue(name.Id, "P Activity");
        activity.SetFieldValue(activityStatus.Id, "completed");
        activity.SetFieldValue(deliveryStatus.Id, "on_track");
        activity.SetFieldValue(forecast.Id, "8000");
        activity.SetFieldValue(actual.Id, "2000");
        activity.SetFieldValue(committed.Id, "1000");
        activity.MarkAsComplete();

        Context.FormSubmissions.Add(activity);
    }

    private void SeedRollover()
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(), DataCollectionFormModuleCodes.RolloverSpend, 9, "Rollover spend");
        module.SetAllowsMultipleActivities(true);

        var forecast = module.AddField("RO10", "2025-26 funding planned", FieldType.Currency, 10);
        var actual = module.AddField("RO11", "Actual", FieldType.Currency, 11);
        var committed = module.AddField("RO12", "Committed", FieldType.Currency, 12);
        var planned = module.AddField("RO13", "Planned", FieldType.Currency, 13);
        Context.DataCollectionFormModules.Add(module);

        // Rollover Spend is now a multi-activity module like Delivery Grant/Parenting, so its
        // submissions are Activity-tagged rather than the old single-form EntityType = null shape.
        var submission = FormSubmission.Create(module.Id, _organisation.Id, _dataCollectionId, "Activity");
        submission.SetFieldValue(forecast.Id, "5000");
        submission.SetFieldValue(actual.Id, "2000");
        submission.SetFieldValue(committed.Id, "1000");
        submission.SetFieldValue(planned.Id, "1500");

        Context.FormSubmissions.Add(submission);
    }
}