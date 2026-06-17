using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.Organisations;
using Domain.ServiceForms;
using Domain.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Submissions;

public class SubmissionsServiceUsersFilterTests(OrganisationAdminIntegrationTestWebFactory factory)
    : BaseOrganisationAdminIntegrationTest(factory), IAsyncLifetime
{
    private static readonly DateTime QuarterStart = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime QuarterEnd = new(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc);

    private DataCollection _dataCollection = null!;
    private DataCollectionFormModule _formModule = null!;
    private Service _liveService = null!;
    private Service _closedWithinQuarter = null!;
    private Service _closedBeforeQuarter = null!;
    private Service _closedOnQuarterStart = null!;
    private Service _closedNoDate = null!;

    [Fact]
    public async Task GetServiceUsersModule_ExcludesServicesClosedBeforeQuarter()
    {
        var response = await Client.GetAsync(ListUrl());

        response.IsSuccessStatusCode.Should().BeTrue();
        var module = await response.ToResponseModel<ServiceUsersModuleDetailDto>();

        module.Services.Select(s => s.ServiceId).Should()
            .Contain(_liveService.Id.Value)
            .And.Contain(_closedWithinQuarter.Id.Value)
            .And.Contain(_closedOnQuarterStart.Id.Value)
            .And.NotContain(_closedBeforeQuarter.Id.Value)
            .And.NotContain(_closedNoDate.Id.Value);

        module.TotalServices.Should().Be(3);
    }

    [Fact]
    public async Task GetServiceForm_ReturnsOk_WhenServiceClosedWithinQuarter()
    {
        var response = await Client.GetAsync(ServiceFormUrl(_closedWithinQuarter.Id.Value));

        response.IsSuccessStatusCode.Should().BeTrue();
        var detail = await response.ToResponseModel<ServiceFormDetailDto>();
        detail.ServiceId.Should().Be(_closedWithinQuarter.Id.Value);
    }

    [Fact]
    public async Task GetServiceForm_ReturnsNotFound_WhenServiceClosedBeforeQuarter()
    {
        var response = await Client.GetAsync(ServiceFormUrl(_closedBeforeQuarter.Id.Value));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetServiceForm_ReturnsNotFound_WhenNoLongerOfferedAndClosedDateMissing()
    {
        var response = await Client.GetAsync(ServiceFormUrl(_closedNoDate.Id.Value));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SaveServiceForm_ReturnsBadRequest_WhenServiceClosedBeforeQuarter()
    {
        var request = new SaveServiceFormRequest(new Dictionary<string, string?>(), MarkComplete: false);

        var response = await Client.PostAsJsonAsync(ServiceFormUrl(_closedBeforeQuarter.Id.Value), request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetServiceUsersModule_IncludesServiceClosedExactlyOnQuarterStartDate()
    {
        var response = await Client.GetAsync(ListUrl());

        response.IsSuccessStatusCode.Should().BeTrue();
        var module = await response.ToResponseModel<ServiceUsersModuleDetailDto>();
        module.Services.Should().Contain(s => s.ServiceId == _closedOnQuarterStart.Id.Value);
    }

    private string ListUrl() =>
        $"organisation-admin/submissions/{_dataCollection.Id.Value}/modules/{_formModule.Id.Value}/services";

    private string ServiceFormUrl(Guid serviceId) =>
        $"organisation-admin/submissions/{_dataCollection.Id.Value}/modules/{_formModule.Id.Value}/services/{serviceId}";

    public async Task InitializeAsync()
    {
        var organisation = Organisation.New(
            id: OrganisationAdminUsersData.OrganisationId,
            name: "Test Organisation",
            oNSCode: "E09000001",
            isActive: true);
        Context.Organisations.Add(organisation);

        _dataCollection = DataCollection.New(
            id: DataCollectionId.New(),
            name: "Q1 2026",
            description: null,
            startDate: QuarterStart,
            endDate: QuarterEnd);
        Context.DataCollections.Add(_dataCollection);

        var existingModule = await Context.DataCollectionFormModules
            .FirstOrDefaultAsync(m => m.Code == DataCollectionFormModuleCodes.ServiceUsers);

        if (existingModule != null)
        {
            _formModule = existingModule;
        }
        else
        {
            _formModule = DataCollectionFormModule.Create(
                id: DataCollectionFormModuleId.New(),
                code: DataCollectionFormModuleCodes.ServiceUsers,
                sectionNumber: 1,
                name: "Service Users",
                description: "Quarterly service users collection");
            Context.DataCollectionFormModules.Add(_formModule);
        }

        _liveService = LiveService("Live Service");
        _closedWithinQuarter = NoLongerOfferedService("Closed Within Quarter", QuarterStart.AddDays(30));
        _closedBeforeQuarter = NoLongerOfferedService("Closed Before Quarter", QuarterStart.AddDays(-30));
        _closedOnQuarterStart = NoLongerOfferedService("Closed On Quarter Start", QuarterStart);
        _closedNoDate = NoLongerOfferedServiceWithoutDate("Closed No Date");

        Context.Services.Add(_liveService);
        Context.Services.Add(_closedWithinQuarter);
        Context.Services.Add(_closedBeforeQuarter);
        Context.Services.Add(_closedOnQuarterStart);
        Context.Services.Add(_closedNoDate);

        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }

    private static Service LiveService(string name)
    {
        var service = Service.New(ServiceId.New(), OrganisationAdminUsersData.OrganisationId, name);
        service.AddAnswer("SMD03", "What is the status of the service?", null, ServiceFormQuestionType.Radio, 1, 3, "0", "Live");
        return service;
    }

    private static Service NoLongerOfferedService(string name, DateTime closedDate)
    {
        var service = Service.New(ServiceId.New(), OrganisationAdminUsersData.OrganisationId, name);
        service.AddAnswer("SMD03", "What is the status of the service?", null, ServiceFormQuestionType.Radio, 1, 3, "2", "No longer offered");
        service.AddAnswer("SMD04", "When did the service close?", null, ServiceFormQuestionType.Date, 1, 4, closedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), null);
        return service;
    }

    private static Service NoLongerOfferedServiceWithoutDate(string name)
    {
        var service = Service.New(ServiceId.New(), OrganisationAdminUsersData.OrganisationId, name);
        service.AddAnswer("SMD03", "What is the status of the service?", null, ServiceFormQuestionType.Radio, 1, 3, "2", "No longer offered");
        return service;
    }
}