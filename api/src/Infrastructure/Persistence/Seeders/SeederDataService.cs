using System.Text.Json;
using Domain.DataCollections;
using Domain.ServiceCategoryForms;
using Domain.ServiceForms;
using Domain.SiteForms;
using Domain.Systems;
using Infrastructure.Persistence.Seeders.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Seeders;

public interface ISeederDataService
{
    Task<string> ExportSeederDataAsync(CancellationToken cancellationToken = default);
    Task<SeederExportData?> LoadSeederDataAsync(CancellationToken cancellationToken = default);
    Task<string?> GetSeederDataJsonAsync(CancellationToken cancellationToken = default);
}

public class SeederDataService(
    ApplicationDbContext context,
    ILogger<SeederDataService> logger) : ISeederDataService
{
    private const string SeederDataFileName = "seeder-data.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<string> ExportSeederDataAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Exporting seeder data from database...");

        var exportData = new SeederExportData
        {
            ServiceFormQuestions = await ExportServiceFormQuestionsAsync(cancellationToken),
            SiteFormQuestions = await ExportSiteFormQuestionsAsync(cancellationToken),
            ServiceCategoryFormQuestions = await ExportServiceCategoryFormQuestionsAsync(cancellationToken),
            WiderServiceCategories = await ExportWiderServiceCategoriesAsync(cancellationToken),
            DataCollectionFormModules = await ExportDataCollectionFormModulesAsync(cancellationToken),
            ExportedAt = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        };

        var json = JsonSerializer.Serialize(exportData, JsonOptions);
        var filePath = GetSeederDataSourceFilePath();

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        logger.LogInformation("Seeder data exported to {FilePath}", filePath);

        return filePath;
    }

    public async Task<SeederExportData?> LoadSeederDataAsync(CancellationToken cancellationToken = default)
    {
        var filePath = GetSeederDataFilePath();

        if (!File.Exists(filePath))
        {
            logger.LogWarning("Seeder data file not found at {FilePath}", filePath);
            return null;
        }

        logger.LogInformation("Loading seeder data from {FilePath}", filePath);

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var data = JsonSerializer.Deserialize<SeederExportData>(json, JsonOptions);

        if (data != null)
        {
            logger.LogInformation(
                "Loaded seeder data exported from {Environment} at {ExportedAt}",
                data.Environment,
                data.ExportedAt);
        }

        return data;
    }

    public async Task<string?> GetSeederDataJsonAsync(CancellationToken cancellationToken = default)
    {
        var filePath = GetSeederDataSourceFilePath();

        if (!File.Exists(filePath))
        {
            logger.LogWarning("Seeder data file not found at {FilePath}", filePath);
            return null;
        }

        return await File.ReadAllTextAsync(filePath, cancellationToken);
    }

    private static string GetSeederDataFilePath()
    {
        // At runtime, read from the output directory (where files are copied during build)
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(baseDirectory, "SeedData", SeederDataFileName);
    }

    private static string GetSeederDataSourceFilePath()
    {
        // For export, write to the source project folder so it gets committed to git
        // and automatically deployed with the next build
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        // Navigate from bin/Debug/net10.0 back to src/Api/SeedData
        var sourceDirectory = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "SeedData"));
        return Path.Combine(sourceDirectory, SeederDataFileName);
    }

    private async Task<List<ServiceFormQuestionExport>> ExportServiceFormQuestionsAsync(CancellationToken cancellationToken)
    {
        var questions = await context.ServiceFormQuestions
            .Include(q => q.Options)
            .ToListAsync(cancellationToken);

        return questions.Select(q => new ServiceFormQuestionExport
        {
            Code = q.Code,
            Label = Enc(q.Label),
            Hint = EncN(q.Hint),
            Placeholder = EncN(q.Placeholder),
            QuestionType = q.QuestionType.ToString(),
            Step = q.Step,
            DisplayOrder = q.DisplayOrder,
            IsRequired = q.IsRequired,
            IsPredefined = q.IsPredefined,
            HelpTextSummary = EncN(q.HelpTextSummary),
            HelpText = EncN(q.HelpText),
            ConditionalQuestionCode = q.ConditionalQuestionCode,
            ConditionalValue = EncN(q.ConditionalValue),
            IsActive = q.IsActive,
            Options = q.Options.Select(o => new QuestionOptionExport
            {
                Value = Enc(o.Value),
                Label = Enc(o.Label),
                DisplayOrder = o.DisplayOrder
            }).OrderBy(o => o.DisplayOrder).ToList()
        }).OrderBy(q => q.Step).ThenBy(q => q.DisplayOrder).ToList();
    }

    private async Task<List<SiteFormQuestionExport>> ExportSiteFormQuestionsAsync(CancellationToken cancellationToken)
    {
        var questions = await context.SiteFormQuestions
            .Include(q => q.Options)
            .ToListAsync(cancellationToken);

        return questions.Select(q => new SiteFormQuestionExport
        {
            Code = q.Code,
            Label = Enc(q.Label),
            Hint = EncN(q.Hint),
            Placeholder = EncN(q.Placeholder),
            QuestionType = q.QuestionType.ToString(),
            DisplayOrder = q.DisplayOrder,
            IsRequired = q.IsRequired,
            IsPredefined = q.IsPredefined,
            HelpTextSummary = EncN(q.HelpTextSummary),
            HelpText = EncN(q.HelpText),
            ConditionalQuestionCode = q.ConditionalQuestionCode,
            ConditionalValue = EncN(q.ConditionalValue),
            IsActive = q.IsActive,
            Options = q.Options.Select(o => new QuestionOptionExport
            {
                Value = Enc(o.Value),
                Label = Enc(o.Label),
                DisplayOrder = o.DisplayOrder
            }).OrderBy(o => o.DisplayOrder).ToList()
        }).OrderBy(q => q.DisplayOrder).ToList();
    }

    private async Task<List<ServiceCategoryFormQuestionExport>> ExportServiceCategoryFormQuestionsAsync(CancellationToken cancellationToken)
    {
        var questions = await context.ServiceCategoryFormQuestions
            .Include(q => q.Options)
            .ToListAsync(cancellationToken);

        return questions.Select(q => new ServiceCategoryFormQuestionExport
        {
            Code = q.Code,
            Label = Enc(q.Label),
            Hint = EncN(q.Hint),
            Placeholder = EncN(q.Placeholder),
            QuestionType = q.QuestionType.ToString(),
            Step = q.Step,
            DisplayOrder = q.DisplayOrder,
            IsRequired = q.IsRequired,
            IsPredefined = q.IsPredefined,
            HelpTextSummary = EncN(q.HelpTextSummary),
            HelpText = EncN(q.HelpText),
            ConditionalQuestionCode = q.ConditionalQuestionCode,
            ConditionalValue = EncN(q.ConditionalValue),
            IsActive = q.IsActive,
            Options = q.Options.Select(o => new QuestionOptionExport
            {
                Value = Enc(o.Value),
                Label = Enc(o.Label),
                DisplayOrder = o.DisplayOrder
            }).OrderBy(o => o.DisplayOrder).ToList()
        }).OrderBy(q => q.Step).ThenBy(q => q.DisplayOrder).ToList();
    }

    private async Task<List<GlobalDataExport>> ExportWiderServiceCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await context.GlobalData
            .Where(x => x.Entity == GlobalDataEntityTypes.WiderServiceCategory)
            .ToListAsync(cancellationToken);

        return categories.Select(c => new GlobalDataExport
        {
            Entity = Enc(c.Entity),
            Value = Enc(c.Value),
            Description = EncN(c.Description)
        }).OrderBy(c => c.Value).ToList();
    }

    private async Task<List<DataCollectionFormModuleExport>> ExportDataCollectionFormModulesAsync(CancellationToken cancellationToken)
    {
        var modules = await context.DataCollectionFormModules
            .Include(m => m.Sections)
            .Include(m => m.Fields)
            .ThenInclude(f => f.Options)
            .ToListAsync(cancellationToken);

        return modules.Select(m => new DataCollectionFormModuleExport
        {
            Code = m.Code,
            SectionNumber = m.SectionNumber,
            Name = Enc(m.Name),
            Description = EncN(m.Description),
            LastChangedOn = m.LastChangedOn,
            IsActive = m.IsActive,
            Status = m.Status.ToString(),
            ValidationSchema = m.ValidationSchema,
            Sections = m.Sections.Select(s => new FormSectionExport
            {
                SectionNumber = s.SectionNumber,
                Title = Enc(s.Title),
                Description = EncN(s.Description),
                HelpText = EncN(s.HelpText),
                HelpUrl = EncN(s.HelpUrl)
            }).OrderBy(s => s.SectionNumber).ToList(),
            Fields = m.Fields.Select(f => new FormFieldExport
            {
                FieldKey = f.FieldKey,
                Label = Enc(f.Label),
                FieldType = f.FieldType.ToString(),
                DisplayOrder = f.DisplayOrder,
                IsRequired = f.IsRequired,
                Placeholder = EncN(f.Placeholder),
                HelpText = EncN(f.HelpText),
                DefaultValue = EncN(f.DefaultValue),
                ValidationRules = f.ValidationRules,
                ConditionalRules = f.ConditionalRules,
                Configuration = f.Configuration,
                SectionNumber = m.Sections.FirstOrDefault(s => s.Id == f.FormSectionId)?.SectionNumber,
                Options = f.Options.Select(o => new FieldOptionExport
                {
                    Value = Enc(o.Value),
                    Label = Enc(o.Label),
                    DisplayOrder = o.DisplayOrder
                }).OrderBy(o => o.DisplayOrder).ToList()
            }).OrderBy(f => f.DisplayOrder).ToList()
        }).OrderBy(m => m.SectionNumber).ToList();
    }

    private static string Enc(string value) =>
        "b64:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));

    private static string? EncN(string? value) => value is null ? null : Enc(value);
}