using Domain.ServiceCategoryForms;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seeders;

public static class ServiceCategoryFormQuestionSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var existingQuestions = await context.ServiceCategoryFormQuestions.ToListAsync();
        if (existingQuestions.Count > 0)
        {
            return;
        }

        var questions = CreateQuestions();
        context.ServiceCategoryFormQuestions.AddRange(questions);
        await context.SaveChangesAsync();
    }

    private static List<ServiceCategoryFormQuestion> CreateQuestions()
    {
        var questions = new List<ServiceCategoryFormQuestion>();

        // WSDM01 - Do your delivery locations offer any wider services within this category?
        var wsdm01 = ServiceCategoryFormQuestion.New(
            ServiceCategoryFormQuestionId.New(),
            "WSDM01",
            "Do your delivery locations offer any wider services within this category?",
            null,
            null,
            ServiceCategoryFormQuestionType.Radio,
            step: 1,
            displayOrder: 1,
            isRequired: true,
            isPredefined: true);
        wsdm01.AddOption("yes", "Yes", 1);
        wsdm01.AddOption("no", "No", 2);
        questions.Add(wsdm01);

        // WSDM02 - Where are these wider services delivered? (conditional on WSDM01 = yes)
        var wsdm02 = ServiceCategoryFormQuestion.New(
            ServiceCategoryFormQuestionId.New(),
            "WSDM02",
            "Where are these wider services delivered?",
            "Select all delivery locations that apply.",
            null,
            ServiceCategoryFormQuestionType.Checkbox,
            step: 1,
            displayOrder: 2,
            isRequired: true,
            isPredefined: true);
        wsdm02.AddOption("hub_sites", "Hub site(s)", 1);
        wsdm02.AddOption("home_visits", "Home visits", 2);
        wsdm02.AddOption("early_years_setting", "Early years setting", 3);
        wsdm02.AddOption("hospital", "Hospital", 4);
        wsdm02.AddOption("virtual", "Virtual", 5);
        wsdm02.AddOption("network_sites", "Network Site(s)", 6);
        wsdm02.AddOption("other_locations_la", "Other locations in LA", 7);
        wsdm02.SetConditional("WSDM01", "yes");
        questions.Add(wsdm02);

        return questions;
    }
}