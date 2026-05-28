using Domain.ServiceForms;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seeders;

public static class ServiceFormQuestionSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.ServiceFormQuestions.AnyAsync(cancellationToken))
        {
            return;
        }

        var questions = CreateQuestions();
        await context.ServiceFormQuestions.AddRangeAsync(questions, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static List<ServiceFormQuestion> CreateQuestions()
    {
        var questions = new List<ServiceFormQuestion>();

        // Step 1 Questions
        // SMD01 - Service name (predefined)
        var smd01 = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            "SMD01",
            "What is the service name?",
            "Enter the name of the service",
            "Enter service name",
            ServiceFormQuestionType.Text,
            step: 1,
            displayOrder: 1,
            isRequired: true,
            isPredefined: true);
        questions.Add(smd01);

        // SMD02 - Funding type
        var smd02 = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            "SMD02",
            "Is the service funded by the BSFH&HB programme?",
            null,
            null,
            ServiceFormQuestionType.Radio,
            step: 1,
            displayOrder: 2,
            isRequired: true);
        smd02.AddOption("0", "Funded", 1);
        smd02.AddOption("1", "Partially funded", 2);
        smd02.AddOption("2", "Not programme funded", 3);
        questions.Add(smd02);

        // SMD03 - Live status
        var smd03 = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            "SMD03",
            "What is the status of the service?",
            null,
            null,
            ServiceFormQuestionType.Radio,
            step: 1,
            displayOrder: 3,
            isRequired: true);
        smd03.AddOption("0", "Live", 1);
        smd03.AddOption("1", "Planned for implementation", 2);
        smd03.AddOption("2", "No longer offered", 3);
        questions.Add(smd03);

        // SMD04 - Planned implementation date (conditional on SMD03 = 1)
        var smd04 = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            "SMD04",
            "What is the planned service start date?",
            null,
            null,
            ServiceFormQuestionType.Date,
            step: 1,
            displayOrder: 4,
            isRequired: true,
            isPredefined: false,
            helpText: null,
            conditionalQuestionCode: "SMD03",
            conditionalValue: "1");
        questions.Add(smd04);

        // Step 2 Questions
        // SMD05 - Frequency
        var smd05 = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            "SMD05",
            "How often is the service offered?",
            null,
            null,
            ServiceFormQuestionType.Radio,
            step: 2,
            displayOrder: 1,
            isRequired: true);
        smd05.AddOption("0", "Daily", 1);
        smd05.AddOption("1", "Weekly", 2);
        smd05.AddOption("2", "Monthly", 3);
        smd05.AddOption("3", "One-off event", 4);
        smd05.AddOption("4", "Ad-hoc as needed", 5);
        smd05.AddOption("5", "Always live", 6);
        questions.Add(smd05);

        // SMD07 - Has name changed
        var smd07 = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            "SMD07",
            "Has the name of the service changed?",
            null,
            null,
            ServiceFormQuestionType.Radio,
            step: 2,
            displayOrder: 2,
            isRequired: true);
        smd07.AddOption("true", "Yes", 1);
        smd07.AddOption("false", "No", 2);
        questions.Add(smd07);

        // SMD08 - Previous name (conditional on SMD07 = true)
        var smd08 = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            "SMD08",
            "What was the previous name of the service?",
            null,
            "Enter previous service name",
            ServiceFormQuestionType.Text,
            step: 2,
            displayOrder: 3,
            isRequired: true,
            isPredefined: false,
            helpText: null,
            conditionalQuestionCode: "SMD07",
            conditionalValue: "true");
        questions.Add(smd08);

        // SMD09 - Strand
        var smd09 = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            "SMD09",
            "Which strand does this service belong to?",
            null,
            null,
            ServiceFormQuestionType.Radio,
            step: 2,
            displayOrder: 4,
            isRequired: true);
        smd09.AddOption("0", "Parenting support", 1);
        smd09.AddOption("1", "Parent-infant relationships and perinatal mental health", 2);
        smd09.AddOption("2", "Infant feeding support", 3);
        smd09.AddOption("3", "Early language and the home learning environment", 4);
        smd09.AddOption("4", "Wider services", 5);
        questions.Add(smd09);

        // SMD10 - Strand category (conditional on SMD09 = 1)
        var smd10 = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            "SMD10",
            "Which category does this service belong to?",
            null,
            null,
            ServiceFormQuestionType.Select,
            step: 2,
            displayOrder: 5,
            isRequired: true,
            isPredefined: false,
            helpText: null,
            conditionalQuestionCode: "SMD09",
            conditionalValue: "1");
        smd10.AddOption("0", "Perinatal mental health", 1);
        smd10.AddOption("1", "Parent-infant relationships", 2);
        smd10.AddOption("2", "Both", 3);
        questions.Add(smd10);

        // SMD12 - Lowest age
        var smd12 = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            "SMD12",
            "What is the lowest age of children who can access this service?",
            null,
            null,
            ServiceFormQuestionType.Radio,
            step: 2,
            displayOrder: 6,
            isRequired: true);
        smd12.AddOption("0", "From conception", 1);
        smd12.AddOption("1", "0-2 years", 2);
        smd12.AddOption("2", "3-5 years", 3);
        smd12.AddOption("3", "6-10 years", 4);
        smd12.AddOption("4", "11-15 years", 5);
        smd12.AddOption("5", "16-25 years", 6);
        questions.Add(smd12);

        // SMD13 - Highest age
        var smd13 = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            "SMD13",
            "What is the highest age of children who can access this service?",
            null,
            null,
            ServiceFormQuestionType.Radio,
            step: 2,
            displayOrder: 7,
            isRequired: true);
        smd13.AddOption("0", "0-2 years", 1);
        smd13.AddOption("1", "3-5 years", 2);
        smd13.AddOption("2", "6-10 years", 3);
        smd13.AddOption("3", "11-15 years", 4);
        smd13.AddOption("4", "16-25 years", 5);
        questions.Add(smd13);

        // SMD14 - Service types (checkbox)
        var smd14 = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            "SMD14",
            "Is this a targeted, specialist or universal service?",
            "Select all that apply",
            null,
            ServiceFormQuestionType.Checkbox,
            step: 2,
            displayOrder: 8,
            isRequired: true,
            isPredefined: false,
            helpTextSummary: "Help with targeted, specialist, and universal services",
            helpText: "**Targeted:** A targeted service is a type of support or intervention designed to meet the specific needs of a particular group of individuals, often those with specific challenges or conditions which require more focused attention than universal services can provide. For BSFH&HB services, this may be parenting courses targeted specifically to financially insecure families or infant feeding support groups specifically for mums whose babies are tongue-tied.\n\n**Specialist:** Specialised services support people with a range of rare and complex conditions, this can include treatments provided to patients with complex medical or surgical conditions, rare cancers or genetic disorders. For BSFH&HB services, this may be services with specialist staff supporting families where the parents or baby have a congenital condition requiring additional, specialist support.\n\n**Universal:** These are publicly available services that are provided to every family in your local area, regardless of their background or specific needs.");
        smd14.AddOption("0", "Targeted", 1);
        smd14.AddOption("1", "Specialist", 2);
        smd14.AddOption("2", "Universal", 3);
        questions.Add(smd14);

        // SMD15 - Evidence based status
        var smd15 = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            "SMD15",
            "Is the service evidence based?",
            null,
            null,
            ServiceFormQuestionType.Radio,
            step: 2,
            displayOrder: 9,
            isRequired: true,
            isPredefined: false,
            helpTextSummary: "Help with evidence-based services",
            helpText: "Interventions which are evidence-based (EBIs) are those which have been shown to be effective through rigorous impact evaluations. Impact evaluations allow us to demonstrate that an intervention has had a measurable, positive effect on child and/or family outcomes. Therefore, EBIs provide the most reliable way to improve child and family outcomes and strengthen the consistency and quality of services. EBIs focus on increasing practitioners' knowledge of scientifically proven theories of change and provide effective methods for engaging families in support.");
        smd15.AddOption("0", "Yes for all service users", 1);
        smd15.AddOption("1", "Yes for some service users", 2);
        smd15.AddOption("2", "No", 3);
        smd15.AddOption("3", "Unknown", 4);
        smd15.AddOption("4", "Not applicable", 5);
        questions.Add(smd15);

        // SMD16 - Delivery method
        var smd16 = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            "SMD16",
            "What are the methods of delivery for this service?",
            null,
            null,
            ServiceFormQuestionType.Select,
            step: 2,
            displayOrder: 10,
            isRequired: true);
        smd16.AddOption("0", "Face to face", 1);
        smd16.AddOption("1", "Online", 2);
        smd16.AddOption("2", "Telephone", 3);
        smd16.AddOption("3", "Hybrid", 4);
        questions.Add(smd16);

        // SMD17 - Delivery locations (checkbox)
        var smd17 = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            "SMD17",
            "Where is the service being delivered?",
            "Select all that apply",
            null,
            ServiceFormQuestionType.Checkbox,
            step: 2,
            displayOrder: 11,
            isRequired: true);
        smd17.AddOption("all_hub_sites", "All hub sites", 1);
        smd17.AddOption("some_hub_sites", "Some hub sites", 2);
        smd17.AddOption("all_network_sites", "All network sites", 3);
        smd17.AddOption("some_network_sites", "Some network sites", 4);
        smd17.AddOption("home_visits", "Home visits", 5);
        smd17.AddOption("early_years_setting", "Early years setting", 6);
        smd17.AddOption("primary_school", "Primary school", 7);
        smd17.AddOption("hospital", "Hospital", 8);
        smd17.AddOption("virtual", "Virtual", 9);
        smd17.AddOption("other", "Other", 10);
        questions.Add(smd17);

        // SMD18 - Service deliverer
        var smd18 = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            "SMD18",
            "Who is delivering the service on behalf of the Local Authority?",
            null,
            null,
            ServiceFormQuestionType.Radio,
            step: 2,
            displayOrder: 12,
            isRequired: true);
        smd18.AddOption("0", "Local Authority", 1);
        smd18.AddOption("1", "ICB/NHS", 2);
        smd18.AddOption("2", "Commissioned delivery partner", 3);
        smd18.AddOption("3", "Commissioned VCS partner", 4);
        smd18.AddOption("4", "Non-commissioned VCS partner", 5);
        smd18.AddOption("5", "To be confirmed", 6);
        smd18.AddOption("6", "Other", 7);
        questions.Add(smd18);

        // SMD19 - Which sites offer the service? (conditional on SMD17 containing some_hub_sites or some_network_sites)
        var smd19 = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            "SMD19",
            "Which sites offer the service?",
            "Select all sites that apply",
            null,
            ServiceFormQuestionType.Checkbox,
            step: 2,
            displayOrder: 13,
            isRequired: false,
            isPredefined: false,
            helpText: "Select the Family Hub or Network sites where this service is offered. Options are populated from your organisation's registered locations.",
            conditionalQuestionCode: "SMD17",
            conditionalValue: "some_hub_sites,some_network_sites");
        questions.Add(smd19);

        // SMD20 - User data status
        var smd20 = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            "SMD20",
            "Do you have data on the number of users accessing this service?",
            null,
            null,
            ServiceFormQuestionType.Radio,
            step: 2,
            displayOrder: 14,
            isRequired: true);
        smd20.AddOption("0", "Yes", 1);
        smd20.AddOption("1", "No", 2);
        smd20.AddOption("2", "Unknown", 3);
        smd20.AddOption("3", "Not applicable", 4);
        questions.Add(smd20);

        // SMD21 - Outcome scores status
        var smd21 = ServiceFormQuestion.New(
            ServiceFormQuestionId.New(),
            "SMD21",
            "Does the service provider collect pre- and post-outcome scores from service users?",
            null,
            null,
            ServiceFormQuestionType.Radio,
            step: 2,
            displayOrder: 15,
            isRequired: true);
        smd21.AddOption("0", "Yes", 1);
        smd21.AddOption("1", "No", 2);
        smd21.AddOption("2", "Unknown", 3);
        smd21.AddOption("3", "Not applicable", 4);
        questions.Add(smd21);

        return questions;
    }
}