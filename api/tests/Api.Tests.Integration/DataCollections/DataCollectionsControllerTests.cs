using System.Net;
using System.Net.Http.Json;
using Application.DataCollections.Dtos;
using Domain.DataCollections;
using Domain.Organisations;
using Domain.Users;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.DataCollections;

public class DataCollectionsControllerTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    private readonly User _adminUser = AdminsData.MainAdmin;

    private readonly DataCollection _mainDataCollection = DataCollectionsData.MainDataCollection;
    private readonly DataCollection _secondDataCollection = DataCollectionsData.SecondDataCollection;

    private readonly Organisation _mainOrganisation = OrganisationsData.MainOrganisation;
    private readonly Organisation _secondOrganisation = OrganisationsData.SecondOrganisation;

    private readonly string _mainCreateRoute = "admin/data-collections/create";
    private readonly string _mainGetAllRoute = "admin/data-collections";

    private static string B64(string value) => "b64:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));
    private static string? B64N(string? value) => value is not null ? "b64:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value)) : null;

    [Fact]
    public async Task ShouldCreateDataCollection()
    {
        // Arrange
        var request = new CreateDataCollectionDto(
            Name: B64(_mainDataCollection.Name),
            Description: B64N(_mainDataCollection.Description),
            StartDate: _mainDataCollection.StartDate,
            EndDate: _mainDataCollection.EndDate);

        // Act
        var response = await Client.PostAsJsonAsync(_mainCreateRoute, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var dataCollection = await response.ToResponseModel<DataCollectionDto>();
        Context.DataCollections.Any(x => x.Id == new DataCollectionId(dataCollection.Id!.Value)).Should().BeTrue();
    }

    [Fact]
    public async Task ShouldNotCreateDataCollectionBecauseNameDuplication()
    {
        // Arrange
        var request = new CreateDataCollectionDto(
            Name: B64(_secondDataCollection.Name),
            Description: B64N(_secondDataCollection.Description),
            StartDate: _secondDataCollection.StartDate,
            EndDate: _secondDataCollection.EndDate);

        // Act
        var response = await Client.PostAsJsonAsync(_mainCreateRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ShouldUpdateDataCollection()
    {
        // Arrange
        const string newName = "Updated Data Collection Name";
        var request = new UpdateDataCollectionDto(
            Name: B64(newName),
            Description: B64N(_secondDataCollection.Description),
            StartDate: _secondDataCollection.StartDate,
            EndDate: _secondDataCollection.EndDate);

        var route = $"{_mainGetAllRoute}/{_secondDataCollection.Id.Value}/edit";

        // Act
        var response = await Client.PutAsJsonAsync(route, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var dataCollection = await response.ToResponseModel<DataCollectionDto>();
        var updated = Context.DataCollections.First(x => x.Id == new DataCollectionId(dataCollection.Id!.Value));
        updated.Name.Should().Be(newName);

        // Documents that this endpoint replaces the whole collection: omitting SaveAsDraft publishes a
        // draft. Callers that only need to move a date must use PATCH {id}/dates instead.
        updated.IsDraft.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldGetAllDataCollections()
    {
        // Act
        var response = await Client.GetAsync(_mainGetAllRoute);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var dataCollections = await response.ToResponseModel<List<DataCollectionDto>>();
        dataCollections.Should().NotBeNull();
        dataCollections.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ShouldGetDataCollectionById()
    {
        // Arrange
        var route = $"{_mainGetAllRoute}/{_secondDataCollection.Id.Value}";

        // Act
        var response = await Client.GetAsync(route);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var dataCollection = await response.ToResponseModel<DataCollectionDto>();
        dataCollection.Should().NotBeNull();
        dataCollection.Name.Should().Be(_secondDataCollection.Name);
    }

    [Fact]
    public async Task ShouldDeleteDataCollection()
    {
        // Arrange
        var route = $"{_mainGetAllRoute}/{_secondDataCollection.Id.Value}";

        // Act
        var response = await Client.DeleteAsync(route);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var deletedDataCollection = Context.DataCollections
            .IgnoreQueryFilters()
            .First(x => x.Id == _secondDataCollection.Id);
        deletedDataCollection.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldGetDataCollectionWithLocalAuthorities()
    {
        // Arrange
        var route = $"{_mainGetAllRoute}/{_secondDataCollection.Id.Value}/local-authorities";

        // Act
        var response = await Client.GetAsync(route);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var dataCollection = await response.ToResponseModel<DataCollectionDto>();
        dataCollection.Should().NotBeNull();
        dataCollection.LocalAuthorities.Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldUpdateLocalAuthorities()
    {
        // Arrange
        var route = $"{_mainGetAllRoute}/{_secondDataCollection.Id.Value}/local-authorities";
        var request = new UpdateDataCollectionLocalAuthoritiesDto(
            LocalAuthorityIds: new List<Guid> { _mainOrganisation.Id.Value, _secondOrganisation.Id.Value });

        // Act
        var response = await Client.PutAsJsonAsync(route, request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var dataCollection = await response.ToResponseModel<DataCollectionDto>();
        dataCollection.LocalAuthorities.Should().HaveCount(2);
    }

    [Fact]
    public async Task ShouldRemoveLocalAuthorityFromDataCollection()
    {
        // Arrange
        _secondDataCollection.AssignLocalAuthority(_mainOrganisation.Id);
        await SaveChangesAsync();

        var route = $"{_mainGetAllRoute}/{_secondDataCollection.Id.Value}/local-authorities/{_mainOrganisation.Id.Value}";

        // Act
        var response = await Client.DeleteAsync(route);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var dataCollection = await response.ToResponseModel<DataCollectionDto>();
        dataCollection.LocalAuthorities.Should().BeEmpty();
    }

    /// <summary>
    /// BLUM-6773. A local authority with no DataCollectionSubmission row - status "Not started", the
    /// normal state for most authorities on a newly opened window - returned 500 instead of its details
    /// page. GetLocalAuthoritySubmission resolved the optional submission with LanguageExt's Match and a
    /// None branch that returned null; Match puts its result through Check.NullReturn, which raises
    /// ResultIsNullException on null, so the request died in the unhandled exception handler and the
    /// client rendered "Failed to load local authority submission details." MatchUnsafe is the variant
    /// that permits null.
    ///
    /// The production fix is not in the commit that added this test: d285b58 had already swapped that
    /// line to MatchUnsafe, incidentally, while adding the Delivery Plan review pages. This test is the
    /// regression guard that change never got. Keep it - it is the only one that names the
    /// no-submission path.
    /// </summary>
    [Fact]
    public async Task ShouldGetLocalAuthoritySubmissionWhenLocalAuthorityHasNotStarted()
    {
        // Arrange
        var dataCollection = await AddTestCollectionAsync(_mainOrganisation.Id);

        // Act
        var response = await Client.GetAsync(LocalAuthoritySubmissionRoute(dataCollection.Id, _mainOrganisation.Id));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var submission = await response.ToResponseModel<LocalAuthoritySubmissionDetailDto>();
        submission.Status.Should().Be(DataCollectionSubmissionStatus.NotStarted.ToString());
        submission.LocalAuthorityName.Should().Be(_mainOrganisation.Name);
        submission.DataCollectionName.Should().Be(dataCollection.Name);
        submission.SubmittedAt.Should().BeNull();
        submission.SubmittedBy.Should().BeNull();
        submission.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldGetLocalAuthoritySubmissionWhenSubmissionExists()
    {
        // Arrange
        var dataCollection = await AddTestCollectionAsync(_mainOrganisation.Id);
        var submission = DataCollectionSubmission.Create(dataCollection.Id, _mainOrganisation.Id);
        submission.Submit(_adminUser.Id);
        Context.DataCollectionSubmissions.Add(submission);
        await SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(LocalAuthoritySubmissionRoute(dataCollection.Id, _mainOrganisation.Id));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var detail = await response.ToResponseModel<LocalAuthoritySubmissionDetailDto>();
        detail.Status.Should().Be(DataCollectionSubmissionStatus.Submitted.ToString());
        detail.SubmittedAt.Should().NotBeNull();

        // Download link is attached once the authority has submitted
        detail.Files.Should().ContainSingle();
        detail.Files.Single().DownloadUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ShouldNotGetLocalAuthoritySubmissionBecauseLocalAuthorityDoesNotExist()
    {
        // Arrange
        var dataCollection = await AddTestCollectionAsync(_mainOrganisation.Id);
        var route = LocalAuthoritySubmissionRoute(dataCollection.Id, new OrganisationId(Guid.NewGuid()));

        // Act
        var response = await Client.GetAsync(route);

        // Assert - a genuine miss is a 404, so a 500 from this endpoint stays distinguishable in CI
        // rather than blending into the same message on the client
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldUpdateEndDateOnly()
    {
        // Arrange
        var dataCollection = await AddTestCollectionAsync(_mainOrganisation.Id);
        var newEndDate = dataCollection.EndDate.AddMonths(1);
        var request = new UpdateDataCollectionDatesDto(StartDate: null, EndDate: newEndDate);

        // Act
        var response = await Client.PatchAsJsonAsync(DatesRoute(dataCollection.Id), request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var updated = await GetWithLocalAuthoritiesAsync(dataCollection.Id);
        updated.EndDate.Should().BeCloseTo(newEndDate, TimeSpan.FromSeconds(1));

        // Everything PUT {id}/edit would have destroyed must survive
        updated.IsDraft.Should().BeTrue();
        updated.StartDate.Should().BeCloseTo(dataCollection.StartDate, TimeSpan.FromSeconds(1));
        updated.Name.Should().Be(dataCollection.Name);
        updated.LocalAuthorities.Should().HaveCount(1);
    }

    [Fact]
    public async Task ShouldUpdateBothDates()
    {
        // Arrange
        var dataCollection = await AddTestCollectionAsync();
        var newStartDate = dataCollection.StartDate.AddDays(2);
        var newEndDate = dataCollection.EndDate.AddDays(10);
        var request = new UpdateDataCollectionDatesDto(StartDate: newStartDate, EndDate: newEndDate);

        // Act
        var response = await Client.PatchAsJsonAsync(DatesRoute(dataCollection.Id), request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var updated = Context.DataCollections.First(x => x.Id == dataCollection.Id);
        updated.StartDate.Should().BeCloseTo(newStartDate, TimeSpan.FromSeconds(1));
        updated.EndDate.Should().BeCloseTo(newEndDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ShouldCascadeEndDateToUnextendedLocalAuthorityAssignments()
    {
        // Arrange - the assignment snapshots the collection end date when it is created
        var dataCollection = await AddTestCollectionAsync(_mainOrganisation.Id);
        var newEndDate = dataCollection.EndDate.AddMonths(2);
        var request = new UpdateDataCollectionDatesDto(StartDate: null, EndDate: newEndDate);

        // Act
        var response = await Client.PatchAsJsonAsync(DatesRoute(dataCollection.Id), request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var updated = await GetWithLocalAuthoritiesAsync(dataCollection.Id);
        updated.LocalAuthorities.Single().EndDate.Should().BeCloseTo(newEndDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ShouldCascadeEndDateWhenEditingWholeDataCollection()
    {
        // Arrange - the edit wizard moves the end date too, so it must cascade like PATCH {id}/dates
        var dataCollection = await AddTestCollectionAsync(_mainOrganisation.Id);
        var newEndDate = dataCollection.EndDate.AddMonths(1);
        var request = new UpdateDataCollectionDto(
            Name: B64(dataCollection.Name),
            Description: B64N(dataCollection.Description),
            StartDate: dataCollection.StartDate,
            EndDate: newEndDate,
            IsSubmittedByAllLocalAuthorities: false,
            SaveAsDraft: true,
            LocalAuthorityIds: new List<Guid> { _mainOrganisation.Id.Value });

        // Act
        var response = await Client.PutAsJsonAsync($"{_mainGetAllRoute}/{dataCollection.Id.Value}/edit", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var updated = await GetWithLocalAuthoritiesAsync(dataCollection.Id);
        updated.LocalAuthorities.Single().EndDate.Should().BeCloseTo(newEndDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ShouldPreserveIndividuallyExtendedLocalAuthorityEndDate()
    {
        // Arrange - this local authority was granted its own extension, so it must not be rolled forward
        var dataCollection = await AddTestCollectionAsync(_mainOrganisation.Id);
        var extendedEndDate = dataCollection.EndDate.AddDays(45);
        dataCollection.UpdateLocalAuthorityEndDate(_mainOrganisation.Id, extendedEndDate);
        Context.DataCollections.Update(dataCollection);
        await SaveChangesAsync();

        var request = new UpdateDataCollectionDatesDto(StartDate: null, EndDate: dataCollection.EndDate.AddDays(3));

        // Act
        var response = await Client.PatchAsJsonAsync(DatesRoute(dataCollection.Id), request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var updated = await GetWithLocalAuthoritiesAsync(dataCollection.Id);
        updated.LocalAuthorities.Single().EndDate.Should().BeCloseTo(extendedEndDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ShouldNotUpdateDatesBecauseEndDateBeforeStartDate()
    {
        // Arrange
        var dataCollection = await AddTestCollectionAsync();
        var request = new UpdateDataCollectionDatesDto(
            StartDate: null,
            EndDate: dataCollection.StartDate.AddDays(-1));

        // Act
        var response = await Client.PatchAsJsonAsync(DatesRoute(dataCollection.Id), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        Context.DataCollections.First(x => x.Id == dataCollection.Id)
            .EndDate.Should().BeCloseTo(dataCollection.EndDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ShouldNotUpdateDatesBecauseDataCollectionDoesNotExist()
    {
        // Arrange
        var route = DatesRoute(new DataCollectionId(Guid.NewGuid()));
        var request = new UpdateDataCollectionDatesDto(StartDate: null, EndDate: DateTime.UtcNow.AddMonths(1));

        // Act
        var response = await Client.PatchAsJsonAsync(route, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// BLUM-7626. A collection open to every local authority holds no DataCollectionLocalAuthority
    /// rows at all, and the reviewer's progress list was built by walking those rows - so an
    /// authority that had submitted was missing from it entirely, and so was every authority still
    /// outstanding.
    /// </summary>
    [Fact]
    public async Task ShouldListSubmittedLocalAuthorityWhenCollectionIsOpenToAllLocalAuthorities()
    {
        // Arrange - no assigned authorities, so the collection is open to all of them
        var dataCollection = await AddTestCollectionAsync();
        var submission = DataCollectionSubmission.Create(dataCollection.Id, _mainOrganisation.Id);
        submission.Submit(_adminUser.Id);
        Context.DataCollectionSubmissions.Add(submission);
        await SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(CollectionRoute(dataCollection.Id));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var dto = await response.ToResponseModel<DataCollectionDto>();
        var localAuthoritySubmissions = dto.LocalAuthoritySubmissions.Should().NotBeNull().And.Subject!.ToList();

        localAuthoritySubmissions.Single(la => la.LocalAuthorityId == _mainOrganisation.Id.Value)
            .Status.Should().Be(DataCollectionSubmissionStatus.Submitted.ToString());

        // Every other authority is still listed, as outstanding
        localAuthoritySubmissions.Single(la => la.LocalAuthorityId == _secondOrganisation.Id.Value)
            .Status.Should().Be(DataCollectionSubmissionStatus.NotStarted.ToString());
    }

    /// <summary>
    /// BLUM-7626. Granting one authority its own deadline creates its assignment row on demand
    /// (DataCollection.UpdateLocalAuthorityEndDate), which used to make it the sole entry in the
    /// reviewer's list - reading as though it, and only it, had taken part.
    /// </summary>
    [Fact]
    public async Task ShouldNotReportLocalAuthorityAsSubmittedAfterDueDateExtension()
    {
        // Arrange
        var dataCollection = await AddTestCollectionAsync();
        var extendedEndDate = dataCollection.EndDate.AddDays(30);

        // Act
        var extensionResponse = await Client.PatchAsJsonAsync(
            EndDateRoute(dataCollection.Id, _secondOrganisation.Id),
            new UpdateLocalAuthorityEndDateDto(extendedEndDate));
        var response = await Client.GetAsync(CollectionRoute(dataCollection.Id));

        // Assert
        extensionResponse.IsSuccessStatusCode.Should().BeTrue();
        response.IsSuccessStatusCode.Should().BeTrue();
        var dto = await response.ToResponseModel<DataCollectionDto>();
        var localAuthoritySubmissions = dto.LocalAuthoritySubmissions.Should().NotBeNull().And.Subject!.ToList();

        localAuthoritySubmissions.Single(la => la.LocalAuthorityId == _secondOrganisation.Id.Value)
            .Status.Should().Be(DataCollectionSubmissionStatus.NotStarted.ToString());
        localAuthoritySubmissions.Should().Contain(la => la.LocalAuthorityId == _mainOrganisation.Id.Value);
    }

    [Fact]
    public async Task ShouldListOnlyAssignedLocalAuthoritiesWhenCollectionIsScopedToNamedAuthorities()
    {
        // Arrange
        var dataCollection = await AddTestCollectionAsync(_mainOrganisation.Id);

        // Act
        var response = await Client.GetAsync(CollectionRoute(dataCollection.Id));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var dto = await response.ToResponseModel<DataCollectionDto>();
        dto.LocalAuthoritySubmissions.Should().ContainSingle()
            .Which.LocalAuthorityId.Should().Be(_mainOrganisation.Id.Value);
    }

    /// <summary>
    /// BLUM-7626. Assignments double as the store for a single authority's own deadline, so wiping
    /// them when a collection switches to "all local authorities" silently reverted every due date
    /// extension - and every resubmit-by date set by a reopen - to the collection-wide end date.
    /// </summary>
    [Fact]
    public async Task ShouldKeepExtendedEndDateWhenCollectionSwitchesToAllLocalAuthorities()
    {
        // Arrange - one authority is individually extended, the other is only in scope
        var dataCollection = await AddTestCollectionAsync(_mainOrganisation.Id, _secondOrganisation.Id);
        var extendedEndDate = dataCollection.EndDate.AddDays(45);
        var extensionResponse = await Client.PatchAsJsonAsync(
            EndDateRoute(dataCollection.Id, _mainOrganisation.Id),
            new UpdateLocalAuthorityEndDateDto(extendedEndDate));
        extensionResponse.IsSuccessStatusCode.Should().BeTrue();

        var request = new UpdateDataCollectionDto(
            Name: B64(dataCollection.Name),
            Description: B64N(dataCollection.Description),
            StartDate: dataCollection.StartDate,
            EndDate: dataCollection.EndDate,
            IsSubmittedByAllLocalAuthorities: true,
            SaveAsDraft: true);

        // Act
        var response = await Client.PutAsJsonAsync($"{_mainGetAllRoute}/{dataCollection.Id.Value}/edit", request);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var updated = await GetWithLocalAuthoritiesAsync(dataCollection.Id);
        updated.LocalAuthorities.Should().ContainSingle();
        updated.LocalAuthorities.Single().LocalAuthorityId.Should().Be(_mainOrganisation.Id);
        updated.LocalAuthorities.Single().EndDate.Should().BeCloseTo(extendedEndDate, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// An assignment left on a date the collection has since moved past is a stale snapshot, not an
    /// extension - snapshots only started rolling forward with CascadeEndDateToLocalAuthorities and
    /// nothing backfilled the older rows. Keeping one would pin its authority to an earlier deadline
    /// than every other authority on a collection that is still open.
    /// </summary>
    [Fact]
    public async Task ShouldDropStaleAssignmentSnapshotWhenCollectionSwitchesToAllLocalAuthorities()
    {
        // Arrange
        var dataCollection = await AddTestCollectionAsync(_mainOrganisation.Id);
        dataCollection.UpdateLocalAuthorityEndDate(_mainOrganisation.Id, dataCollection.EndDate.AddDays(-30));
        Context.DataCollections.Update(dataCollection);
        await SaveChangesAsync();

        var request = new UpdateDataCollectionDto(
            Name: B64(dataCollection.Name),
            Description: B64N(dataCollection.Description),
            StartDate: dataCollection.StartDate,
            EndDate: dataCollection.EndDate,
            IsSubmittedByAllLocalAuthorities: true,
            SaveAsDraft: true);

        // Act
        var response = await Client.PutAsJsonAsync($"{_mainGetAllRoute}/{dataCollection.Id.Value}/edit", request);

        // Assert - the authority falls back to the collection-wide deadline like everybody else
        response.IsSuccessStatusCode.Should().BeTrue();
        var updated = await GetWithLocalAuthoritiesAsync(dataCollection.Id);
        updated.LocalAuthorities.Should().BeEmpty();
    }

    /// <summary>
    /// Organisation.IsActive is display-only and gates nothing - GetByOrganisationIdAsync, which
    /// decides who can actually see and submit a collection open to all local authorities, ignores
    /// it. So an inactive authority stays on the reviewer's list rather than surfacing only if and
    /// when it submits.
    /// </summary>
    [Fact]
    public async Task ShouldListInactiveLocalAuthorityWhenCollectionIsOpenToAllLocalAuthorities()
    {
        // Arrange
        var inactiveOrganisation = Organisation.New(
            id: OrganisationId.New(),
            name: "Inactive",
            oNSCode: "E00000003",
            isActive: false);
        Context.Organisations.Add(inactiveOrganisation);
        await SaveChangesAsync();
        var dataCollection = await AddTestCollectionAsync();

        // Act
        var response = await Client.GetAsync(CollectionRoute(dataCollection.Id));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var dto = await response.ToResponseModel<DataCollectionDto>();
        dto.LocalAuthoritySubmissions.Should().Contain(la => la.LocalAuthorityId == inactiveOrganisation.Id.Value);
    }

    private string CollectionRoute(DataCollectionId dataCollectionId) =>
        $"{_mainGetAllRoute}/{dataCollectionId.Value}";

    private string EndDateRoute(DataCollectionId dataCollectionId, OrganisationId localAuthorityId) =>
        $"{_mainGetAllRoute}/{dataCollectionId.Value}/local-authorities/{localAuthorityId.Value}/end-date";

    private string DatesRoute(DataCollectionId dataCollectionId) =>
        $"{_mainGetAllRoute}/{dataCollectionId.Value}/dates";

    private string LocalAuthoritySubmissionRoute(DataCollectionId dataCollectionId, OrganisationId localAuthorityId) =>
        $"{_mainGetAllRoute}/{dataCollectionId.Value}/local-authorities/{localAuthorityId.Value}";

    // Built per test rather than reusing the shared static test data, because the entities in Tests.Data
    // are static and assigning a local authority to one leaks into every later test in the run.
    private async Task<DataCollection> AddTestCollectionAsync(params OrganisationId[] localAuthorityIds)
    {
        var dataCollection = DataCollection.New(
            id: DataCollectionId.New(),
            name: $"Test Collection {Guid.NewGuid()}",
            description: "Test collection description",
            startDate: DateTime.UtcNow.AddDays(7),
            endDate: DateTime.UtcNow.AddMonths(6),
            isSubmittedByAllLocalAuthorities: localAuthorityIds.Length == 0,
            saveAsDraft: true);

        foreach (var localAuthorityId in localAuthorityIds)
        {
            dataCollection.AssignLocalAuthority(localAuthorityId);
        }

        Context.DataCollections.Add(dataCollection);
        await SaveChangesAsync();

        return dataCollection;
    }

    private async Task<DataCollection> GetWithLocalAuthoritiesAsync(DataCollectionId dataCollectionId) =>
        await Context.DataCollections
            .AsNoTracking()
            .Include(x => x.LocalAuthorities)
            .FirstAsync(x => x.Id == dataCollectionId);

    public async Task InitializeAsync()
    {
        Context.Users.Add(_adminUser);
        Context.Organisations.Add(_mainOrganisation);
        Context.Organisations.Add(_secondOrganisation);
        Context.DataCollections.Add(_secondDataCollection);

        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }
}