using Application.DataCollections.Dtos;

namespace Application.Submissions.Dtos;

/// <summary>
/// One row on the "Previous submissions" list - a past round DHSC rejected, frozen at the moment
/// of rejection. Read-only: nothing here round-trips back to the API.
/// </summary>
public record SubmissionVersionSummaryDto(
    int VersionNumber,
    string? SubmittedBy,
    DateTime? SubmittedAt);

/// <summary>
/// The header for one previous submission - who submitted it, DHSC's reason for rejecting it, and
/// the sections it contains. Built from the version's own stored content, not live data, so it
/// keeps showing what was true at the time even if sections are renamed or reassigned later.
/// </summary>
public record SubmissionVersionDetailDto(
    int VersionNumber,
    string? SubmittedBy,
    string? SubmittedByEmail,
    DateTime? SubmittedAt,
    string? ReviewNotes,
    IReadOnlyList<ReviewSectionSummaryDto> SectionsToReview,
    IReadOnlyList<ReviewSectionSummaryDto> Sections);

/// <summary>
/// One section's answers within a previous submission. Mirrors the live ReviewSectionDetailDto
/// shape so the client renders both with the same components: a module that allows multiple
/// activities lists them in <see cref="Activities"/> (AnswerGroups is empty) and links through to
/// SubmissionVersionActivityDetailDto for each one, exactly like the live admin review; every
/// other module's answers are in <see cref="AnswerGroups"/> directly.
/// </summary>
public record SubmissionVersionSectionDetailDto(
    Guid FormModuleId,
    string Code,
    int SectionNumber,
    string Name,
    string DataCollectionName,
    bool AllowsMultipleActivities,
    IReadOnlyList<ReviewAnswerGroupDto> AnswerGroups,
    IReadOnlyList<ReviewActivityRowDto> Activities,
    ReviewNavigationDto Navigation);

/// <summary>
/// One activity's own answers within a previous submission's activity-based section. Mirrors the
/// live ReviewActivityDetailDto shape.
/// </summary>
public record SubmissionVersionActivityDetailDto(
    Guid ActivityId,
    string ActivityName,
    Guid FormModuleId,
    string ModuleName,
    int SectionNumber,
    string DataCollectionName,
    IReadOnlyList<ReviewAnswerGroupDto> AnswerGroups,
    ReviewNavigationDto Navigation);