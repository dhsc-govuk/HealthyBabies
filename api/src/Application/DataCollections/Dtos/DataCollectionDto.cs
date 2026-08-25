using Domain.DataCollections;
using Domain.Organisations;

namespace Application.DataCollections.Dtos;

public record DataCollectionDto(
    Guid? Id,
    string Name,
    string? Description,
    DateTime? StartDate,
    DateTime? EndDate,
    bool IsSubmittedByAllLocalAuthorities,
    string Status,
    string Type,
    DateTime? CreatedAt,
    DateTime? UpdatedAt,
    string? CreatedByName,
    string? LastModifiedByName,
    IReadOnlyList<LocalAuthorityAssignmentDto>? LocalAuthorities = null,
    IReadOnlyList<FormModuleAssignmentDto>? FormModules = null,
    IReadOnlyList<LocalAuthoritySubmissionDto>? LocalAuthoritySubmissions = null)
{
    public static DataCollectionDto FromDomainModel(DataCollection input)
        => new(
            Id: input.Id.Value,
            Name: input.Name,
            Description: input.Description,
            StartDate: input.StartDate,
            EndDate: input.EndDate,
            IsSubmittedByAllLocalAuthorities: input.IsSubmittedByAllLocalAuthorities,
            Status: input.GetStatus().ToString(),
            Type: input.Type.Value,
            CreatedAt: input.CreatedAt,
            UpdatedAt: input.UpdatedAt,
            CreatedByName: input.CreatedBy?.Name.ToString(),
            LastModifiedByName: input.LastModifiedBy?.Name.ToString());

    public static DataCollectionDto FromDomainModelWithLocalAuthorities(DataCollection input)
        => new(
            Id: input.Id.Value,
            Name: input.Name,
            Description: input.Description,
            StartDate: input.StartDate,
            EndDate: input.EndDate,
            IsSubmittedByAllLocalAuthorities: input.IsSubmittedByAllLocalAuthorities,
            Status: input.GetStatus().ToString(),
            Type: input.Type.Value,
            CreatedAt: input.CreatedAt,
            UpdatedAt: input.UpdatedAt,
            CreatedByName: input.CreatedBy?.Name.ToString(),
            LastModifiedByName: input.LastModifiedBy?.Name.ToString(),
            LocalAuthorities: input.LocalAuthorities
                .Select(la => new LocalAuthorityAssignmentDto(
                    la.LocalAuthorityId.Value,
                    la.LocalAuthority?.Name ?? string.Empty,
                    la.AssignedAt,
                    la.EndDate))
                .ToList(),
            FormModules: input.FormModuleAssignments
                .Where(fm => fm.FormModule != null)
                .OrderBy(fm => fm.FormModule.SectionNumber)
                .Select(fm => new FormModuleAssignmentDto(
                    fm.FormModuleId.Value,
                    fm.FormModule.Code,
                    fm.FormModule.SectionNumber,
                    fm.FormModule.Name,
                    fm.FormModule.Description))
                .ToList());

    /// <param name="allLocalAuthorities">
    /// Every local authority known to the system. Only read when the collection is open to all of
    /// them; pass an empty list for a collection scoped to named authorities.
    /// </param>
    public static DataCollectionDto FromDomainModelWithSubmissions(
        DataCollection input,
        IReadOnlyList<DataCollectionSubmission> submissions,
        IReadOnlyList<Organisation> allLocalAuthorities)
    {
        // Build submission lookup by organisation ID
        var submissionsByOrgId = submissions.ToDictionary(s => s.OrganisationId.Value);

        // The set of local authorities the reviewer's progress list covers. A collection open to
        // every local authority holds no DataCollectionLocalAuthority rows at all - see the note on
        // DataCollection.UpdateLocalAuthorityEndDate - so its roster has to come from the
        // organisations. Reading it off the assignments left that list empty however many
        // authorities had submitted, and made a lone due date extension (which creates an
        // assignment row on demand) look like the only participant (BLUM-7626).
        var roster = new Dictionary<Guid, string>();

        if (input.IsSubmittedByAllLocalAuthorities)
        {
            // Every organisation, deliberately unfiltered: this mirrors
            // DataCollectionRepository.GetByOrganisationIdAsync, which is what actually decides who
            // sees and can submit the collection, and it ignores Organisation.IsActive - that flag
            // is display-only and gates nothing. Filtering here would drop authorities that can
            // still submit, only for them to reappear the moment they did.
            foreach (var localAuthority in allLocalAuthorities)
            {
                roster[localAuthority.Id.Value] = localAuthority.Name;
            }
        }
        else
        {
            foreach (var la in input.LocalAuthorities)
            {
                roster[la.LocalAuthorityId.Value] = la.LocalAuthority?.Name ?? string.Empty;
            }
        }

        // An assignment row is not a precondition for submitting, so an authority that has a
        // submission is listed even when the roster above misses it - the same union
        // SubmissionExportService.GetExportOrganisationsAsync applies to the consolidated export.
        foreach (var submission in submissions)
        {
            roster.TryAdd(submission.OrganisationId.Value, submission.Organisation?.Name ?? string.Empty);
        }

        var localAuthoritySubmissions = roster
            .Select(entry => submissionsByOrgId.TryGetValue(entry.Key, out var submission)
                ? LocalAuthoritySubmissionDto.FromDomainModel(submission)
                // No submission exists - return NotStarted status
                : new LocalAuthoritySubmissionDto(
                    Id: Guid.Empty,
                    LocalAuthorityId: entry.Key,
                    LocalAuthorityName: entry.Value,
                    Status: DataCollectionSubmissionStatus.NotStarted.ToString(),
                    SubmittedAt: null,
                    SubmittedBy: null))
            .OrderBy(la => la.LocalAuthorityName)
            .ToList();

        return new(
            Id: input.Id.Value,
            Name: input.Name,
            Description: input.Description,
            StartDate: input.StartDate,
            EndDate: input.EndDate,
            IsSubmittedByAllLocalAuthorities: input.IsSubmittedByAllLocalAuthorities,
            Status: input.GetStatus().ToString(),
            Type: input.Type.Value,
            CreatedAt: input.CreatedAt,
            UpdatedAt: input.UpdatedAt,
            CreatedByName: input.CreatedBy?.Name.ToString(),
            LastModifiedByName: input.LastModifiedBy?.Name.ToString(),
            LocalAuthorities: input.LocalAuthorities
                .Select(la => new LocalAuthorityAssignmentDto(
                    la.LocalAuthorityId.Value,
                    la.LocalAuthority?.Name ?? string.Empty,
                    la.AssignedAt,
                    la.EndDate))
                .ToList(),
            FormModules: input.FormModuleAssignments
                .Where(fm => fm.FormModule != null)
                .OrderBy(fm => fm.FormModule.SectionNumber)
                .Select(fm => new FormModuleAssignmentDto(
                    fm.FormModuleId.Value,
                    fm.FormModule.Code,
                    fm.FormModule.SectionNumber,
                    fm.FormModule.Name,
                    fm.FormModule.Description))
                .ToList(),
            LocalAuthoritySubmissions: localAuthoritySubmissions);
    }
}

public record LocalAuthorityAssignmentDto(
    Guid Id,
    string Name,
    DateTime AssignedAt,
    DateTime? EndDate);

public record LocalAuthoritySubmissionDto(
    Guid Id,
    Guid LocalAuthorityId,
    string LocalAuthorityName,
    string Status,
    DateTime? SubmittedAt,
    string? SubmittedBy)
{
    public static LocalAuthoritySubmissionDto FromDomainModel(DataCollectionSubmission submission)
        => new(
            Id: submission.Id.Value,
            LocalAuthorityId: submission.OrganisationId.Value,
            LocalAuthorityName: submission.Organisation?.Name ?? string.Empty,
            Status: submission.Status.ToString(),
            SubmittedAt: submission.SubmittedAt,
            SubmittedBy: submission.SubmittedBy?.Name.ToString());
}

public record FormModuleAssignmentDto(
    Guid Id,
    string Code,
    int SectionNumber,
    string Name,
    string? Description);

public record CreateDataCollectionDto(
    string Name,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    string Type = DataCollectionTypeConstants.DeliveryPlan,
    bool IsSubmittedByAllLocalAuthorities = true,
    bool SaveAsDraft = false,
    IReadOnlyList<Guid>? LocalAuthorityIds = null,
    IReadOnlyList<Guid>? FormModuleIds = null);

public record UpdateDataCollectionDto(
    string Name,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    string Type = DataCollectionTypeConstants.DeliveryPlan,
    bool IsSubmittedByAllLocalAuthorities = true,
    bool SaveAsDraft = false,
    IReadOnlyList<Guid>? LocalAuthorityIds = null,
    IReadOnlyList<Guid>? FormModuleIds = null);

public record UpdateDataCollectionLocalAuthoritiesDto(
    IReadOnlyList<Guid> LocalAuthorityIds);

public record UpdateLocalAuthorityEndDateDto(
    DateTime? EndDate);

public record UpdateDataCollectionDatesDto(
    DateTime? StartDate,
    DateTime? EndDate);

public record DuplicateDataCollectionDto(
    string? NewName);

public record LocalAuthoritySubmissionDetailDto(
    Guid Id,
    Guid DataCollectionId,
    string DataCollectionName,
    Guid LocalAuthorityId,
    string LocalAuthorityName,
    string Status,
    DateTime? SubmittedAt,
    string? SubmittedBy,
    string? SubmittedByEmail,
    LocalAuthorityContactDto? Contact,
    IReadOnlyList<LocalAuthorityFileDto> Files,
    string Type,
    IReadOnlyList<ReviewSectionSummaryDto> Sections,
    DateTime? ReviewedAt,
    string? ReviewedBy,
    string? ReviewNotes,
    DateTime? DueDate,
    IReadOnlyList<ReviewSectionSummaryDto> SectionsToReview,
    int PreviousVersionCount);

/// <summary>
/// Body of a "Reopen submission" request. The reason is mandatory and is emailed to the local
/// authority contact; ResubmitBy becomes that authority's new deadline. SectionsToReview is
/// advisory only - the local authority can still amend any section regardless of this list.
/// </summary>
public record ReopenSubmissionDto(
    string Reason,
    DateTime ResubmitBy,
    IReadOnlyList<Guid>? SectionsToReview = null);

/// <summary>
/// Body of an "Accept submission" request. The comment is optional, unlike Reopen's reason.
/// </summary>
public record ApproveSubmissionDto(string? Comment);

/// <summary>
/// Outcome of an approve or reopen action, used to render the confirmation banner.
/// </summary>
public record SubmissionReviewResultDto(
    Guid Id,
    Guid DataCollectionId,
    Guid LocalAuthorityId,
    string Status,
    DateTime? ReviewedAt,
    string? ReviewNotes)
{
    public static SubmissionReviewResultDto FromDomainModel(DataCollectionSubmission submission) =>
        new(
            submission.Id.Value,
            submission.DataCollectionId.Value,
            submission.OrganisationId.Value,
            submission.Status.ToString(),
            submission.ReviewedAt,
            submission.ReviewNotes);
}

/// <summary>
/// One reviewable section of a submitted Delivery Plan, used to build the section links on the
/// local authority submission summary page. Populated for delivery-plan collections only; a
/// management-information collection returns an empty list.
/// </summary>
public record ReviewSectionSummaryDto(
    Guid FormModuleId,
    string Code,
    int SectionNumber,
    string Name,
    bool AllowsMultipleActivities);

public record LocalAuthorityContactDto(
    string OrganisationName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string Postcode,
    string Country,
    string? Phone,
    string? Email);

/// <param name="Status">Short label shown next to the file name, e.g. "Submission pending". Null when downloadable.</param>
public record LocalAuthorityFileDto(
    string Name,
    string Description,
    string? DownloadUrl,
    string? DownloadSize,
    string? Status = null);