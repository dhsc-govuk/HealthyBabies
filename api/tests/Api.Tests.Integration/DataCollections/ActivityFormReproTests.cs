using Api.Services.Abstract;
using Application.Organisations;
using Domain.DataCollections;
using Domain.Organisations;
using Domain.Services;
using Domain.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.DataCollections;

/// <summary>
/// Covers ISubmissionsControllerService.GetActivityTemplate/CreateActivity — previously untested,
/// which is how a broken field/section mapping for a whole activity form went unnoticed.
/// </summary>
public class ActivityFormReproTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    private readonly User _adminUser = AdminsData.MainAdmin;
    private readonly Organisation _organisation = OrganisationsData.MainOrganisation;
    private DataCollection _deliveryPlan = null!;

    public async Task InitializeAsync()
    {
        await ClearAllTablesAsync();
        await SeedApplicationDataAsync();

        Context.Users.Add(_adminUser);
        Context.Organisations.Add(_organisation);

        _deliveryPlan = DataCollection.New(
            id: DataCollectionId.New(),
            name: "Repro Delivery Plan",
            description: null,
            startDate: DateTime.UtcNow.AddDays(-30),
            endDate: DateTime.UtcNow.AddDays(30),
            type: DataCollectionType.DeliveryPlan);
        _deliveryPlan.AssignLocalAuthority(_organisation.Id);

        var deliveryGrant = await Context.DataCollectionFormModules
            .SingleAsync(m => m.Code == DataCollectionFormModuleCodes.DeliveryGrant);
        _deliveryPlan.AssignFormModule(deliveryGrant.Id);

        Context.DataCollections.Add(_deliveryPlan);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }

    [Fact]
    public async Task GetActivityTemplate_ForDeliveryGrant_ReturnsAllExpectedFields()
    {
        await using var scope = Server.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ISubmissionsControllerService>();

        var deliveryGrant = await Context.DataCollectionFormModules
            .SingleAsync(m => m.Code == DataCollectionFormModuleCodes.DeliveryGrant);

        var result = await service.GetActivityTemplate(
            _organisation.Id.Value, _deliveryPlan.Id.Value, deliveryGrant.Id.Value, CancellationToken.None);

        result.IsSome.Should().BeTrue("GetActivityTemplate should return a template for a correctly configured module");
        result.IfSome(dto =>
        {
            // Activity ID (DG01) has no section of its own — a single read-only auto-generated field
            // isn't worth its own wizard step ahead of the real questions.
            dto.Sections.Should().HaveCount(3);
            dto.Fields.Should().HaveCount(25, "Delivery Grant defines 25 fields");
            dto.Fields.First(f => f.Code == "DG01").StepNumber.Should().Be(dto.Sections.Min(s => s.SectionNumber));
        });
    }

    [Fact]
    public async Task CreateActivity_ForDeliveryGrant_ReturnsAllExpectedFields()
    {
        await using var scope = Server.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ISubmissionsControllerService>();

        var deliveryGrant = await Context.DataCollectionFormModules
            .SingleAsync(m => m.Code == DataCollectionFormModuleCodes.DeliveryGrant);

        var result = await service.CreateActivity(
            _organisation.Id.Value, _deliveryPlan.Id.Value, deliveryGrant.Id.Value, CancellationToken.None);

        result.IsRight.Should().BeTrue(result.Match(_ => string.Empty, error => error));
        result.IfRight(dto =>
        {
            dto.Sections.Should().HaveCount(3);
            dto.Fields.Should().HaveCount(25, "Delivery Grant defines 25 fields");
        });
    }

    [Fact]
    public async Task GetActivityTemplate_WhenTheOrganisationHasServices_ReturnsThemForTheServiceBackedField()
    {
        var service = Service.New(ServiceId.New(), _organisation.Id, "Baby Sensory");
        Context.Services.Add(service);
        await SaveChangesAsync();

        await using var scope = Server.Services.CreateAsyncScope();
        var serviceProvider = scope.ServiceProvider.GetRequiredService<ISubmissionsControllerService>();

        var deliveryGrant = await Context.DataCollectionFormModules
            .SingleAsync(m => m.Code == DataCollectionFormModuleCodes.DeliveryGrant);

        var result = await serviceProvider.GetActivityTemplate(
            _organisation.Id.Value, _deliveryPlan.Id.Value, deliveryGrant.Id.Value, CancellationToken.None);

        result.IsSome.Should().BeTrue();
        result.IfSome(dto =>
        {
            dto.AvailableServices.Should().ContainSingle(s => s.ServiceId == service.Id.Value && s.ServiceName == "Baby Sensory");

            var serviceField = dto.Fields.Single(f => f.Code == "DG05.2");
            serviceField.Configuration.Should().Contain("dynamicOptions");
            serviceField.ConditionalRules.Should().Contain("DG05.1");
        });
    }

    [Fact]
    public async Task GetActivityTemplate_ReturnsStrandSpendForDG11MatchingTheRealAllocation()
    {
        // Mirrors the live dev DB exactly: £100,000 allocation for Delivery Grant, 2026-27.
        Context.OrganisationFinancialData.Add(OrganisationFinancialData.New(
            OrganisationFinancialDataId.New(), _organisation.Id, FinancialDataModule.DeliveryGrant, DeliveryPlanStrandFields.Year2627, 100_000m));
        await SaveChangesAsync();

        await using var scope = Server.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ISubmissionsControllerService>();

        var deliveryGrant = await Context.DataCollectionFormModules
            .SingleAsync(m => m.Code == DataCollectionFormModuleCodes.DeliveryGrant);

        var result = await service.GetActivityTemplate(
            _organisation.Id.Value, _deliveryPlan.Id.Value, deliveryGrant.Id.Value, CancellationToken.None);

        result.IsSome.Should().BeTrue();
        result.IfSome(dto =>
        {
            var dg11Spend = dto.StrandSpend.SingleOrDefault(s => s.ForecastFieldKey == "DG11");
            dg11Spend.Should().NotBeNull("DG11 (2026-27 forecast) must have a strand-spend entry for the client-side overspend check to find");
            dg11Spend!.Allocation.Should().Be(100_000m);
            dg11Spend.UpperBound.Should().Be(110_000m);
            dg11Spend.SpentByOtherActivities.Should().Be(0m, "no other activities exist yet");
        });
    }
}