using Domain.DataCollections;

namespace Application.DataCollections.Dtos;

public record DataCollectionDto(
    Guid? Id,
    string Name,
    string? Description,
    DateTime? StartDate,
    DateTime? EndDate,
    bool IsSubmittedByAllLocalAuthorities,
    string Status,
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

    public static DataCollectionDto FromDomainModelWithSubmissions(
        DataCollection input,
        IReadOnlyList<DataCollectionSubmission> submissions)
    {
        // Build submission lookup by organisation ID
        var submissionsByOrgId = submissions.ToDictionary(s => s.OrganisationId.Value);

        // Create LocalAuthoritySubmissionDto for ALL assigned local authorities
        var localAuthoritySubmissions = input.LocalAuthorities
            .Select(la =>
            {
                if (submissionsByOrgId.TryGetValue(la.LocalAuthorityId.Value, out var submission))
                {
                    return LocalAuthoritySubmissionDto.FromDomainModel(submission);
                }

                // No submission exists - return NotStarted status
                return new LocalAuthoritySubmissionDto(
                    Id: Guid.Empty,
                    LocalAuthorityId: la.LocalAuthorityId.Value,
                    LocalAuthorityName: la.LocalAuthority?.Name ?? string.Empty,
                    Status: DataCollectionSubmissionStatus.NotStarted.ToString(),
                    SubmittedAt: null,
                    SubmittedBy: null);
            })
            .ToList();

        return new(
            Id: input.Id.Value,
            Name: input.Name,
            Description: input.Description,
            StartDate: input.StartDate,
            EndDate: input.EndDate,
            IsSubmittedByAllLocalAuthorities: input.IsSubmittedByAllLocalAuthorities,
            Status: input.GetStatus().ToString(),
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
    bool IsSubmittedByAllLocalAuthorities = true,
    bool SaveAsDraft = false,
    IReadOnlyList<Guid>? LocalAuthorityIds = null,
    IReadOnlyList<Guid>? FormModuleIds = null);

public record UpdateDataCollectionDto(
    string Name,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    bool IsSubmittedByAllLocalAuthorities = true,
    bool SaveAsDraft = false,
    IReadOnlyList<Guid>? LocalAuthorityIds = null,
    IReadOnlyList<Guid>? FormModuleIds = null);

public record UpdateDataCollectionLocalAuthoritiesDto(
    IReadOnlyList<Guid> LocalAuthorityIds);

public record UpdateLocalAuthorityEndDateDto(
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
    IReadOnlyList<LocalAuthorityFileDto> Files);

public record LocalAuthorityContactDto(
    string OrganisationName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string Postcode,
    string Country,
    string? Phone,
    string? Email);

public record LocalAuthorityFileDto(
    string Name,
    string Description,
    string? CsvUrl,
    string? CsvSize,
    string? JsonUrl,
    string? JsonSize);