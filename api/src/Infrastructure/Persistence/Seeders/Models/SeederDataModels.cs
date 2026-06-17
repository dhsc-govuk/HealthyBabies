namespace Infrastructure.Persistence.Seeders.Models;

public record SeederExportData
{
    public List<ServiceFormQuestionExport> ServiceFormQuestions { get; init; } = [];
    public List<SiteFormQuestionExport> SiteFormQuestions { get; init; } = [];
    public List<ServiceCategoryFormQuestionExport> ServiceCategoryFormQuestions { get; init; } = [];
    public List<GlobalDataExport> WiderServiceCategories { get; init; } = [];
    public List<DataCollectionFormModuleExport> DataCollectionFormModules { get; init; } = [];
    public DateTime ExportedAt { get; init; } = DateTime.UtcNow;
    public string Environment { get; init; } = string.Empty;
}

public record ServiceFormQuestionExport
{
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? Hint { get; init; }
    public string? Placeholder { get; init; }
    public string QuestionType { get; init; } = string.Empty;
    public int Step { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsRequired { get; init; }
    public bool IsPredefined { get; init; }
    public string? HelpTextSummary { get; init; }
    public string? HelpText { get; init; }
    public string? ConditionalQuestionCode { get; init; }
    public string? ConditionalValue { get; init; }
    public bool IsActive { get; init; }
    public List<QuestionOptionExport> Options { get; init; } = [];
}

public record SiteFormQuestionExport
{
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? Hint { get; init; }
    public string? Placeholder { get; init; }
    public string QuestionType { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public bool IsRequired { get; init; }
    public bool IsPredefined { get; init; }
    public string? HelpTextSummary { get; init; }
    public string? HelpText { get; init; }
    public string? ConditionalQuestionCode { get; init; }
    public string? ConditionalValue { get; init; }
    public bool IsActive { get; init; }
    public List<QuestionOptionExport> Options { get; init; } = [];
}

public record ServiceCategoryFormQuestionExport
{
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? Hint { get; init; }
    public string? Placeholder { get; init; }
    public string QuestionType { get; init; } = string.Empty;
    public int Step { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsRequired { get; init; }
    public bool IsPredefined { get; init; }
    public string? HelpTextSummary { get; init; }
    public string? HelpText { get; init; }
    public string? ConditionalQuestionCode { get; init; }
    public string? ConditionalValue { get; init; }
    public bool IsActive { get; init; }
    public List<QuestionOptionExport> Options { get; init; } = [];
}

public record QuestionOptionExport
{
    public string Value { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
}

public record GlobalDataExport
{
    public string Entity { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public record DataCollectionFormModuleExport
{
    public string Code { get; init; } = string.Empty;
    public int SectionNumber { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime LastChangedOn { get; init; }
    public bool IsActive { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ValidationSchema { get; init; }
    public List<FormSectionExport> Sections { get; init; } = [];
    public List<FormFieldExport> Fields { get; init; } = [];
}

public record FormSectionExport
{
    public int SectionNumber { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? HelpText { get; init; }
    public string? HelpUrl { get; init; }
}

public record FormFieldExport
{
    public string FieldKey { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string FieldType { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public bool IsRequired { get; init; }
    public string? Placeholder { get; init; }
    public string? HelpText { get; init; }
    public string? DefaultValue { get; init; }
    public string? ValidationRules { get; init; }
    public string? ConditionalRules { get; init; }
    public string? Configuration { get; init; }
    public int? SectionNumber { get; init; }
    public List<FieldOptionExport> Options { get; init; } = [];
}

public record FieldOptionExport
{
    public string Value { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
}