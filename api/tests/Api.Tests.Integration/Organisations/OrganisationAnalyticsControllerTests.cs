using System.Net.Http.Json;
using Application.Organisations.Dtos;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Organisations;
using FluentAssertions;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Organisations;

public class OrganisationAnalyticsControllerTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    private readonly Organisation _organisation = OrganisationsData.MainOrganisation;

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
        summary.Started.Should().Be(1);
        summary.Delayed.Should().Be(1);
        summary.Completed.Should().Be(1);
        summary.NotStarted.Should().Be(0);
        summary.OffTrackActivities.Should().ContainSingle(a => a.Module == "delivery-grant" && a.ActualSpend == 1000m);
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
        summary.Completed.Should().Be(0);
    }

    public async Task InitializeAsync()
    {
        await ClearAllTablesAsync();
        Context.Organisations.Add(_organisation);

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

        var name = module.AddField("DG02", "Activity name", FieldType.Textarea, 4);
        var activityStatus = module.AddField("DG04", "Activity status", FieldType.Select, 6);
        var deliveryStatus = module.AddField("DG05", "Delivery status", FieldType.Select, 7);
        var forecast = module.AddField("DG07", "Forecast 2026-27", FieldType.Currency, 11);
        var actual = module.AddField("DG09", "Actual", FieldType.Currency, 13);
        var committed = module.AddField("DG10", "Committed", FieldType.Currency, 14);
        Context.DataCollectionFormModules.Add(module);

        var activityA = FormSubmission.Create(module.Id, _organisation.Id, null, "Activity");
        activityA.SetFieldValue(name.Id, "Activity A");
        activityA.SetFieldValue(activityStatus.Id, "started");
        activityA.SetFieldValue(deliveryStatus.Id, "on_track");
        activityA.SetFieldValue(forecast.Id, "10000");
        activityA.SetFieldValue(actual.Id, "3000");
        activityA.SetFieldValue(committed.Id, "2000");

        var activityB = FormSubmission.Create(module.Id, _organisation.Id, null, "Activity");
        activityB.SetFieldValue(name.Id, "Activity B");
        activityB.SetFieldValue(activityStatus.Id, "delayed");
        activityB.SetFieldValue(deliveryStatus.Id, "off_track");
        activityB.SetFieldValue(forecast.Id, "5000");
        activityB.SetFieldValue(actual.Id, "1000");
        activityB.SetFieldValue(committed.Id, "500");

        Context.FormSubmissions.AddRange(activityA, activityB);
    }

    private void SeedParenting()
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(), DataCollectionFormModuleCodes.Parenting, 3, "Parenting Support");

        var name = module.AddField("P02", "Activity name", FieldType.Textarea, 4);
        var activityStatus = module.AddField("P05", "Activity status", FieldType.Select, 7);
        var deliveryStatus = module.AddField("P06", "Delivery status", FieldType.Select, 8);
        var forecast = module.AddField("P08", "Forecast 2026-27", FieldType.Currency, 12);
        var actual = module.AddField("P10", "Actual", FieldType.Currency, 14);
        var committed = module.AddField("P11", "Committed", FieldType.Currency, 15);
        Context.DataCollectionFormModules.Add(module);

        var activity = FormSubmission.Create(module.Id, _organisation.Id, null, "Activity");
        activity.SetFieldValue(name.Id, "P Activity");
        activity.SetFieldValue(activityStatus.Id, "completed");
        activity.SetFieldValue(deliveryStatus.Id, "on_track");
        activity.SetFieldValue(forecast.Id, "8000");
        activity.SetFieldValue(actual.Id, "2000");
        activity.SetFieldValue(committed.Id, "1000");

        Context.FormSubmissions.Add(activity);
    }

    private void SeedRollover()
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(), DataCollectionFormModuleCodes.RolloverSpend, 9, "Rollover spend");

        var forecast = module.AddField("RO10", "2025-26 funding planned", FieldType.Currency, 10);
        var actual = module.AddField("RO11", "Actual", FieldType.Currency, 11);
        var committed = module.AddField("RO12", "Committed", FieldType.Currency, 12);
        var planned = module.AddField("RO13", "Planned", FieldType.Currency, 13);
        Context.DataCollectionFormModules.Add(module);

        var activity = FormSubmission.Create(module.Id, _organisation.Id, null, "Activity");
        activity.SetFieldValue(forecast.Id, "5000");
        activity.SetFieldValue(actual.Id, "2000");
        activity.SetFieldValue(committed.Id, "1000");
        activity.SetFieldValue(planned.Id, "1500");

        Context.FormSubmissions.Add(activity);
    }
}