namespace Application.Submissions.Dtos;

public record SubmissionDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    int DaysRemaining,
    bool IsWindowClosed);

public record SubmissionDetailDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    int DaysRemaining,
    bool IsWindowClosed,
    IReadOnlyList<SubmissionFormModuleDto> FormModules);

public record SubmissionFormModuleDto(
    Guid Id,
    string Code,
    int SectionNumber,
    string Name,
    string? Description,
    string Status,
    bool AllowsMultipleActivities,
    IReadOnlyList<SubmissionSectionDto> Sections);

public record SubmissionSectionDto(
    Guid Id,
    int Number,
    string Title,
    string? Description,
    string Status);

public record SectionDetailDto(
    Guid Id,
    Guid SubmissionId,
    int Number,
    string Title,
    string? Description,
    string Status,
    IReadOnlyList<SectionFieldDto> Fields);

public record SectionFieldDto(
    Guid Id,
    string Code,
    string Label,
    string? HelpText,
    string FieldType,
    bool IsRequired,
    int DisplayOrder,
    string? Value,
    string? DisplayValue,
    IReadOnlyList<SectionFieldOptionDto> Options);

public record SectionFieldOptionDto(
    string Value,
    string Label,
    int DisplayOrder);

public record SaveSectionRequest(
    Dictionary<string, string?> FieldValues,
    bool MarkComplete);

public record FormModuleDetailDto(
    Guid Id,
    Guid SubmissionId,
    string SubmissionName,
    string Code,
    int SectionNumber,
    string Name,
    string? Description,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    int CurrentStep,
    int TotalSteps,
    bool AllowsMultipleActivities,
    IReadOnlyList<FormModuleSectionDto> Sections,
    IReadOnlyList<FormModuleFieldDto> Fields);

public record FormModuleSectionDto(
    Guid Id,
    int SectionNumber,
    string Title,
    string? Description,
    string? HelpText,
    string? HelpUrl);

public record FormModuleFieldDto(
    Guid Id,
    string Code,
    string Label,
    string? HelpText,
    string FieldType,
    bool IsRequired,
    int DisplayOrder,
    int? StepNumber,
    string? Value,
    string? DisplayValue,
    string? ConditionalRules,
    string? Configuration,
    string? Placeholder,
    IReadOnlyList<SectionFieldOptionDto> Options);

public record SaveFormModuleRequest(
    Dictionary<string, string?> FieldValues,
    bool MarkComplete,
    int? CurrentStep);

public record FileUploadResultDto(
    string FileName,
    string BlobUrl);

public record ServiceUsersModuleDetailDto(
    Guid Id,
    Guid SubmissionId,
    string SubmissionName,
    string Code,
    int SectionNumber,
    string Name,
    string? Description,
    string Status,
    int TotalServices,
    int CompletedServices,
    IReadOnlyList<ServiceFormStatusDto> Services);

public record ServiceFormStatusDto(
    Guid ServiceId,
    string ServiceName,
    string FundingType,
    string Status);

public record ServiceFormDetailDto(
    Guid ServiceId,
    string ServiceName,
    string FundingType,
    Guid FormModuleId,
    string Status,
    IReadOnlyList<FormModuleSectionDto> Sections,
    IReadOnlyList<FormModuleFieldDto> Fields,
    IReadOnlyDictionary<string, string> ServiceAnswers);

public record SaveServiceFormRequest(
    Dictionary<string, string?> FieldValues,
    bool MarkComplete);

public record WiderServiceUsersModuleDetailDto(
    Guid Id,
    Guid SubmissionId,
    string SubmissionName,
    string Code,
    int SectionNumber,
    string Name,
    string? Description,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    int TotalCategories,
    int CompletedCategories,
    IReadOnlyList<WiderServiceCategoryStatusDto> Categories);

public record WiderServiceCategoryStatusDto(
    Guid ServiceCategoryId,
    string CategoryName,
    string Status,
    int? UserCount);

public record WiderServiceCategoryFormDto(
    Guid ServiceCategoryId,
    string CategoryName,
    string Status,
    string StartDate,
    string EndDate,
    IReadOnlyList<FormModuleSectionDto> Sections,
    IReadOnlyList<FormModuleFieldDto> Fields);

public record SaveWiderServiceCategoryFormRequest(
    Dictionary<string, string?> FieldValues,
    bool MarkComplete);

public record OutcomeScoresModuleDetailDto(
    Guid Id,
    Guid SubmissionId,
    string SubmissionName,
    string Code,
    int SectionNumber,
    string Name,
    string? Description,
    string Status,
    int TotalRecords,
    int TotalExpectedRecords,
    bool IsSection2Complete,
    IReadOnlyList<OutcomeScoreRecordDto> Records,
    IReadOnlyList<AvailableServiceDto> AvailableServices,
    IReadOnlyList<ServiceRecordRequirementDto> ServiceRequirements);

public record ServiceRecordRequirementDto(
    Guid ServiceId,
    string ServiceName,
    int ExpectedRecords,
    int ActualRecords,
    bool IsComplete);

public record OutcomeScoreRecordDto(
    Guid RecordId,
    string AnonymisedId,
    Guid ServiceId,
    string ServiceName,
    string Status,
    DateTime? LastModified);

public record AvailableServiceDto(
    Guid ServiceId,
    string ServiceName);

public record OutcomeScoreFormDetailDto(
    Guid RecordId,
    Guid FormModuleId,
    Guid? ServiceId,
    string? ServiceName,
    string Status,
    IReadOnlyList<FormModuleSectionDto> Sections,
    IReadOnlyList<FormModuleFieldDto> Fields,
    IReadOnlyList<AvailableServiceDto> AvailableServices);

public record SaveOutcomeScoreFormRequest(
    Dictionary<string, string?> FieldValues,
    bool MarkComplete);

public record CreateOutcomeScoreRecordRequest(
    Guid? ServiceId);

// Activity Module DTOs
public record ActivitiesModuleDetailDto(
    Guid Id,
    Guid SubmissionId,
    string SubmissionName,
    string Code,
    int SectionNumber,
    string Name,
    string? Description,
    string Status,
    bool AllowsMultipleActivities,
    IReadOnlyList<ActivityRecordDto> Activities);

public record ActivityRecordDto(
    Guid ActivityId,
    string ActivityName,
    string Status,
    DateTime? LastModified);

public record ActivityFormDetailDto(
    Guid ActivityId,
    Guid SubmissionId,
    string SubmissionName,
    Guid FormModuleId,
    string ModuleName,
    string ModuleCode,
    int SectionNumber,
    string ActivityName,
    string Status,
    IReadOnlyList<FormModuleSectionDto> Sections,
    IReadOnlyList<FormModuleFieldDto> Fields);

public record CreateActivityRequest;

public record SaveActivityFormRequest(
    Dictionary<string, string?> FieldValues,
    bool MarkComplete);

// Bulk Upload DTOs
public record BulkUploadValidationResultDto(
    bool IsValid,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    IReadOnlyList<BulkUploadRowValidationDto> RowValidations,
    IReadOnlyList<BulkUploadFieldMetadataDto> FieldMetadata,
    Guid StagingId,
    string? ModuleError = null);

public record BulkUploadRowValidationDto(
    int RowNumber,
    string? ServiceName,
    bool IsValid,
    IReadOnlyList<BulkUploadFieldErrorDto> Errors);

public record BulkUploadFieldErrorDto(
    string FieldCode,
    string FieldLabel,
    string ErrorMessage);

public record BulkUploadFieldMetadataDto(
    string FieldCode,
    string FieldType,
    bool IsRequired,
    IReadOnlyList<BulkUploadFieldOptionDto> Options,
    string? ConditionalRules,
    string? Configuration);

public record BulkUploadFieldOptionDto(
    string Value,
    string Label);

public record BulkUploadResultDto(
    bool Success,
    int TotalRows,
    int SuccessCount,
    int ErrorCount,
    IReadOnlyList<BulkUploadRowResultDto> Results);

public record BulkUploadRowResultDto(
    int RowNumber,
    string? ServiceName,
    bool IsSuccess,
    string? ErrorMessage);

public record ProcessStagedBulkUploadRequestDto(
    Guid StagingId,
    IReadOnlyList<int> SelectedRowIndices,
    IReadOnlyList<BulkUploadCellEditDto> CellEdits);

public record BulkUploadCellEditDto(
    int RowIndex,
    int ColumnIndex,
    string Value);

public record CheckOutcomeScoreDuplicatesRequestDto(IReadOnlyList<OutcomeScoreRowCheckDto> Rows);

public record OutcomeScoreRowCheckDto(
    int RowIndex,
    string ServiceName,
    IReadOnlyList<OutcomeScoreFieldValueCheckDto> FieldValues);

public record OutcomeScoreFieldValueCheckDto(string FieldKey, string Value);

public record OutcomeScoreDuplicateCheckDto(int RowIndex, string ServiceName, bool HasExistingData);

public record CheckServiceUserDuplicatesRequestDto(IReadOnlyList<ServiceUserRowCheckDto> Rows);

public record ServiceUserRowCheckDto(int RowIndex, string ServiceName);

public record ServiceUserDuplicateCheckDto(int RowIndex, string ServiceName, bool HasExistingData);

// Admin data-collection submission integrity DTOs (BLUM-7047)
public record SubmissionIntegrityDto(
    bool IsCorrupted,
    int OrphanedSubmittedRecordCount,
    IReadOnlyList<Guid> OrphanedFormSubmissionIds);

public record ResetCorruptedSubmissionResultDto(
    bool WasCorrupted,
    int RecordsReset);