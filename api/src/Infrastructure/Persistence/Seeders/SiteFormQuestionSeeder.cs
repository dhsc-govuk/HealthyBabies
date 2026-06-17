using Domain.SiteForms;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seeders;

public static class SiteFormQuestionSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        var existingCodes = await context.SiteFormQuestions
            .Select(q => q.Code)
            .ToListAsync(cancellationToken);

        var questions = CreateQuestions()
            .Where(q => !existingCodes.Contains(q.Code))
            .ToList();

        if (questions.Count == 0)
        {
            return;
        }

        await context.SiteFormQuestions.AddRangeAsync(questions, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static List<SiteFormQuestion> CreateQuestions()
    {
        var questions = new List<SiteFormQuestion>();

        // FHS01 - Delivery site name (predefined - maps to Location.Name)
        var fhs01 = SiteFormQuestion.New(
            SiteFormQuestionId.New(),
            "FHS01",
            "What is the official, public name of the site?",
            "Enter the name of the site",
            "Enter site name",
            SiteFormQuestionType.Text,
            displayOrder: 1,
            isRequired: true,
            isPredefined: true);
        questions.Add(fhs01);

        // FHS02 - Postcode (predefined - maps to Location.PostCode)
        var fhs02 = SiteFormQuestion.New(
            SiteFormQuestionId.New(),
            "FHS02",
            "Postcode (address)",
            null,
            "Enter postcode",
            SiteFormQuestionType.Text,
            displayOrder: 2,
            isRequired: true,
            isPredefined: true);
        questions.Add(fhs02);

        // FHS12 - Address line 1 (predefined - maps to Location.AddressLine1)
        var fhs12 = SiteFormQuestion.New(
            SiteFormQuestionId.New(),
            "FHS12",
            "Address line 1",
            null,
            "Enter address line 1",
            SiteFormQuestionType.Text,
            displayOrder: 3,
            isRequired: false,
            isPredefined: true);
        questions.Add(fhs12);

        // FHS13 - Address line 2 (predefined - maps to Location.AddressLine2)
        var fhs13 = SiteFormQuestion.New(
            SiteFormQuestionId.New(),
            "FHS13",
            "Address line 2 (optional)",
            null,
            "Enter address line 2",
            SiteFormQuestionType.Text,
            displayOrder: 4,
            isRequired: false,
            isPredefined: true);
        questions.Add(fhs13);

        // FHS14 - Town or city (predefined - maps to Location.TownOrCity)
        var fhs14 = SiteFormQuestion.New(
            SiteFormQuestionId.New(),
            "FHS14",
            "Town or city",
            null,
            "Enter town or city",
            SiteFormQuestionType.Text,
            displayOrder: 5,
            isRequired: false,
            isPredefined: true);
        questions.Add(fhs14);

        // FHS15 - County (predefined - maps to Location.County)
        var fhs15 = SiteFormQuestion.New(
            SiteFormQuestionId.New(),
            "FHS15",
            "County (optional)",
            null,
            "Enter county",
            SiteFormQuestionType.Text,
            displayOrder: 6,
            isRequired: false,
            isPredefined: true);
        questions.Add(fhs15);

        // FHS03 - UPRN (predefined - maps to Location.ReferenceNumber)
        var fhs03 = SiteFormQuestion.New(
            SiteFormQuestionId.New(),
            "FHS03",
            "What is the Unique Property Reference Number (UPRN) of the Site?",
            null,
            "Enter UPRN",
            SiteFormQuestionType.Text,
            displayOrder: 7,
            isRequired: false,
            isPredefined: true);
        questions.Add(fhs03);

        // FHS04 - Status of Site
        var fhs04 = SiteFormQuestion.New(
            SiteFormQuestionId.New(),
            "FHS04",
            "Is this site currently active?",
            null,
            null,
            SiteFormQuestionType.Radio,
            displayOrder: 8,
            isRequired: true);
        fhs04.AddOption("active", "Active", 1);
        fhs04.AddOption("inactive", "Inactive", 2);
        fhs04.AddOption("pending", "Pending", 3);
        questions.Add(fhs04);

        // FHS05 - Type of site
        var fhs05 = SiteFormQuestion.New(
            SiteFormQuestionId.New(),
            "FHS05",
            "What type of site is this?",
            null,
            null,
            SiteFormQuestionType.Radio,
            displayOrder: 9,
            isRequired: true);
        fhs05.AddOption("family_hub", "Family Hub", 1);
        fhs05.AddOption("best_start_family_hub", "Best Start Family Hub", 2);
        fhs05.AddOption("linked_site", "Linked Site", 3);
        fhs05.AddOption("spoke_site", "Spoke Site", 4);
        questions.Add(fhs05);

        // FHS06 - Name change (static Yes/No options)
        var fhs06 = SiteFormQuestion.New(
            SiteFormQuestionId.New(),
            "FHS06",
            "Has the site changed name?",
            null,
            null,
            SiteFormQuestionType.Radio,
            displayOrder: 10,
            isRequired: true);
        fhs06.AddOption("true", "Yes", 1);
        fhs06.AddOption("false", "No", 2);
        questions.Add(fhs06);

        // FHS08 - Date opened
        var fhs08 = SiteFormQuestion.New(
            SiteFormQuestionId.New(),
            "FHS08",
            "When was the site officially opened as a Family Hub, Best Start Family Hub or linked site?",
            null,
            null,
            SiteFormQuestionType.Date,
            displayOrder: 11,
            isRequired: true);
        questions.Add(fhs08);

        // FHS09 - BSFH branding (static Yes/No options)
        var fhs09 = SiteFormQuestion.New(
            SiteFormQuestionId.New(),
            "FHS09",
            "Has the site introduced branded banners, stickers and other such means to clearly associate your sites with the Best Start in Life campaign, noting it is funded by the UK government?",
            null,
            null,
            SiteFormQuestionType.Radio,
            displayOrder: 12,
            isRequired: true);
        fhs09.AddOption("true", "Yes", 1);
        fhs09.AddOption("false", "No", 2);
        questions.Add(fhs09);

        // FHS10 - Location type (checkbox for multi-select)
        var fhs10 = SiteFormQuestion.New(
            SiteFormQuestionId.New(),
            "FHS10",
            "Is the site co-located with other community functions? Please select all that apply",
            "Select all that apply",
            null,
            SiteFormQuestionType.Checkbox,
            displayOrder: 13,
            isRequired: false);
        fhs10.AddOption("library", "Library", 1);
        fhs10.AddOption("community_centre", "Community Centre", 2);
        fhs10.AddOption("health_centre", "Health Centre", 3);
        fhs10.AddOption("school", "School", 4);
        fhs10.AddOption("childrens_centre", "Children's Centre", 5);
        fhs10.AddOption("other", "Other", 6);
        questions.Add(fhs10);

        // FHS11 - Clarification comments
        var fhs11 = SiteFormQuestion.New(
            SiteFormQuestionId.New(),
            "FHS11",
            "Please provide any relevant additional information - for example if one of the questions doesn't properly capture the information about a Family Hub site. Please only add comments in this section if necessary.",
            null,
            "Enter comments",
            SiteFormQuestionType.Text,
            displayOrder: 14,
            isRequired: false);
        questions.Add(fhs11);

        return questions;
    }
}