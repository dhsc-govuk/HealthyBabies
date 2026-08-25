using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using Application.DataCollections.Dtos;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Organisations;
using Domain.Users;
using FluentAssertions;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.DataCollections;

/// <summary>
/// Covers the admin review of a submitted Delivery Plan (BLUM-7429).
///
/// These tests do double duty. Besides proving the acceptance criteria, they are the contract guard
/// on the reused read methods: DeliveryPlanReviewService injects ISubmissionsControllerService and
/// depends on the shapes GetFormModuleById and GetActivityForm return. Neither has direct test cover
/// of its own, so a change made to either for a local authority reason should surface here.
/// </summary>
public class DeliveryPlanReviewTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    private readonly User _adminUser = AdminsData.MainAdmin;
    private readonly Organisation _organisation = OrganisationsData.MainOrganisation;

    private DataCollection _deliveryPlan = null!;
    private DataCollection _managementInformation = null!;
    private DataCollectionFormModule _focusModule = null!;
    private DataCollectionFormModule _sendModule = null!;
    private DataCollectionFormModule _rolloverModule = null!;
    private FormField _focusQuestion = null!;
    private FormField _sendActivityStatus = null!;
    private FormField _sendDeliveryStatus = null!;
    private FormField _rolloverDeliveryStatus = null!;
    private FormSubmission _onTrackActivity = null!;
    private FormSubmission _offTrackActivity = null!;
    private FormSubmission _rolloverActivity = null!;

    [Fact]
    public async Task GetSection_WithoutActivityRecords_ReturnsQuestionAndValuePairs()
    {
        var response = await Client.GetAsync(SectionRoute(_deliveryPlan.Id.Value, _organisation.Id.Value, _focusModule.Id.Value));

        response.IsSuccessStatusCode.Should().BeTrue();
        var section = await response.Content.ReadFromJsonAsync<ReviewSectionDetailDto>();

        section.Should().NotBeNull();
        section!.AllowsMultipleActivities.Should().BeFalse();
        section.Activities.Should().BeEmpty();
        section.LocalAuthorityName.Should().Be(_organisation.Name);

        var answers = section.AnswerGroups.SelectMany(group => group.Answers).ToList();
        answers.Should().ContainSingle(answer => answer.QuestionCode == _focusQuestion.FieldKey);
        answers.Single(answer => answer.QuestionCode == _focusQuestion.FieldKey).Value.Should().Be("Our stated focus");
    }

    [Fact]
    public async Task GetSection_WithActivityRecords_ReturnsOneRowPerActivityWithBothStatuses()
    {
        var response = await Client.GetAsync(SectionRoute(_deliveryPlan.Id.Value, _organisation.Id.Value, _sendModule.Id.Value));

        response.IsSuccessStatusCode.Should().BeTrue();
        var section = await response.Content.ReadFromJsonAsync<ReviewSectionDetailDto>();

        section.Should().NotBeNull();
        section!.AllowsMultipleActivities.Should().BeTrue();
        section.AnswerGroups.Should().BeEmpty();
        section.Activities.Should().HaveCount(2);

        var first = section.Activities.Single(a => a.ActivityId == _onTrackActivity.Id.Value);
        first.ActivityName.Should().Be("Activity 1");
        first.ActivityStatus.Should().Be("Started");
        first.DeliveryStatus.Should().Be("On track");

        // The raw value travels alongside the label so the client picks a tag colour without
        // matching on English text.
        first.DeliveryStatusValue.Should().Be("on_track");

        var second = section.Activities.Single(a => a.ActivityId == _offTrackActivity.Id.Value);
        second.ActivityStatus.Should().Be("Delayed");
        second.DeliveryStatus.Should().Be("Off track");
        second.DeliveryStatusValue.Should().Be("off_track");
    }

    [Fact]
    public async Task GetSection_ForRolloverSpend_ReturnsDeliveryStatusWithoutActivityStatus()
    {
        var response = await Client.GetAsync(SectionRoute(_deliveryPlan.Id.Value, _organisation.Id.Value, _rolloverModule.Id.Value));

        response.IsSuccessStatusCode.Should().BeTrue();
        var section = await response.Content.ReadFromJsonAsync<ReviewSectionDetailDto>();

        var activity = section!.Activities.Single(a => a.ActivityId == _rolloverActivity.Id.Value);

        // Rollover Spend is seeded with a delivery status question only, so the activity status
        // column is legitimately empty rather than an error.
        activity.ActivityStatus.Should().BeNull();
        activity.ActivityStatusValue.Should().BeNull();
        activity.DeliveryStatus.Should().Be("On track");
    }

    [Fact]
    public async Task GetSection_Navigation_HasNoPreviousOnFirstSectionAndNoNextOnLast()
    {
        var firstResponse = await Client.GetAsync(SectionRoute(_deliveryPlan.Id.Value, _organisation.Id.Value, _focusModule.Id.Value));
        var first = await firstResponse.Content.ReadFromJsonAsync<ReviewSectionDetailDto>();

        first!.Navigation.Position.Should().Be(1);
        first.Navigation.Total.Should().Be(3);
        first.Navigation.Previous.Should().BeNull();
        first.Navigation.Next!.Id.Should().Be(_sendModule.Id.Value);
        first.Navigation.Next.Position.Should().Be(2);

        var lastResponse = await Client.GetAsync(SectionRoute(_deliveryPlan.Id.Value, _organisation.Id.Value, _rolloverModule.Id.Value));
        var last = await lastResponse.Content.ReadFromJsonAsync<ReviewSectionDetailDto>();

        last!.Navigation.Position.Should().Be(3);
        last.Navigation.Next.Should().BeNull();
        last.Navigation.Previous!.Id.Should().Be(_sendModule.Id.Value);
    }

    [Fact]
    public async Task GetActivity_ReturnsAnswersAndActivityNavigation()
    {
        var route = ActivityRoute(_deliveryPlan.Id.Value, _organisation.Id.Value, _sendModule.Id.Value, _onTrackActivity.Id.Value);

        var response = await Client.GetAsync(route);

        response.IsSuccessStatusCode.Should().BeTrue();
        var activity = await response.Content.ReadFromJsonAsync<ReviewActivityDetailDto>();

        activity.Should().NotBeNull();
        activity!.ActivityName.Should().Be("Activity 1");
        activity.AnswerGroups.SelectMany(group => group.Answers).Should().NotBeEmpty();

        activity.Navigation.Position.Should().Be(1);
        activity.Navigation.Total.Should().Be(2);
        activity.Navigation.Previous.Should().BeNull();
        activity.Navigation.Next!.Id.Should().Be(_offTrackActivity.Id.Value);
    }

    [Fact]
    public async Task GetActivity_ReturnsNotFound_WhenActivityBelongsToAnotherSection()
    {
        var route = ActivityRoute(_deliveryPlan.Id.Value, _organisation.Id.Value, _sendModule.Id.Value, _rolloverActivity.Id.Value);

        var response = await Client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSection_ReturnsNotFound_ForManagementInformationCollection()
    {
        var response = await Client.GetAsync(SectionRoute(_managementInformation.Id.Value, _organisation.Id.Value, _focusModule.Id.Value));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSection_ReturnsNotFound_WhenModuleIsNotAssignedToTheCollection()
    {
        var response = await Client.GetAsync(SectionRoute(_deliveryPlan.Id.Value, _organisation.Id.Value, Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetLocalAuthoritySubmission_ReturnsTypeAndSections_ForDeliveryPlan()
    {
        var response = await Client.GetAsync(
            $"admin/data-collections/{_deliveryPlan.Id.Value}/local-authorities/{_organisation.Id.Value}");

        response.IsSuccessStatusCode.Should().BeTrue();
        var detail = await response.Content.ReadFromJsonAsync<LocalAuthoritySubmissionDetailDto>();

        detail!.Type.Should().Be(DataCollectionTypeConstants.DeliveryPlan);
        detail.Sections.Should().HaveCount(3);
        detail.Sections.Select(s => s.SectionNumber).Should().BeInAscendingOrder();
        detail.Sections.Single(s => s.Code == DataCollectionFormModuleCodes.Send).AllowsMultipleActivities.Should().BeTrue();
        detail.Sections.Single(s => s.Code == DataCollectionFormModuleCodes.Focus).AllowsMultipleActivities.Should().BeFalse();
    }

    [Fact]
    public async Task GetLocalAuthoritySubmission_ReturnsNoSections_ForManagementInformation()
    {
        var response = await Client.GetAsync(
            $"admin/data-collections/{_managementInformation.Id.Value}/local-authorities/{_organisation.Id.Value}");

        response.IsSuccessStatusCode.Should().BeTrue();
        var detail = await response.Content.ReadFromJsonAsync<LocalAuthoritySubmissionDetailDto>();

        detail!.Type.Should().Be(DataCollectionTypeConstants.ManagementInformation);
        detail.Sections.Should().BeEmpty();
    }

    /// <summary>
    /// AC6 — what a reviewer reads on screen must be the same answer the CSV export produces.
    /// Both paths resolve through AnswerSnapshotResolver, so this asserts they have not diverged.
    /// </summary>
    [Fact]
    public async Task GetActivity_AnswerValues_MatchTheCsvExportForTheSameSubmission()
    {
        var reviewResponse = await Client.GetAsync(
            ActivityRoute(_deliveryPlan.Id.Value, _organisation.Id.Value, _sendModule.Id.Value, _onTrackActivity.Id.Value));
        var review = await reviewResponse.Content.ReadFromJsonAsync<ReviewActivityDetailDto>();

        var onScreen = review!.AnswerGroups
            .SelectMany(group => group.Answers)
            .ToDictionary(answer => answer.QuestionCode, answer => answer.Value ?? string.Empty);

        var exportResponse = await Client.GetAsync(
            $"admin/data-collections/{_deliveryPlan.Id.Value}/local-authorities/{_organisation.Id.Value}/download");
        exportResponse.IsSuccessStatusCode.Should().BeTrue();
        var exportJson = await ReadSubmissionJsonEntry(exportResponse);

        foreach (var (questionCode, value) in onScreen.Where(pair => pair.Value.Length > 0))
        {
            exportJson.Should().Contain(questionCode, $"the export should include question {questionCode}");
            exportJson.Should().Contain(value, $"the export value for {questionCode} should match the screen");
        }
    }

    public async Task InitializeAsync()
    {
        await ClearAllTablesAsync();

        Context.Users.Add(_adminUser);
        Context.Organisations.Add(_organisation);

        _focusModule = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(), DataCollectionFormModuleCodes.Focus, 1, "Focus");
        _focusQuestion = _focusModule.AddField("FOC01", "What is your focus?", FieldType.Text, 1);

        _sendModule = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(), DataCollectionFormModuleCodes.Send, 2, "SEND", allowsMultipleActivities: true);
        _sendActivityStatus = _sendModule.AddField("SEND07", "Select the activity status", FieldType.Select, 1);
        _sendActivityStatus.AddOption("started", "Started", 1);
        _sendActivityStatus.AddOption("delayed", "Delayed", 2);
        _sendDeliveryStatus = _sendModule.AddField("SEND08", "Select the delivery status of this activity", FieldType.Select, 2);
        _sendDeliveryStatus.AddOption("on_track", "On track", 1);
        _sendDeliveryStatus.AddOption("off_track", "Off track", 2);

        _rolloverModule = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(), DataCollectionFormModuleCodes.RolloverSpend, 3, "Rollover Spend", allowsMultipleActivities: true);
        _rolloverDeliveryStatus = _rolloverModule.AddField("RO08", "What is the status of this activity?", FieldType.Select, 1);
        _rolloverDeliveryStatus.AddOption("on_track", "On track", 1);
        _rolloverDeliveryStatus.AddOption("off_track", "Off track", 2);

        Context.DataCollectionFormModules.AddRange(_focusModule, _sendModule, _rolloverModule);

        _deliveryPlan = DataCollection.New(
            id: DataCollectionId.New(),
            name: "2027 Delivery Plan",
            description: null,
            startDate: DateTime.UtcNow.AddDays(-30),
            endDate: DateTime.UtcNow.AddDays(30),
            type: DataCollectionType.DeliveryPlan);
        _deliveryPlan.AssignLocalAuthority(_organisation.Id);
        _deliveryPlan.AssignFormModule(_focusModule.Id);
        _deliveryPlan.AssignFormModule(_sendModule.Id);
        _deliveryPlan.AssignFormModule(_rolloverModule.Id);

        _managementInformation = DataCollection.New(
            id: DataCollectionId.New(),
            name: "2027 Management Information",
            description: null,
            startDate: DateTime.UtcNow.AddDays(-30),
            endDate: DateTime.UtcNow.AddDays(30),
            type: DataCollectionType.ManagementInformation);
        _managementInformation.AssignLocalAuthority(_organisation.Id);
        _managementInformation.AssignFormModule(_focusModule.Id);

        Context.DataCollections.AddRange(_deliveryPlan, _managementInformation);

        var focusSubmission = FormSubmission.Create(_focusModule.Id, _organisation.Id, _deliveryPlan.Id);
        focusSubmission.SetFieldValue(_focusQuestion, "Our stated focus");

        _onTrackActivity = FormSubmission.Create(_sendModule.Id, _organisation.Id, _deliveryPlan.Id, "Activity", null, "Activity 1");
        _onTrackActivity.SetFieldValue(_sendActivityStatus, "started");
        _onTrackActivity.SetFieldValue(_sendDeliveryStatus, "on_track");

        _rolloverActivity = FormSubmission.Create(_rolloverModule.Id, _organisation.Id, _deliveryPlan.Id, "Activity", null, "Rollover activity");
        _rolloverActivity.SetFieldValue(_rolloverDeliveryStatus, "on_track");

        Context.FormSubmissions.AddRange(focusSubmission, _onTrackActivity, _rolloverActivity);

        // Saved separately from _offTrackActivity below: CreatedAt comes from the database's
        // timezone('utc', now()) default, which is the transaction start time — every row in one
        // SaveChangesAsync call gets an identical timestamp. GetActivitySubmissions orders by
        // CreatedAt to match navigation position to the CSV export row order, so activities created
        // together in the same batch here (unlike the one-at-a-time real user flow) need separate
        // saves to get genuinely distinct timestamps and a deterministic order.
        await SaveChangesAsync();

        _offTrackActivity = FormSubmission.Create(_sendModule.Id, _organisation.Id, _deliveryPlan.Id, "Activity", null, "Activity 2");
        _offTrackActivity.SetFieldValue(_sendActivityStatus, "delayed");
        _offTrackActivity.SetFieldValue(_sendDeliveryStatus, "off_track");

        Context.FormSubmissions.Add(_offTrackActivity);

        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }

    private static string SectionRoute(Guid dataCollectionId, Guid localAuthorityId, Guid formModuleId) =>
        $"admin/data-collections/{dataCollectionId}/local-authorities/{localAuthorityId}/review/sections/{formModuleId}";

    private static string ActivityRoute(Guid dataCollectionId, Guid localAuthorityId, Guid formModuleId, Guid activityId) =>
        $"{SectionRoute(dataCollectionId, localAuthorityId, formModuleId)}/activities/{activityId}";

    private static async Task<string> ReadSubmissionJsonEntry(HttpResponseMessage response)
    {
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        var entry = archive.GetEntry("submission.json")!;
        await using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}

/// <summary>
/// Guards the module-code to status-field-key map against drift in DeliveryPlanFormModuleSeeder.
/// If a module is marked AllowsMultipleActivities without an entry here, its activity and delivery
/// status columns would silently render empty on the review screens.
/// </summary>
public class DeliveryPlanActivityStatusFieldsTests
{
    private static readonly string[] ActivityModuleCodes =
    [
        DataCollectionFormModuleCodes.DeliveryGrant,
        DataCollectionFormModuleCodes.Parenting,
        DataCollectionFormModuleCodes.Hle,
        DataCollectionFormModuleCodes.Send,
        DataCollectionFormModuleCodes.PnmhAndPir,
        DataCollectionFormModuleCodes.InfantFeeding,
        DataCollectionFormModuleCodes.HealthyBabiesOffer,
        DataCollectionFormModuleCodes.RolloverSpend,
    ];

    [Fact]
    public void EveryActivityModuleHasADeliveryStatusField()
    {
        foreach (var code in ActivityModuleCodes)
        {
            var keys = DeliveryPlanActivityStatusFields.ForModule(code);

            keys.Should().NotBeNull($"{code} allows multiple activities and needs status field keys");
            keys!.Value.DeliveryStatusFieldKey.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void OnlyRolloverSpendOmitsTheActivityStatusField()
    {
        var withoutActivityStatus = ActivityModuleCodes
            .Where(code => DeliveryPlanActivityStatusFields.ForModule(code)!.Value.ActivityStatusFieldKey == null)
            .ToList();

        withoutActivityStatus.Should().Equal(DataCollectionFormModuleCodes.RolloverSpend);
    }

    [Fact]
    public void NonActivityModulesHaveNoStatusFields()
    {
        DeliveryPlanActivityStatusFields.ForModule(DataCollectionFormModuleCodes.Focus).Should().BeNull();
    }
}