namespace Application.DataCollections.Dtos;

/// <summary>
/// Read-only projections backing the admin review of a submitted Delivery Plan. Every type here is
/// display-shaped and carries no identifiers the screens do not need — these views are read-only, so
/// nothing round-trips back to the API.
/// </summary>
public record ReviewSectionDetailDto(
    Guid FormModuleId,
    string Code,
    int SectionNumber,
    string Name,
    string? Description,
    string DataCollectionName,
    string LocalAuthorityName,
    bool AllowsMultipleActivities,
    IReadOnlyList<ReviewAnswerGroupDto> AnswerGroups,
    IReadOnlyList<ReviewActivityRowDto> Activities,
    ReviewNavigationDto Navigation);

public record ReviewActivityDetailDto(
    Guid ActivityId,
    string ActivityName,
    Guid FormModuleId,
    string ModuleName,
    int SectionNumber,
    string DataCollectionName,
    string LocalAuthorityName,
    IReadOnlyList<ReviewAnswerGroupDto> AnswerGroups,
    ReviewNavigationDto Navigation);

/// <summary>
/// Answers grouped by the form section they belong to, so each group renders as one summary card.
/// A module with no form sections yields a single group with a null title.
/// </summary>
public record ReviewAnswerGroupDto(
    Guid? SectionId,
    string? Title,
    IReadOnlyList<ReviewAnswerDto> Answers);

public record ReviewAnswerDto(
    string QuestionCode,
    string Question,
    string? Value,
    string FieldType);

/// <param name="ActivityStatusValue">
/// Raw option value, e.g. "on_track". Supplied alongside the label so the client can pick a tag
/// colour without matching on English text. Null when the module has no such question.
/// </param>
public record ReviewActivityRowDto(
    Guid ActivityId,
    string ActivityName,
    string? ActivityStatus,
    string? ActivityStatusValue,
    string? DeliveryStatus,
    string? DeliveryStatusValue);

public record ReviewNavigationDto(
    int Position,
    int Total,
    ReviewNavigationLinkDto? Previous,
    ReviewNavigationLinkDto? Next);

public record ReviewNavigationLinkDto(
    Guid Id,
    string Name,
    int Position);