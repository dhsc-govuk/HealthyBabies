using System.Text.Json;
using Domain.DataCollections;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.ServiceCategoryForms;
using Domain.ServiceForms;
using Domain.SiteForms;
using Domain.Systems;
using Infrastructure.Persistence.Seeders.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Seeders;

public interface ISeederDataImporter
{
    Task<SeederImportResult> ImportManuallyAsync(bool flushExisting, CancellationToken cancellationToken = default);
    Task<SeederImportResult> SyncManuallyAsync(CancellationToken cancellationToken = default);
    Task<SeederImportResult> ImportFromJsonAsync(string jsonContent, bool flushExisting, CancellationToken cancellationToken = default);
    Task<SeederImportResult> SyncFromJsonAsync(string jsonContent, CancellationToken cancellationToken = default);
}

public record SeederImportResult(
    bool Success,
    string Message,
    int ServiceFormQuestions,
    int SiteFormQuestions,
    int ServiceCategoryFormQuestions,
    int WiderServiceCategories,
    int DataCollectionFormModules);

public class SeederDataImporter(
    ApplicationDbContext context,
    ILogger<SeederDataImporter> logger) : ISeederDataImporter
{
    private const string SeederDataFileName = "seeder-data.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<SeederImportResult> ImportManuallyAsync(bool flushExisting, CancellationToken cancellationToken = default)
    {
        var filePath = GetSeederDataFilePath();
        if (!File.Exists(filePath))
        {
            return new SeederImportResult(false, $"Seeder data file not found at {filePath}", 0, 0, 0, 0, 0);
        }

        logger.LogInformation("Manual import triggered. Loading seeder data from {FilePath}", filePath);

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var data = JsonSerializer.Deserialize<SeederExportData>(json, JsonOptions);

        if (data == null)
        {
            return new SeederImportResult(false, "Failed to deserialize seeder data", 0, 0, 0, 0, 0);
        }

        data = DecodeExportData(data);

        if (flushExisting)
        {
            await FlushExistingDataAsync(cancellationToken);
        }

        await ImportServiceFormQuestionsAsync(data.ServiceFormQuestions, cancellationToken);
        await ImportSiteFormQuestionsAsync(data.SiteFormQuestions, cancellationToken);
        await ImportServiceCategoryFormQuestionsAsync(data.ServiceCategoryFormQuestions, cancellationToken);
        await ImportWiderServiceCategoriesAsync(data.WiderServiceCategories, cancellationToken);
        await ImportDataCollectionFormModulesAsync(data.DataCollectionFormModules, cancellationToken);

        logger.LogInformation("Manual import completed successfully from {Environment}", data.Environment);

        return new SeederImportResult(
            true,
            $"Successfully imported seeder data from {data.Environment} (exported at {data.ExportedAt})",
            data.ServiceFormQuestions.Count,
            data.SiteFormQuestions.Count,
            data.ServiceCategoryFormQuestions.Count,
            data.WiderServiceCategories.Count,
            data.DataCollectionFormModules.Count);
    }

    public async Task<SeederImportResult> SyncManuallyAsync(CancellationToken cancellationToken = default)
    {
        var filePath = GetSeederDataFilePath();
        if (!File.Exists(filePath))
        {
            return new SeederImportResult(false, $"Seeder data file not found at {filePath}", 0, 0, 0, 0, 0);
        }

        logger.LogInformation("Manual sync triggered. Loading seeder data from {FilePath}", filePath);

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var data = JsonSerializer.Deserialize<SeederExportData>(json, JsonOptions);

        if (data == null)
        {
            return new SeederImportResult(false, "Failed to deserialize seeder data", 0, 0, 0, 0, 0);
        }

        data = DecodeExportData(data);

        await UpsertServiceFormQuestionsAsync(data.ServiceFormQuestions, cancellationToken);
        await UpsertSiteFormQuestionsAsync(data.SiteFormQuestions, cancellationToken);
        await UpsertServiceCategoryFormQuestionsAsync(data.ServiceCategoryFormQuestions, cancellationToken);
        await UpsertWiderServiceCategoriesAsync(data.WiderServiceCategories, cancellationToken);
        await UpsertDataCollectionFormModulesAsync(data.DataCollectionFormModules, cancellationToken);

        logger.LogInformation("Manual sync completed successfully from {Environment}", data.Environment);

        return new SeederImportResult(
            true,
            $"Successfully synced seeder data from {data.Environment} (exported at {data.ExportedAt})",
            data.ServiceFormQuestions.Count,
            data.SiteFormQuestions.Count,
            data.ServiceCategoryFormQuestions.Count,
            data.WiderServiceCategories.Count,
            data.DataCollectionFormModules.Count);
    }

    public async Task<SeederImportResult> ImportFromJsonAsync(string jsonContent, bool flushExisting, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Import from uploaded JSON triggered");

        var data = JsonSerializer.Deserialize<SeederExportData>(jsonContent, JsonOptions);

        if (data == null)
        {
            return new SeederImportResult(false, "Failed to deserialize uploaded JSON content", 0, 0, 0, 0, 0);
        }

        data = DecodeExportData(data);

        if (flushExisting)
        {
            await FlushExistingDataAsync(cancellationToken);
        }

        await ImportServiceFormQuestionsAsync(data.ServiceFormQuestions, cancellationToken);
        await ImportSiteFormQuestionsAsync(data.SiteFormQuestions, cancellationToken);
        await ImportServiceCategoryFormQuestionsAsync(data.ServiceCategoryFormQuestions, cancellationToken);
        await ImportWiderServiceCategoriesAsync(data.WiderServiceCategories, cancellationToken);
        await ImportDataCollectionFormModulesAsync(data.DataCollectionFormModules, cancellationToken);

        logger.LogInformation("Import from uploaded JSON completed successfully from {Environment}", data.Environment);

        return new SeederImportResult(
            true,
            $"Successfully imported seeder data from {data.Environment} (exported at {data.ExportedAt})",
            data.ServiceFormQuestions.Count,
            data.SiteFormQuestions.Count,
            data.ServiceCategoryFormQuestions.Count,
            data.WiderServiceCategories.Count,
            data.DataCollectionFormModules.Count);
    }

    public async Task<SeederImportResult> SyncFromJsonAsync(string jsonContent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sync (upsert) from uploaded JSON triggered");

        var data = JsonSerializer.Deserialize<SeederExportData>(jsonContent, JsonOptions);

        if (data == null)
        {
            return new SeederImportResult(false, "Failed to deserialize uploaded JSON content", 0, 0, 0, 0, 0);
        }

        data = DecodeExportData(data);

        await UpsertServiceFormQuestionsAsync(data.ServiceFormQuestions, cancellationToken);
        await UpsertSiteFormQuestionsAsync(data.SiteFormQuestions, cancellationToken);
        await UpsertServiceCategoryFormQuestionsAsync(data.ServiceCategoryFormQuestions, cancellationToken);
        await UpsertWiderServiceCategoriesAsync(data.WiderServiceCategories, cancellationToken);
        await UpsertDataCollectionFormModulesAsync(data.DataCollectionFormModules, cancellationToken);

        logger.LogInformation("Sync from uploaded JSON completed successfully from {Environment}", data.Environment);

        return new SeederImportResult(
            true,
            $"Successfully synced seeder data from {data.Environment} (exported at {data.ExportedAt})",
            data.ServiceFormQuestions.Count,
            data.SiteFormQuestions.Count,
            data.ServiceCategoryFormQuestions.Count,
            data.WiderServiceCategories.Count,
            data.DataCollectionFormModules.Count);
    }

    private async Task UpsertServiceFormQuestionsAsync(
        List<ServiceFormQuestionExport> questions,
        CancellationToken cancellationToken)
    {
        if (questions.Count == 0)
        {
            return;
        }

        var existing = await context.ServiceFormQuestions
            .Include(q => q.Options)
            .ToDictionaryAsync(q => q.Code, cancellationToken);

        foreach (var q in questions)
        {
            var questionType = Enum.Parse<ServiceFormQuestionType>(q.QuestionType);

            if (existing.TryGetValue(q.Code, out var existingQuestion))
            {
                existingQuestion.UpdateDetails(
                    q.Label,
                    q.Hint,
                    q.Placeholder,
                    questionType,
                    q.Step,
                    q.DisplayOrder,
                    q.IsRequired,
                    q.IsPredefined,
                    q.HelpTextSummary,
                    q.HelpText,
                    q.ConditionalQuestionCode,
                    q.ConditionalValue);

                existingQuestion.ClearOptions();
                foreach (var opt in q.Options)
                {
                    existingQuestion.AddOption(opt.Value, opt.Label, opt.DisplayOrder);
                }

                if (q.IsActive)
                {
                    existingQuestion.Activate();
                }
                else
                {
                    existingQuestion.Deactivate();
                }
            }
            else
            {
                var question = ServiceFormQuestion.New(
                    ServiceFormQuestionId.New(),
                    q.Code,
                    q.Label,
                    q.Hint,
                    q.Placeholder,
                    questionType,
                    q.Step,
                    q.DisplayOrder,
                    q.IsRequired,
                    q.IsPredefined,
                    q.HelpTextSummary,
                    q.HelpText,
                    q.ConditionalQuestionCode,
                    q.ConditionalValue);

                foreach (var opt in q.Options)
                {
                    question.AddOption(opt.Value, opt.Label, opt.DisplayOrder);
                }

                if (!q.IsActive)
                {
                    question.Deactivate();
                }

                context.ServiceFormQuestions.Add(question);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertSiteFormQuestionsAsync(
        List<SiteFormQuestionExport> questions,
        CancellationToken cancellationToken)
    {
        if (questions.Count == 0)
        {
            return;
        }

        var existing = await context.SiteFormQuestions
            .Include(q => q.Options)
            .ToDictionaryAsync(q => q.Code, cancellationToken);

        foreach (var q in questions)
        {
            var questionType = Enum.Parse<SiteFormQuestionType>(q.QuestionType);

            if (existing.TryGetValue(q.Code, out var existingQuestion))
            {
                existingQuestion.UpdateDetails(
                    q.Label,
                    q.Hint,
                    q.Placeholder,
                    questionType,
                    q.DisplayOrder,
                    q.IsRequired,
                    q.HelpTextSummary,
                    q.HelpText,
                    q.ConditionalQuestionCode,
                    q.ConditionalValue);

                existingQuestion.ClearOptions();
                foreach (var opt in q.Options)
                {
                    existingQuestion.AddOption(opt.Value, opt.Label, opt.DisplayOrder);
                }

                if (q.IsActive)
                {
                    existingQuestion.Activate();
                }
                else
                {
                    existingQuestion.Deactivate();
                }
            }
            else
            {
                var question = SiteFormQuestion.New(
                    SiteFormQuestionId.New(),
                    q.Code,
                    q.Label,
                    q.Hint,
                    q.Placeholder,
                    questionType,
                    q.DisplayOrder,
                    q.IsRequired,
                    q.IsPredefined,
                    q.HelpTextSummary,
                    q.HelpText,
                    q.ConditionalQuestionCode,
                    q.ConditionalValue);

                foreach (var opt in q.Options)
                {
                    question.AddOption(opt.Value, opt.Label, opt.DisplayOrder);
                }

                if (!q.IsActive)
                {
                    question.Deactivate();
                }

                context.SiteFormQuestions.Add(question);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertServiceCategoryFormQuestionsAsync(
        List<ServiceCategoryFormQuestionExport> questions,
        CancellationToken cancellationToken)
    {
        if (questions.Count == 0)
        {
            return;
        }

        var existing = await context.ServiceCategoryFormQuestions
            .Include(q => q.Options)
            .ToDictionaryAsync(q => q.Code, cancellationToken);

        foreach (var q in questions)
        {
            var questionType = Enum.Parse<ServiceCategoryFormQuestionType>(q.QuestionType);

            if (existing.TryGetValue(q.Code, out var existingQuestion))
            {
                existingQuestion.UpdateDetails(
                    q.Label,
                    q.Hint,
                    q.Placeholder,
                    questionType,
                    q.Step,
                    q.DisplayOrder,
                    q.IsRequired,
                    q.IsPredefined,
                    q.HelpTextSummary,
                    q.HelpText,
                    q.ConditionalQuestionCode,
                    q.ConditionalValue);

                existingQuestion.ClearOptions();
                foreach (var opt in q.Options)
                {
                    existingQuestion.AddOption(opt.Value, opt.Label, opt.DisplayOrder);
                }

                if (q.IsActive)
                {
                    existingQuestion.Activate();
                }
                else
                {
                    existingQuestion.Deactivate();
                }
            }
            else
            {
                var question = ServiceCategoryFormQuestion.New(
                    ServiceCategoryFormQuestionId.New(),
                    q.Code,
                    q.Label,
                    q.Hint,
                    q.Placeholder,
                    questionType,
                    q.Step,
                    q.DisplayOrder,
                    q.IsRequired,
                    q.IsPredefined);

                if (!string.IsNullOrEmpty(q.ConditionalQuestionCode) && !string.IsNullOrEmpty(q.ConditionalValue))
                {
                    question.SetConditional(q.ConditionalQuestionCode, q.ConditionalValue);
                }

                if (!string.IsNullOrEmpty(q.HelpTextSummary) && !string.IsNullOrEmpty(q.HelpText))
                {
                    question.SetHelpText(q.HelpTextSummary, q.HelpText);
                }

                foreach (var opt in q.Options)
                {
                    question.AddOption(opt.Value, opt.Label, opt.DisplayOrder);
                }

                if (!q.IsActive)
                {
                    question.Deactivate();
                }

                context.ServiceCategoryFormQuestions.Add(question);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertWiderServiceCategoriesAsync(
        List<GlobalDataExport> categories,
        CancellationToken cancellationToken)
    {
        if (categories.Count == 0)
        {
            return;
        }

        var existing = await context.GlobalData
            .Where(x => x.Entity == GlobalDataEntityTypes.WiderServiceCategory)
            .ToDictionaryAsync(x => x.Value, cancellationToken);

        foreach (var c in categories)
        {
            if (existing.TryGetValue(c.Value, out var existingCategory))
            {
                existingCategory.UpdateDetails(c.Entity, c.Value, c.Description);
            }
            else
            {
                context.GlobalData.Add(GlobalData.New(c.Entity, c.Value, c.Description));
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertDataCollectionFormModulesAsync(
        List<DataCollectionFormModuleExport> modules,
        CancellationToken cancellationToken)
    {
        if (modules.Count == 0)
        {
            return;
        }

        var existingModules = await context.DataCollectionFormModules
            .Include(m => m.Sections)
            .Include(m => m.Fields)
                .ThenInclude(f => f.Options)
            .ToDictionaryAsync(m => m.Code, cancellationToken);

        var newModules = new List<DataCollectionFormModuleExport>();

        foreach (var m in modules)
        {
            if (existingModules.TryGetValue(m.Code, out var existing))
            {
                UpsertDataCollectionFormModule(existing, m);
            }
            else
            {
                newModules.Add(m);
            }
        }

        if (newModules.Count > 0)
        {
            logger.LogInformation("Adding {Count} new data collection form modules", newModules.Count);
            await ImportDataCollectionFormModulesAsync(newModules, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static void UpsertDataCollectionFormModule(
        DataCollectionFormModule existing,
        DataCollectionFormModuleExport incoming)
    {
        existing.UpdateDetails(incoming.Name, incoming.Description);
        existing.UpdateLastChangedOn(incoming.LastChangedOn);

        var sectionMap = new Dictionary<int, Domain.DataCollections.Forms.FormSection>();
        var existingSectionsBySectionNumber = existing.Sections.ToDictionary(s => s.SectionNumber);

        foreach (var s in incoming.Sections.OrderBy(x => x.SectionNumber))
        {
            if (existingSectionsBySectionNumber.TryGetValue(s.SectionNumber, out var existingSection))
            {
                existingSection.UpdateDetails(s.Title, s.Description, s.HelpText, s.HelpUrl);
                sectionMap[s.SectionNumber] = existingSection;
            }
            else
            {
                var newSection = existing.AddSection(s.SectionNumber, s.Title, s.Description, s.HelpText, s.HelpUrl);
                sectionMap[s.SectionNumber] = newSection;
            }
        }

        var existingFieldsByKey = existing.Fields.ToDictionary(f => f.FieldKey);

        foreach (var f in incoming.Fields.OrderBy(x => x.DisplayOrder))
        {
            var fieldType = FieldType.From(f.FieldType);

            if (existingFieldsByKey.TryGetValue(f.FieldKey, out var existingField))
            {
                existingField.UpdateDetails(
                    f.Label,
                    f.DisplayOrder,
                    f.IsRequired,
                    f.Placeholder,
                    f.HelpText,
                    f.DefaultValue);

                existingField.SetFieldType(fieldType);
                existingField.SetValidationRules(f.ValidationRules);
                existingField.SetConditionalRules(f.ConditionalRules);
                existingField.SetConfiguration(f.Configuration);

                if (f.SectionNumber.HasValue && sectionMap.TryGetValue(f.SectionNumber.Value, out var section))
                {
                    existingField.SetSection(section.Id);
                }
                else
                {
                    existingField.SetSection(null);
                }

                var optionIdsToRemove = existingField.Options.Select(o => o.Id).ToList();
                foreach (var optionId in optionIdsToRemove)
                {
                    existingField.RemoveOption(optionId);
                }

                foreach (var opt in f.Options)
                {
                    existingField.AddOption(opt.Value, opt.Label, opt.DisplayOrder);
                }
            }
            else
            {
                var newField = existing.AddField(
                    f.FieldKey,
                    f.Label,
                    fieldType,
                    f.DisplayOrder,
                    f.IsRequired,
                    f.Placeholder,
                    f.HelpText,
                    f.DefaultValue,
                    f.ValidationRules);

                if (f.SectionNumber.HasValue && sectionMap.TryGetValue(f.SectionNumber.Value, out var section))
                {
                    newField.SetSection(section.Id);
                }

                if (!string.IsNullOrEmpty(f.ConditionalRules))
                {
                    newField.SetConditionalRules(f.ConditionalRules);
                }

                if (!string.IsNullOrEmpty(f.Configuration))
                {
                    newField.SetConfiguration(f.Configuration);
                }

                foreach (var opt in f.Options)
                {
                    newField.AddOption(opt.Value, opt.Label, opt.DisplayOrder);
                }
            }
        }
    }

    private static string GetSeederDataFilePath()
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(baseDirectory, "SeedData", SeederDataFileName);
    }

    // Decode b64:-prefixed values written by SeederDataService.Enc/EncN.
    // Returns the value unchanged when no prefix is present (backward compat with plain-text exports).
    private static string Decode(string value)
    {
        const string prefix = "b64:";
        if (string.IsNullOrEmpty(value) || !value.StartsWith(prefix, StringComparison.Ordinal))
        {
            return value;
        }

        return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(value.Substring(prefix.Length)));
    }

    private static string? DecodeN(string? value) => value is null ? null : Decode(value);

    private static SeederExportData DecodeExportData(SeederExportData data) => data with
    {
        ServiceFormQuestions = data.ServiceFormQuestions.Select(q => q with
        {
            Label = Decode(q.Label),
            Hint = DecodeN(q.Hint),
            Placeholder = DecodeN(q.Placeholder),
            HelpTextSummary = DecodeN(q.HelpTextSummary),
            HelpText = DecodeN(q.HelpText),
            ConditionalValue = DecodeN(q.ConditionalValue),
            Options = q.Options.Select(o => o with { Value = Decode(o.Value), Label = Decode(o.Label) }).ToList()
        }).ToList(),
        SiteFormQuestions = data.SiteFormQuestions.Select(q => q with
        {
            Label = Decode(q.Label),
            Hint = DecodeN(q.Hint),
            Placeholder = DecodeN(q.Placeholder),
            HelpTextSummary = DecodeN(q.HelpTextSummary),
            HelpText = DecodeN(q.HelpText),
            ConditionalValue = DecodeN(q.ConditionalValue),
            Options = q.Options.Select(o => o with { Value = Decode(o.Value), Label = Decode(o.Label) }).ToList()
        }).ToList(),
        ServiceCategoryFormQuestions = data.ServiceCategoryFormQuestions.Select(q => q with
        {
            Label = Decode(q.Label),
            Hint = DecodeN(q.Hint),
            Placeholder = DecodeN(q.Placeholder),
            HelpTextSummary = DecodeN(q.HelpTextSummary),
            HelpText = DecodeN(q.HelpText),
            ConditionalValue = DecodeN(q.ConditionalValue),
            Options = q.Options.Select(o => o with { Value = Decode(o.Value), Label = Decode(o.Label) }).ToList()
        }).ToList(),
        WiderServiceCategories = data.WiderServiceCategories.Select(c => c with
        {
            Entity = Decode(c.Entity),
            Value = Decode(c.Value),
            Description = DecodeN(c.Description)
        }).ToList(),
        DataCollectionFormModules = data.DataCollectionFormModules.Select(m => m with
        {
            Name = Decode(m.Name),
            Description = DecodeN(m.Description),
            Sections = m.Sections.Select(s => s with
            {
                Title = Decode(s.Title),
                Description = DecodeN(s.Description),
                HelpText = DecodeN(s.HelpText),
                HelpUrl = DecodeN(s.HelpUrl)
            }).ToList(),
            Fields = m.Fields.Select(f => f with
            {
                Label = Decode(f.Label),
                Placeholder = DecodeN(f.Placeholder),
                HelpText = DecodeN(f.HelpText),
                DefaultValue = DecodeN(f.DefaultValue),
                Options = f.Options.Select(o => o with { Value = Decode(o.Value), Label = Decode(o.Label) }).ToList()
            }).ToList()
        }).ToList()
    };

    private async Task FlushExistingDataAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Flushing existing seeder data...");

        // Delete in correct order to respect foreign key constraints
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM service_form_question_options", cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM service_form_questions", cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM site_form_question_options", cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM site_form_questions", cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM service_category_form_question_options", cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM service_category_form_questions", cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM global_data WHERE entity = {0}",
            GlobalDataEntityTypes.WiderServiceCategory);

        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM form_submissions", cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM data_collection_form_module_assignments", cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM form_field_options", cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM form_fields", cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM form_sections", cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM data_collection_form_modules", cancellationToken);

        logger.LogInformation("Existing seeder data flushed");
    }

    private async Task ImportServiceFormQuestionsAsync(
        List<ServiceFormQuestionExport> questions,
        CancellationToken cancellationToken)
    {
        if (questions.Count == 0)
        {
            return;
        }

        logger.LogInformation("Importing {Count} service form questions", questions.Count);

        foreach (var q in questions)
        {
            var questionType = Enum.Parse<ServiceFormQuestionType>(q.QuestionType);
            var question = ServiceFormQuestion.New(
                ServiceFormQuestionId.New(),
                q.Code,
                q.Label,
                q.Hint,
                q.Placeholder,
                questionType,
                q.Step,
                q.DisplayOrder,
                q.IsRequired,
                q.IsPredefined,
                q.HelpTextSummary,
                q.HelpText,
                q.ConditionalQuestionCode,
                q.ConditionalValue);

            foreach (var opt in q.Options)
            {
                question.AddOption(opt.Value, opt.Label, opt.DisplayOrder);
            }

            if (!q.IsActive)
            {
                question.Deactivate();
            }

            context.ServiceFormQuestions.Add(question);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task ImportSiteFormQuestionsAsync(
        List<SiteFormQuestionExport> questions,
        CancellationToken cancellationToken)
    {
        if (questions.Count == 0)
        {
            return;
        }

        logger.LogInformation("Importing {Count} site form questions", questions.Count);

        foreach (var q in questions)
        {
            var questionType = Enum.Parse<SiteFormQuestionType>(q.QuestionType);
            var question = SiteFormQuestion.New(
                SiteFormQuestionId.New(),
                q.Code,
                q.Label,
                q.Hint,
                q.Placeholder,
                questionType,
                q.DisplayOrder,
                q.IsRequired,
                q.IsPredefined,
                q.HelpTextSummary,
                q.HelpText,
                q.ConditionalQuestionCode,
                q.ConditionalValue);

            foreach (var opt in q.Options)
            {
                question.AddOption(opt.Value, opt.Label, opt.DisplayOrder);
            }

            if (!q.IsActive)
            {
                question.Deactivate();
            }

            context.SiteFormQuestions.Add(question);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task ImportServiceCategoryFormQuestionsAsync(
        List<ServiceCategoryFormQuestionExport> questions,
        CancellationToken cancellationToken)
    {
        if (questions.Count == 0)
        {
            return;
        }

        logger.LogInformation("Importing {Count} service category form questions", questions.Count);

        foreach (var q in questions)
        {
            var questionType = Enum.Parse<ServiceCategoryFormQuestionType>(q.QuestionType);
            var question = ServiceCategoryFormQuestion.New(
                ServiceCategoryFormQuestionId.New(),
                q.Code,
                q.Label,
                q.Hint,
                q.Placeholder,
                questionType,
                q.Step,
                q.DisplayOrder,
                q.IsRequired,
                q.IsPredefined);

            if (!string.IsNullOrEmpty(q.ConditionalQuestionCode) && !string.IsNullOrEmpty(q.ConditionalValue))
            {
                question.SetConditional(q.ConditionalQuestionCode, q.ConditionalValue);
            }

            if (!string.IsNullOrEmpty(q.HelpTextSummary) && !string.IsNullOrEmpty(q.HelpText))
            {
                question.SetHelpText(q.HelpTextSummary, q.HelpText);
            }

            foreach (var opt in q.Options)
            {
                question.AddOption(opt.Value, opt.Label, opt.DisplayOrder);
            }

            if (!q.IsActive)
            {
                question.Deactivate();
            }

            context.ServiceCategoryFormQuestions.Add(question);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task ImportWiderServiceCategoriesAsync(
        List<GlobalDataExport> categories,
        CancellationToken cancellationToken)
    {
        if (categories.Count == 0)
        {
            return;
        }

        logger.LogInformation("Importing {Count} wider service categories", categories.Count);

        foreach (var c in categories)
        {
            var category = GlobalData.New(c.Entity, c.Value, c.Description);
            context.GlobalData.Add(category);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task ImportDataCollectionFormModulesAsync(
        List<DataCollectionFormModuleExport> modules,
        CancellationToken cancellationToken)
    {
        if (modules.Count == 0)
        {
            return;
        }

        logger.LogInformation("Importing {Count} data collection form modules", modules.Count);

        foreach (var m in modules)
        {
            var module = DataCollectionFormModule.Create(
                DataCollectionFormModuleId.New(),
                m.Code,
                m.SectionNumber,
                m.Name,
                m.Description,
                m.LastChangedOn,
                m.IsActive);

            // Add sections first and track them by section number
            var sectionMap = new Dictionary<int, Domain.DataCollections.Forms.FormSection>();
            foreach (var s in m.Sections.OrderBy(x => x.SectionNumber))
            {
                var section = module.AddSection(
                    s.SectionNumber,
                    s.Title,
                    s.Description,
                    s.HelpText,
                    s.HelpUrl);
                sectionMap[s.SectionNumber] = section;
            }

            // Add fields
            foreach (var f in m.Fields.OrderBy(x => x.DisplayOrder))
            {
                var fieldType = FieldType.From(f.FieldType);
                var field = module.AddField(
                    f.FieldKey,
                    f.Label,
                    fieldType,
                    f.DisplayOrder,
                    f.IsRequired,
                    f.Placeholder,
                    f.HelpText,
                    f.DefaultValue,
                    f.ValidationRules);

                if (f.SectionNumber.HasValue && sectionMap.TryGetValue(f.SectionNumber.Value, out var section))
                {
                    field.SetSection(section.Id);
                }

                if (!string.IsNullOrEmpty(f.ConditionalRules))
                {
                    field.SetConditionalRules(f.ConditionalRules);
                }

                if (!string.IsNullOrEmpty(f.Configuration))
                {
                    field.SetConfiguration(f.Configuration);
                }

                foreach (var opt in f.Options)
                {
                    field.AddOption(opt.Value, opt.Label, opt.DisplayOrder);
                }
            }

            context.DataCollectionFormModules.Add(module);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}