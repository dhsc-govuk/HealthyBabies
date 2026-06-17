using Domain.DataCollections;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seeders;

public static class DataCollectionFormModuleSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        var systemUser = await context.Users.FirstOrDefaultAsync(cancellationToken);
        var systemUserId = systemUser?.Id ?? UserId.Empty();

        var existingModules = await context.DataCollectionFormModules
            .Include(m => m.Sections)
            .Include(m => m.Fields)
            .ToListAsync(cancellationToken);

        if (!existingModules.Any())
        {
            var modules = CreateFormModules(systemUserId);
            await context.DataCollectionFormModules.AddRangeAsync(modules, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var lastChangedOn = new DateTime(2026, 12, 21, 0, 0, 0, DateTimeKind.Utc);
            var modulesAdded = false;

            // Check if service-users module exists, if not create it
            var serviceUsersModule = existingModules.FirstOrDefault(m => m.Code == DataCollectionFormModuleCodes.ServiceUsers);
            if (serviceUsersModule == null)
            {
                var newServiceUsersModule = CreateServiceUsersModule(lastChangedOn, systemUserId);
                await context.DataCollectionFormModules.AddAsync(newServiceUsersModule, cancellationToken);
                modulesAdded = true;
            }

            // Check if wider-service-users module exists, if not create it
            var widerServiceUsersModule = existingModules.FirstOrDefault(m => m.Code == DataCollectionFormModuleCodes.WiderServiceUsers);
            if (widerServiceUsersModule == null)
            {
                var newWiderServiceUsersModule = CreateWiderServiceUsersModule(lastChangedOn, systemUserId);
                await context.DataCollectionFormModules.AddAsync(newWiderServiceUsersModule, cancellationToken);
                modulesAdded = true;
            }

            // Check if healthy-babies module exists, if not create it
            var healthyBabiesModule = existingModules.FirstOrDefault(m => m.Code == DataCollectionFormModuleCodes.HealthyBabies);
            if (healthyBabiesModule == null)
            {
                var newHealthyBabiesModule = CreateHealthyBabiesModule(lastChangedOn, systemUserId);
                await context.DataCollectionFormModules.AddAsync(newHealthyBabiesModule, cancellationToken);
                modulesAdded = true;
            }

            // Check if outcome-scores module exists, if not create it
            var outcomeScoresModule = existingModules.FirstOrDefault(m => m.Code == DataCollectionFormModuleCodes.OutcomeScores);
            if (outcomeScoresModule == null)
            {
                var newOutcomeScoresModule = CreateOutcomeScoresModule(lastChangedOn, systemUserId);
                await context.DataCollectionFormModules.AddAsync(newOutcomeScoresModule, cancellationToken);
                modulesAdded = true;
            }

            // Check if breastfeeding-rates module exists, if not create it
            var breastfeedingModule = existingModules.FirstOrDefault(m => m.Code == DataCollectionFormModuleCodes.BreastfeedingRates);
            if (breastfeedingModule == null)
            {
                var newBreastfeedingModule = CreateBreastfeedingRatesModule(lastChangedOn, systemUserId);
                await context.DataCollectionFormModules.AddAsync(newBreastfeedingModule, cancellationToken);
                modulesAdded = true;
            }

            if (modulesAdded)
            {
                await context.SaveChangesAsync(cancellationToken);
            }

            var moduleDescriptions = GetModuleDescriptions();

            foreach (var module in existingModules)
            {
                if (moduleDescriptions.TryGetValue(module.Code, out var details))
                {
                    module.UpdateDetails(details.Name, details.Description);
                }
            }

            // Add missing fields to Service Users module (QSU13, QSU16)
            await AddMissingServiceUsersFieldsAsync(context, existingModules, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task AddMissingServiceUsersFieldsAsync(
        ApplicationDbContext context,
        List<DataCollectionFormModule> existingModules,
        CancellationToken cancellationToken)
    {
        var serviceUsersModule = existingModules.FirstOrDefault(m => m.Code == DataCollectionFormModuleCodes.ServiceUsers);
        if (serviceUsersModule == null)
        {
            return;
        }

        var existingFieldKeys = serviceUsersModule.Fields.Select(f => f.FieldKey).ToHashSet();
        var section1 = serviceUsersModule.Sections.FirstOrDefault(s => s.SectionNumber == 1);
        var section4 = serviceUsersModule.Sections.FirstOrDefault(s => s.SectionNumber == 4);

        var fieldsAdded = false;

        // Add QSU02 if missing (Do you have data? - conditional on QSU01=yes, shown inline)
        if (!existingFieldKeys.Contains("QSU02") && section1 != null)
        {
            var qsu02 = serviceUsersModule.AddField("QSU02", "Do you have data on the number of users who accessed this service this quarter?", FieldType.Radio, 2, true, null, "For example, this can be centrally collected data or data received from delivery partners.");
            qsu02.SetSection(section1.Id);
            qsu02.AddOption("yes", "Yes", 1);
            qsu02.AddOption("no", "No", 2);
            qsu02.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU01\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
            fieldsAdded = true;
        }

        // Add QSU17 if missing (Clarification Comments - now in section 4)
        if (!existingFieldKeys.Contains("QSU17") && section4 != null)
        {
            var qsu17 = serviceUsersModule.AddField("QSU17", "Clarification comments", FieldType.Textarea, 34, false, null, "Please provide any relevant additional information – for example if one of the questions doesn't properly capture the information about users. Please only add comments in this section if necessary.");
            qsu17.SetSection(section4.Id);
            qsu17.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU01\",\"equals\":\"yes\"}}");
            fieldsAdded = true;
        }

        if (fieldsAdded)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static Dictionary<string, (string Name, string Description)> GetModuleDescriptions()
    {
        return new Dictionary<string, (string Name, string Description)>
        {
            { DataCollectionFormModuleCodes.HealthyBabies, ("Healthy Babies offer and Parent-Carer Panels", "Tell us about the users who accessed the service over the past 3 months. This information will be included in your quarterly Management Information data collection.") },
            { DataCollectionFormModuleCodes.ServiceUsers, ("Service users", "Questions about service users") },
            { DataCollectionFormModuleCodes.WiderServiceUsers, ("Wider service users", "Questions about wider service users") },
            { DataCollectionFormModuleCodes.OutcomeScores, ("Outcome scores", "Questions about outcome scores") },
            { DataCollectionFormModuleCodes.BreastfeedingRates, ("Breastfeeding rates", "Questions about breastfeeding rates") }
        };
    }

    private static List<DataCollectionFormModule> CreateFormModules(UserId systemUserId)
    {
        var modules = new List<DataCollectionFormModule>();
        var lastChangedOn = new DateTime(2026, 12, 21, 0, 0, 0, DateTimeKind.Utc);

        modules.Add(CreateHealthyBabiesModule(lastChangedOn, systemUserId));
        modules.Add(CreateServiceUsersModule(lastChangedOn, systemUserId));
        modules.Add(CreateWiderServiceUsersModule(lastChangedOn, systemUserId));
        modules.Add(CreateOutcomeScoresModule(lastChangedOn, systemUserId));
        modules.Add(CreateBreastfeedingRatesModule(lastChangedOn, systemUserId));

        return modules;
    }

    private static DataCollectionFormModule CreateHealthyBabiesModule(DateTime lastChangedOn, UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.HealthyBabies,
            1,
            "Healthy Babies offer and Parent-Carer Panels",
            "Tell us about the users who accessed the service over the past 3 months. This information will be included in your quarterly Management Information data collection.",
            lastChangedOn);

        var section1 = module.AddSection(1, "Healthy Babies offer", "Tell us about the users who accessed the service over the past 3 months.");
        var section2 = module.AddSection(2, "Parent-Carer Panel meetings", "Questions about Parent-Carer Panel meetings and engagement.");
        var section3 = module.AddSection(3, "Parent-Carer Panel demographics", "Questions about Parent-Carer Panel membership demographics.");

        var field1 = module.AddField("SPCP01", "Have you published your Healthy Babies offer?", FieldType.Radio, 1, true);
        field1.SetSection(section1.Id);
        field1.SetConfiguration("{\"size\":\"small\"}");
        field1.AddOption("yes_both", "Yes, both physically and online", 1);
        field1.AddOption("yes_physical", "Yes, physical only", 2);
        field1.AddOption("yes_online", "Yes, online only", 3);
        field1.AddOption("no", "No", 4);

        var field2 = module.AddField("SPCP04", "What Parent-Carer Panel meetings and engagement do you have?", FieldType.Checkbox, 2, true);
        field2.SetSection(section2.Id);
        field2.SetConfiguration("{\"size\":\"small\"}");
        field2.AddOption("informal_contact", "Informal contact between meetings", 1);
        field2.AddOption("online_group", "Online group or forum", 2);
        field2.AddOption("panel_meetings", "Parent-Carer Panel meetings", 3);
        field2.AddOption("other", "Other", 4);

        var field3 = module.AddField("SPCP05", "How many Parent-Carer Panel meetings have occurred in the past quarter?", FieldType.Number, 3, true, "meetings", "Give a figure for the last 3 months, from 1 January to 31 March 2026.");
        field3.SetSection(section2.Id);

        var field4 = module.AddField("SPCP06", "Have you used feedback from Parent-Carer Panels to influence service design and delivery?", FieldType.Radio, 4, true);
        field4.SetSection(section2.Id);
        field4.SetConfiguration("{\"size\":\"small\"}");
        field4.AddOption("yes", "Yes", 1);
        field4.AddOption("no", "No", 2);

        var field5 = module.AddField("SPCP06a", "Case study upload", FieldType.File, 5, false, null, "This is optional. If you choose to upload one, use the case study template provided.");
        field5.SetSection(section2.Id);
        field5.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SPCP06\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");

        var field6 = module.AddField("SPCP07", "How many parents/carers are on your Parent Carer Panel?", FieldType.Number, 6, true, "parents/carers", "Give a figure as of 31 March 2026.");
        field6.SetSection(section3.Id);
        field6.SetConfiguration("{\"suffix\":\"parents/carers\",\"width\":\"5\"}");

        var field7 = module.AddField("SPCP08", "Do you have data on how many parents/carers are on your Parent Carer Panel, broken down by sex?", FieldType.Radio, 7, true);
        field7.SetSection(section3.Id);
        field7.SetConfiguration("{\"size\":\"small\"}");
        field7.AddOption("yes", "Yes", 1);
        field7.AddOption("no", "No", 2);

        var field8 = module.AddField("SPCP08a_female", "Female", FieldType.Number, 8, false, "parents/carers");
        field8.SetSection(section3.Id);
        field8.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SPCP08\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        field8.SetConfiguration("{\"suffix\":\"parents/carers\",\"width\":\"5\",\"group\":\"SPCP08a\",\"groupLabel\":\"How many members of the Parent Carer Panel are there, broken down by sex?\",\"groupHint\":\"For each sex, give a figure as of 31 March 2026.\",\"sumGroup\":\"sex\",\"maxSumField\":\"SPCP07\",\"min\":0}");

        var field9 = module.AddField("SPCP08a_male", "Male", FieldType.Number, 9, false, "parents/carers");
        field9.SetSection(section3.Id);
        field9.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SPCP08\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        field9.SetConfiguration("{\"suffix\":\"parents/carers\",\"width\":\"5\",\"group\":\"SPCP08a\",\"sumGroup\":\"sex\",\"maxSumField\":\"SPCP07\",\"min\":0}");

        var field10 = module.AddField("SPCP08a_other", "Other", FieldType.Number, 10, false, "parents/carers");
        field10.SetSection(section3.Id);
        field10.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SPCP08\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        field10.SetConfiguration("{\"suffix\":\"parents/carers\",\"width\":\"5\",\"group\":\"SPCP08a\",\"sumGroup\":\"sex\",\"maxSumField\":\"SPCP07\",\"min\":0}");

        var field11 = module.AddField("SPCP08a_prefer_not_to_say", "Prefer not to say", FieldType.Number, 11, false, "parents/carers");
        field11.SetSection(section3.Id);
        field11.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SPCP08\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        field11.SetConfiguration("{\"suffix\":\"parents/carers\",\"width\":\"5\",\"group\":\"SPCP08a\",\"sumGroup\":\"sex\",\"maxSumField\":\"SPCP07\",\"min\":0}");

        var field12 = module.AddField("SPCP09", "Do you have data on how many parents/carers are on your Parent Carer Panel, broken down by age?", FieldType.Radio, 12, true);
        field12.SetSection(section3.Id);
        field12.SetConfiguration("{\"size\":\"small\"}");
        field12.AddOption("yes", "Yes", 1);
        field12.AddOption("no", "No", 2);

        var field13 = module.AddField("SPCP09a_18_24", "18-24", FieldType.Number, 13, false, "parents/carers");
        field13.SetSection(section3.Id);
        field13.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SPCP09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        field13.SetConfiguration("{\"suffix\":\"parents/carers\",\"width\":\"5\",\"group\":\"SPCP09a\",\"groupLabel\":\"How many members of the Parent Carer Panel are there, broken down by age?\",\"groupHint\":\"For each age group, give a figure as of 31 March 2026.\",\"sumGroup\":\"age\",\"maxSumField\":\"SPCP07\",\"min\":0}");

        var field14 = module.AddField("SPCP09a_25_34", "25-34", FieldType.Number, 14, false, "parents/carers");
        field14.SetSection(section3.Id);
        field14.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SPCP09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        field14.SetConfiguration("{\"suffix\":\"parents/carers\",\"width\":\"5\",\"group\":\"SPCP09a\",\"sumGroup\":\"age\",\"maxSumField\":\"SPCP07\",\"min\":0}");

        var field15 = module.AddField("SPCP09a_35_44", "35-44", FieldType.Number, 15, false, "parents/carers");
        field15.SetSection(section3.Id);
        field15.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SPCP09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        field15.SetConfiguration("{\"suffix\":\"parents/carers\",\"width\":\"5\",\"group\":\"SPCP09a\",\"sumGroup\":\"age\",\"maxSumField\":\"SPCP07\",\"min\":0}");

        var field16 = module.AddField("SPCP09a_45_54", "45-54", FieldType.Number, 16, false, "parents/carers");
        field16.SetSection(section3.Id);
        field16.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SPCP09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        field16.SetConfiguration("{\"suffix\":\"parents/carers\",\"width\":\"5\",\"group\":\"SPCP09a\",\"sumGroup\":\"age\",\"maxSumField\":\"SPCP07\",\"min\":0}");

        var field17 = module.AddField("SPCP09a_55_64", "55-64", FieldType.Number, 17, false, "parents/carers");
        field17.SetSection(section3.Id);
        field17.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SPCP09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        field17.SetConfiguration("{\"suffix\":\"parents/carers\",\"width\":\"5\",\"group\":\"SPCP09a\",\"sumGroup\":\"age\",\"maxSumField\":\"SPCP07\",\"min\":0}");

        var field18 = module.AddField("SPCP09a_65_plus", "65+", FieldType.Number, 18, false, "parents/carers");
        field18.SetSection(section3.Id);
        field18.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SPCP09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        field18.SetConfiguration("{\"suffix\":\"parents/carers\",\"width\":\"5\",\"group\":\"SPCP09a\",\"sumGroup\":\"age\",\"maxSumField\":\"SPCP07\",\"min\":0}");

        var field19 = module.AddField("SPCP010", "Do you have data on how many parents/carers are on your Parent Carer Panel, broken down by ethnicity?", FieldType.Radio, 19, true);
        field19.SetSection(section3.Id);
        field19.SetConfiguration("{\"size\":\"small\"}");
        field19.AddOption("yes", "Yes", 1);
        field19.AddOption("no", "No", 2);

        var field20 = module.AddField("SPCP010a_white", "White/White British", FieldType.Number, 20, false, "parents/carers");
        field20.SetSection(section3.Id);
        field20.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SPCP010\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        field20.SetConfiguration("{\"suffix\":\"parents/carers\",\"width\":\"5\",\"group\":\"SPCP010a\",\"groupLabel\":\"How many members of the Parent Carer Panel are there, broken down by ethnicity?\",\"groupHint\":\"For each ethnicity, give a figure as of 31 March 2026.\",\"sumGroup\":\"ethnicity\",\"maxSumField\":\"SPCP07\",\"min\":0}");

        var field21 = module.AddField("SPCP010a_mixed", "Mixed/Multiple ethnic groups", FieldType.Number, 21, false, "parents/carers");
        field21.SetSection(section3.Id);
        field21.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SPCP010\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        field21.SetConfiguration("{\"suffix\":\"parents/carers\",\"width\":\"5\",\"group\":\"SPCP010a\",\"sumGroup\":\"ethnicity\",\"maxSumField\":\"SPCP07\",\"min\":0}");

        var field22 = module.AddField("SPCP010a_asian", "Asian/Asian British", FieldType.Number, 22, false, "parents/carers");
        field22.SetSection(section3.Id);
        field22.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SPCP010\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        field22.SetConfiguration("{\"suffix\":\"parents/carers\",\"width\":\"5\",\"group\":\"SPCP010a\",\"sumGroup\":\"ethnicity\",\"maxSumField\":\"SPCP07\",\"min\":0}");

        var field23 = module.AddField("SPCP010a_black", "Black/African/Caribbean/Black British", FieldType.Number, 23, false, "parents/carers");
        field23.SetSection(section3.Id);
        field23.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SPCP010\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        field23.SetConfiguration("{\"suffix\":\"parents/carers\",\"width\":\"5\",\"group\":\"SPCP010a\",\"sumGroup\":\"ethnicity\",\"maxSumField\":\"SPCP07\",\"min\":0}");

        var field24 = module.AddField("SPCP010a_other", "Other ethnic group", FieldType.Number, 24, false, "parents/carers");
        field24.SetSection(section3.Id);
        field24.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SPCP010\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        field24.SetConfiguration("{\"suffix\":\"parents/carers\",\"width\":\"5\",\"group\":\"SPCP010a\",\"sumGroup\":\"ethnicity\",\"maxSumField\":\"SPCP07\",\"min\":0}");

        var field25 = module.AddField("SPCP010a_prefer_not_to_say", "Prefer not to say", FieldType.Number, 25, false, "parents/carers");
        field25.SetSection(section3.Id);
        field25.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SPCP010\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        field25.SetConfiguration("{\"suffix\":\"parents/carers\",\"width\":\"5\",\"group\":\"SPCP010a\",\"sumGroup\":\"ethnicity\",\"maxSumField\":\"SPCP07\",\"min\":0}");

        if (systemUserId != UserId.Empty())
        {
            module.Publish(systemUserId);
        }

        return module;
    }

    private static DataCollectionFormModule CreateServiceUsersModule(DateTime lastChangedOn, UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.ServiceUsers,
            2,
            "Service users",
            "Tell us about the users who accessed the service over the past 3 months. This information will be included in your quarterly Management Information data collection.",
            lastChangedOn);

        // 4 sections = 5 steps (Step 5 is summary/check your answers)
        var section1 = module.AddSection(1, "Service delivery", "Tell us if this service was delivered this quarter.");
        var section2 = module.AddSection(2, "User numbers", "Tell us about the number of users who accessed this service.");
        var section3 = module.AddSection(3, "User demographics and data collection", "Tell us about the demographics of users and data collection practices.");
        var section4 = module.AddSection(4, "Clarification comments", "Provide any additional clarification if needed.");

        // ========== STEP 1: Service delivery (Section 1) ==========
        // QSU01: Was the service delivered?
        var qsu01 = module.AddField("QSU01", "Was the service '{serviceName}' delivered for users this quarter?", FieldType.Radio, 1, true);
        qsu01.SetSection(section1.Id);
        qsu01.AddOption("yes", "Yes", 1);
        qsu01.AddOption("no", "No", 2);

        // QSU02: Do you have data? (conditional on QSU01=yes, shown inline)
        var qsu02 = module.AddField("QSU02", "Do you have data on the number of users who accessed this service this quarter?", FieldType.Radio, 2, true, null, "For example, this can be centrally collected data or data received from delivery partners.");
        qsu02.SetSection(section1.Id);
        qsu02.AddOption("yes", "Yes", 1);
        qsu02.AddOption("no", "No", 2);
        qsu02.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU01\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");

        // ========== STEP 2: User numbers (Section 2) ==========
        // QSU03: Overall user count
        var qsu03 = module.AddField("QSU03", "Overall, how many service users have used this service?", FieldType.Number, 3, true, "users", "Give a figure for the last 3 months, from 1 October to 31 December 2026. This includes service users across all delivery methods.");
        qsu03.SetSection(section2.Id);
        qsu03.SetConditionalRules("{\"showWhen\":{\"allOf\":[{\"fieldKey\":\"QSU01\",\"equals\":\"yes\"},{\"fieldKey\":\"QSU02\",\"equals\":\"yes\"}]}}");
        qsu03.SetConfiguration("{\"suffix\":\"users\",\"width\":\"5\"}");

        // QSU04: Virtual users
        var qsu04 = module.AddField("QSU04", "How many service users have used this service virtually?", FieldType.Number, 4, false, "users", "Give a figure for the last 3 months, from 1 October to 31 December 2026. This includes virtual co-ordinator led group sessions and virtual 1:1 sessions, but excludes any self-directed online access (website visits) or app downloads.");
        qsu04.SetSection(section2.Id);
        qsu04.SetConditionalRules("{\"showWhen\":{\"allOf\":[{\"fieldKey\":\"QSU01\",\"equals\":\"yes\"},{\"fieldKey\":\"QSU02\",\"equals\":\"yes\"}]}}");
        qsu04.SetConfiguration("{\"suffix\":\"users\",\"width\":\"5\",\"maxSumField\":\"QSU03\",\"min\":0}");

        // QSU05: De-duplication method
        var qsu05 = module.AddField("QSU05", "Have you used a method for counting unique individuals who attend this service?", FieldType.Radio, 5, true, null, "For example, are you able to de-duplicate users who attend the service multiple times during the quarter?");
        qsu05.SetSection(section2.Id);
        qsu05.AddOption("yes", "Yes", 1);
        qsu05.AddOption("no", "No", 2);
        qsu05.SetConditionalRules("{\"showWhen\":{\"allOf\":[{\"fieldKey\":\"QSU01\",\"equals\":\"yes\"},{\"fieldKey\":\"QSU02\",\"equals\":\"yes\"}]}}");

        // QSU06: User types (checkboxes with exclusive "Unknown" option)
        var qsu06 = module.AddField("QSU06", "What type(s) of users are included in your user numbers?", FieldType.Checkbox, 6, true, null, "Please select all that apply. If you don't have the data, select 'Unknown'.");
        qsu06.SetSection(section2.Id);
        qsu06.AddOption("primary_carer", "Primary carer", 1);
        qsu06.AddOption("co_parent", "Co-parent/Partner/Spouse/Other adult", 2);
        qsu06.AddOption("child", "Child", 3);
        qsu06.AddOption("sibling", "Sibling", 4);
        qsu06.AddOption("other", "Other", 5);
        qsu06.AddOption("unknown", "Unknown", 6);
        qsu06.SetConditionalRules("{\"showWhen\":{\"allOf\":[{\"fieldKey\":\"QSU01\",\"equals\":\"yes\"},{\"fieldKey\":\"QSU02\",\"equals\":\"yes\"}]}}");
        qsu06.SetConfiguration("{\"exclusiveOptions\":[\"unknown\"]}");

        // ========== STEP 3: User demographics and data collection (Section 3) ==========
        // QSU07: Ethnicity data available?
        var qsu07 = module.AddField("QSU07", "Do you have data on how many parents or carers have used this service, broken down by ethnicity?", FieldType.Radio, 7, true);
        qsu07.SetSection(section3.Id);
        qsu07.AddOption("yes", "Yes", 1);
        qsu07.AddOption("no", "No", 2);
        qsu07.SetConditionalRules("{\"showWhen\":{\"allOf\":[{\"fieldKey\":\"QSU01\",\"equals\":\"yes\"},{\"fieldKey\":\"QSU02\",\"equals\":\"yes\"}]}}");

        // QSU08: Ethnicity breakdown (conditional grouped inputs)
        var qsu08_white = module.AddField("QSU08_white", "White/White British", FieldType.Number, 8, false, "parents/carers");
        qsu08_white.SetSection(section3.Id);
        qsu08_white.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU07\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu08_white.SetConfiguration("{\"group\":\"QSU08\",\"groupLabel\":\"How many parents or carers have used this service, broken down by ethnicity?\",\"groupHint\":\"For each ethnic group, give a figure for the last 3 months, from 1 October to 31 December 2026.\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"ethnicity\",\"maxSumField\":\"QSU03\",\"min\":0}");

        var qsu08_mixed = module.AddField("QSU08_mixed", "Mixed/Multiple ethnic groups", FieldType.Number, 9, false, "parents/carers");
        qsu08_mixed.SetSection(section3.Id);
        qsu08_mixed.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU07\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu08_mixed.SetConfiguration("{\"group\":\"QSU08\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"ethnicity\",\"maxSumField\":\"QSU03\",\"min\":0}");

        var qsu08_asian = module.AddField("QSU08_asian", "Asian/Asian British", FieldType.Number, 10, false, "parents/carers");
        qsu08_asian.SetSection(section3.Id);
        qsu08_asian.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU07\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu08_asian.SetConfiguration("{\"group\":\"QSU08\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"ethnicity\",\"maxSumField\":\"QSU03\",\"min\":0}");

        var qsu08_black = module.AddField("QSU08_black", "Black/African/Caribbean/Black British", FieldType.Number, 11, false, "parents/carers");
        qsu08_black.SetSection(section3.Id);
        qsu08_black.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU07\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu08_black.SetConfiguration("{\"group\":\"QSU08\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"ethnicity\",\"maxSumField\":\"QSU03\",\"min\":0}");

        var qsu08_other = module.AddField("QSU08_other", "Other ethnic group", FieldType.Number, 12, false, "parents/carers");
        qsu08_other.SetSection(section3.Id);
        qsu08_other.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU07\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu08_other.SetConfiguration("{\"group\":\"QSU08\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"ethnicity\",\"maxSumField\":\"QSU03\",\"min\":0}");

        var qsu08_prefer_not = module.AddField("QSU08_prefer_not_to_say", "Prefer not to say", FieldType.Number, 13, false, "parents/carers");
        qsu08_prefer_not.SetSection(section3.Id);
        qsu08_prefer_not.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU07\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu08_prefer_not.SetConfiguration("{\"group\":\"QSU08\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"ethnicity\",\"maxSumField\":\"QSU03\",\"min\":0}");

        // QSU09: IMD data available?
        var qsu09 = module.AddField("QSU09", "Do you have data on how many parents or carers have used this service, broken down by the Index of Multiple Deprivation (IMD)?", FieldType.Radio, 14, true);
        qsu09.SetSection(section3.Id);
        qsu09.AddOption("yes", "Yes", 1);
        qsu09.AddOption("no", "No", 2);
        qsu09.SetConditionalRules("{\"showWhen\":{\"allOf\":[{\"fieldKey\":\"QSU01\",\"equals\":\"yes\"},{\"fieldKey\":\"QSU02\",\"equals\":\"yes\"}]}}");

        // QSU10: IMD breakdown (10 deciles)
        var qsu10_decile1 = module.AddField("QSU10_decile1", "IMD Decile 1", FieldType.Number, 15, false, "parents/carers");
        qsu10_decile1.SetSection(section3.Id);
        qsu10_decile1.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu10_decile1.SetConfiguration("{\"group\":\"QSU10\",\"groupLabel\":\"How many parents or carers have used this service, broken down by IMD?\",\"groupHint\":\"For each IMD decile, give a figure for the last 3 months, from 1 October to 31 December 2026.\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"imd\",\"maxSumField\":\"QSU03\",\"min\":0}");

        var qsu10_decile2 = module.AddField("QSU10_decile2", "IMD Decile 2", FieldType.Number, 16, false, "parents/carers");
        qsu10_decile2.SetSection(section3.Id);
        qsu10_decile2.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu10_decile2.SetConfiguration("{\"group\":\"QSU10\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"imd\",\"maxSumField\":\"QSU03\",\"min\":0}");

        var qsu10_decile3 = module.AddField("QSU10_decile3", "IMD Decile 3", FieldType.Number, 17, false, "parents/carers");
        qsu10_decile3.SetSection(section3.Id);
        qsu10_decile3.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu10_decile3.SetConfiguration("{\"group\":\"QSU10\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"imd\",\"maxSumField\":\"QSU03\",\"min\":0}");

        var qsu10_decile4 = module.AddField("QSU10_decile4", "IMD Decile 4", FieldType.Number, 18, false, "parents/carers");
        qsu10_decile4.SetSection(section3.Id);
        qsu10_decile4.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu10_decile4.SetConfiguration("{\"group\":\"QSU10\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"imd\",\"maxSumField\":\"QSU03\",\"min\":0}");

        var qsu10_decile5 = module.AddField("QSU10_decile5", "IMD Decile 5", FieldType.Number, 19, false, "parents/carers");
        qsu10_decile5.SetSection(section3.Id);
        qsu10_decile5.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu10_decile5.SetConfiguration("{\"group\":\"QSU10\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"imd\",\"maxSumField\":\"QSU03\",\"min\":0}");

        var qsu10_decile6 = module.AddField("QSU10_decile6", "IMD Decile 6", FieldType.Number, 20, false, "parents/carers");
        qsu10_decile6.SetSection(section3.Id);
        qsu10_decile6.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu10_decile6.SetConfiguration("{\"group\":\"QSU10\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"imd\",\"maxSumField\":\"QSU03\",\"min\":0}");

        var qsu10_decile7 = module.AddField("QSU10_decile7", "IMD Decile 7", FieldType.Number, 21, false, "parents/carers");
        qsu10_decile7.SetSection(section3.Id);
        qsu10_decile7.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu10_decile7.SetConfiguration("{\"group\":\"QSU10\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"imd\",\"maxSumField\":\"QSU03\",\"min\":0}");

        var qsu10_decile8 = module.AddField("QSU10_decile8", "IMD Decile 8", FieldType.Number, 22, false, "parents/carers");
        qsu10_decile8.SetSection(section3.Id);
        qsu10_decile8.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu10_decile8.SetConfiguration("{\"group\":\"QSU10\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"imd\",\"maxSumField\":\"QSU03\",\"min\":0}");

        var qsu10_decile9 = module.AddField("QSU10_decile9", "IMD Decile 9", FieldType.Number, 23, false, "parents/carers");
        qsu10_decile9.SetSection(section3.Id);
        qsu10_decile9.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu10_decile9.SetConfiguration("{\"group\":\"QSU10\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"imd\",\"maxSumField\":\"QSU03\",\"min\":0}");

        var qsu10_decile10 = module.AddField("QSU10_decile10", "IMD Decile 10", FieldType.Number, 24, false, "parents/carers");
        qsu10_decile10.SetSection(section3.Id);
        qsu10_decile10.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu10_decile10.SetConfiguration("{\"group\":\"QSU10\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"imd\",\"maxSumField\":\"QSU03\",\"min\":0}");

        // QSU11: Sex data available?
        var qsu11 = module.AddField("QSU11", "Do you have data on how many parents or carers have used this service, broken down by sex?", FieldType.Radio, 25, true);
        qsu11.SetSection(section3.Id);
        qsu11.AddOption("yes", "Yes", 1);
        qsu11.AddOption("no", "No", 2);
        qsu11.SetConditionalRules("{\"showWhen\":{\"allOf\":[{\"fieldKey\":\"QSU01\",\"equals\":\"yes\"},{\"fieldKey\":\"QSU02\",\"equals\":\"yes\"}]}}");

        // QSU12: Sex breakdown
        var qsu12_female = module.AddField("QSU12_female", "Female", FieldType.Number, 26, false, "parents/carers");
        qsu12_female.SetSection(section3.Id);
        qsu12_female.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU11\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu12_female.SetConfiguration("{\"group\":\"QSU12\",\"groupLabel\":\"How many parents or carers have used this service, broken down by sex?\",\"groupHint\":\"For each sex, give a figure for the last 3 months, from 1 October to 31 December 2026.\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"sex\",\"maxSumField\":\"QSU03\",\"min\":0}");

        var qsu12_male = module.AddField("QSU12_male", "Male", FieldType.Number, 27, false, "parents/carers");
        qsu12_male.SetSection(section3.Id);
        qsu12_male.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU11\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu12_male.SetConfiguration("{\"group\":\"QSU12\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"sex\",\"maxSumField\":\"QSU03\",\"min\":0}");

        var qsu12_other = module.AddField("QSU12_other", "Other", FieldType.Number, 28, false, "parents/carers");
        qsu12_other.SetSection(section3.Id);
        qsu12_other.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU11\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu12_other.SetConfiguration("{\"group\":\"QSU12\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"sex\",\"maxSumField\":\"QSU03\",\"min\":0}");

        var qsu12_prefer_not = module.AddField("QSU12_prefer_not_to_say", "Prefer not to say", FieldType.Number, 29, false, "parents/carers");
        qsu12_prefer_not.SetSection(section3.Id);
        qsu12_prefer_not.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU11\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu12_prefer_not.SetConfiguration("{\"group\":\"QSU12\",\"suffix\":\"parents/carers\",\"width\":\"5\",\"sumGroup\":\"sex\",\"maxSumField\":\"QSU03\",\"min\":0}");

        // QSU13: Outcome scores collected?
        var qsu13 = module.AddField("QSU13", "Does the service provider collect pre- and post-outcome scores for users who access this service?", FieldType.Radio, 30, true);
        qsu13.SetSection(section3.Id);
        qsu13.AddOption("yes", "Yes", 1);
        qsu13.AddOption("no", "No", 2);
        qsu13.SetConditionalRules("{\"showWhen\":{\"allOf\":[{\"fieldKey\":\"QSU01\",\"equals\":\"yes\"},{\"fieldKey\":\"QSU02\",\"equals\":\"yes\"}]}}");

        // QSU14: Number of users with outcome scores
        var qsu14 = module.AddField("QSU14", "How many of all service users completed a pre- and post-outcome score for at least one service they accessed during the quarter?", FieldType.Number, 31, false, "users", "Give a figure for the last 3 months, from 1 October to 31 December 2026.");
        qsu14.SetSection(section3.Id);
        qsu14.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU13\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu14.SetConfiguration("{\"suffix\":\"users\",\"width\":\"5\",\"maxSumField\":\"QSU03\",\"min\":0}");

        // QSU15: Waiting time data collected?
        var qsu15 = module.AddField("QSU15", "Does the service provider collect waiting time data for this service?", FieldType.Radio, 32, true);
        qsu15.SetSection(section3.Id);
        qsu15.AddOption("yes", "Yes", 1);
        qsu15.AddOption("no", "No", 2);
        qsu15.SetConditionalRules("{\"showWhen\":{\"allOf\":[{\"fieldKey\":\"QSU01\",\"equals\":\"yes\"},{\"fieldKey\":\"QSU02\",\"equals\":\"yes\"}]}}");

        // QSU16: Average waiting time
        var qsu16 = module.AddField("QSU16", "What is the average waiting time for service users from referral to receiving their first appointment or session?", FieldType.Number, 33, false, "days", "Give the average for the last 3 months, from 1 October to 31 December 2026, in number of days.");
        qsu16.SetSection(section3.Id);
        qsu16.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU15\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        qsu16.SetConfiguration("{\"suffix\":\"days\",\"width\":\"5\"}");

        // ========== STEP 4: Clarification comments (Section 4) ==========
        // QSU17: Clarification comments
        var qsu17 = module.AddField("QSU17", "Clarification comments", FieldType.Textarea, 34, false, null, "Please provide any relevant additional information – for example if one of the questions doesn't properly capture the information about users. Please only add comments in this section if necessary.");
        qsu17.SetSection(section4.Id);
        qsu17.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"QSU01\",\"equals\":\"yes\"}}");

        return module;
    }

    private static DataCollectionFormModule CreateWiderServiceUsersModule(DateTime lastChangedOn, UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.WiderServiceUsers,
            3,
            "Wider service users",
            "Tell us about the users who accessed the wider services in this category over the past 3 months. This information will be included in your quarterly Management Information data collection.",
            lastChangedOn);

        var section1 = module.AddSection(1, "Wider Service Users", "Provide data for {widerServiceCategoryName}");

        var field1 = module.AddField(
            "QWSU01",
            "How many unique service users have used wider services in this category?",
            FieldType.Number,
            1,
            true,
            null,
            "Give a figure for the last 3 months, from {startDate} to {endDate}.");
        field1.SetSection(section1.Id);

        if (systemUserId != UserId.Empty())
        {
            module.Publish(systemUserId);
        }

        return module;
    }

    private static DataCollectionFormModule CreateOutcomeScoresModule(DateTime lastChangedOn, UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.OutcomeScores,
            4,
            "Outcome scores",
            "Tell us about outcome scores for your service users. If services delivered by your local authority, or arranged on its behalf through the Best Start Family Hubs and Start for Life programme, collect scores before and after an intervention, provide these outcome scores for service users in the past 3 months.",
            lastChangedOn);

        // Section 1: Service Selection
        var section1 = module.AddSection(1, "Service selection", "Tell us about outcome scores for a service user who used one of your services in the past 3 months. This information will be included in your quarterly Management Information data collection.");

        // Section 2: Outcome Measures Selection
        var section2 = module.AddSection(2, "Outcome measures", "Tell us about outcome scores for a service user who used one of your services in the past 3 months. This information will be included in your quarterly Management Information data collection.");

        // Section 3: User Demographics
        var section3 = module.AddSection(3, "User demographics", "Tell us about outcome scores for a service user who used one of your services in the past 3 months. This information will be included in your quarterly Management Information data collection.");

        // Section 4: Pre and Post Intervention Scores
        var section4 = module.AddSection(4, "Intervention scores", "Tell us about outcome scores for a service user who used one of your services in the past 3 months. This information will be included in your quarterly Management Information data collection.");

        // Section 5: Clarification Comments
        var section5 = module.AddSection(5, "Clarification comments", "Tell us about outcome scores for a service user who used one of your services in the past 3 months. This information will be included in your quarterly Management Information data collection.");

        // PPS01 - Service Selection (Step 1)
        var pps01 = module.AddField("PPS01", "What service does the outcome measure relate to?", FieldType.Select, 1, true, null, null);
        pps01.SetSection(section1.Id);
        pps01.SetConfiguration("{\"placeholder\":\"Select a service...\",\"dynamicOptions\":\"services\"}");

        // PPS02 - Outcome Measures Selection (Step 2)
        var pps02 = module.AddField("PPS02", "What outcome measure(s) did this parent or carer complete?", FieldType.Checkbox, 2, true, null, "Select all which apply.");
        pps02.SetSection(section2.Id);
        pps02.AddOption("asq3", "ASQ-3", 1);
        pps02.AddOption("cprs_sf", "CPRS-SF", 2);
        pps02.AddOption("gad7", "GAD-7", 3);
        pps02.AddOption("hle", "HLE", 4);
        pps02.AddOption("kpcs", "KPCS", 5);
        pps02.AddOption("mors_sf", "MORS-SF", 6);
        pps02.AddOption("phq9", "PHQ-9", 7);
        pps02.AddOption("pss", "PSS", 8);
        pps02.AddOption("swemwbs", "SWEMWBS", 9);
        pps02.AddOption("other", "Other", 10);

        // PPS03 - Ethnicity (Step 3)
        var pps03 = module.AddField("PPS03", "What is the user's ethnicity?", FieldType.Radio, 3, true, null, null);
        pps03.SetSection(section3.Id);
        pps03.AddOption("white", "White or White British", 1);
        pps03.AddOption("mixed", "Mixed or multiple ethnic groups", 2);
        pps03.AddOption("asian", "Asian or Asian British", 3);
        pps03.AddOption("black", "Black, African, Caribbean or Black British", 4);
        pps03.AddOption("other", "Other ethnic group", 5);
        pps03.AddOption("prefer_not_to_say", "Prefer not to say", 6);
        pps03.AddOption("unknown", "Unknown", 7);

        // PPS04 - Biological Sex (Step 3)
        var pps04 = module.AddField("PPS04", "What is the user's biological sex?", FieldType.Radio, 4, true, null, null);
        pps04.SetSection(section3.Id);
        pps04.AddOption("male", "Male", 1);
        pps04.AddOption("female", "Female", 2);
        pps04.AddOption("other", "Other", 3);
        pps04.AddOption("prefer_not_to_say", "Prefer not to say", 4);
        pps04.AddOption("unknown", "Unknown", 5);

        // PPS05 - IDACI Decile (Step 3)
        var pps05 = module.AddField("PPS05", "What is the user's IDACI decile?", FieldType.Select, 5, true, null, null);
        pps05.SetSection(section3.Id);
        pps05.SetConfiguration("{\"placeholder\":\"Select a decile...\"}");
        pps05.AddOption("1", "1 (most deprived)", 1);
        pps05.AddOption("2", "2", 2);
        pps05.AddOption("3", "3", 3);
        pps05.AddOption("4", "4", 4);
        pps05.AddOption("5", "5", 5);
        pps05.AddOption("6", "6", 6);
        pps05.AddOption("7", "7", 7);
        pps05.AddOption("8", "8", 8);
        pps05.AddOption("9", "9", 9);
        pps05.AddOption("10", "10 (least deprived)", 10);
        pps05.AddOption("unknown", "Unknown", 11);

        // PPS06 - ASQ-3 Scores (Step 4)
        var pps06_pre = module.AddField("PPS06_pre", "Pre-intervention ASQ-3 score", FieldType.Number, 6, false, null, null);
        pps06_pre.SetSection(section4.Id);
        pps06_pre.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"asq3\"]}}");
        pps06_pre.SetConfiguration("{\"group\":\"PPS06\",\"groupLabel\":\"What were this parent or carer's ASQ-3 scores pre- and post-intervention?\",\"width\":\"5\"}");

        var pps06_post = module.AddField("PPS06_post", "Post-intervention ASQ-3 score", FieldType.Number, 7, false, null, null);
        pps06_post.SetSection(section4.Id);
        pps06_post.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"asq3\"]}}");
        pps06_post.SetConfiguration("{\"group\":\"PPS06\",\"width\":\"5\"}");

        // PPS07 - CPRS-SF Scores (Step 4)
        var pps07_pre = module.AddField("PPS07_pre", "Pre-intervention CPRS-SF score", FieldType.Number, 8, false, null, null);
        pps07_pre.SetSection(section4.Id);
        pps07_pre.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"cprs_sf\"]}}");
        pps07_pre.SetConfiguration("{\"group\":\"PPS07\",\"groupLabel\":\"What were this parent or carer's CPRS-SF scores pre- and post-intervention?\",\"width\":\"5\"}");

        var pps07_post = module.AddField("PPS07_post", "Post-intervention CPRS-SF score", FieldType.Number, 9, false, null, null);
        pps07_post.SetSection(section4.Id);
        pps07_post.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"cprs_sf\"]}}");
        pps07_post.SetConfiguration("{\"group\":\"PPS07\",\"width\":\"5\"}");

        // PPS08 - GAD-7 Scores (Step 4)
        var pps08_pre = module.AddField("PPS08_pre", "Pre-intervention GAD-7 score", FieldType.Number, 10, false, null, null);
        pps08_pre.SetSection(section4.Id);
        pps08_pre.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"gad7\"]}}");
        pps08_pre.SetConfiguration("{\"group\":\"PPS08\",\"groupLabel\":\"What were this parent or carer's GAD-7 scores pre- and post-intervention?\",\"width\":\"5\"}");

        var pps08_post = module.AddField("PPS08_post", "Post-intervention GAD-7 score", FieldType.Number, 11, false, null, null);
        pps08_post.SetSection(section4.Id);
        pps08_post.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"gad7\"]}}");
        pps08_post.SetConfiguration("{\"group\":\"PPS08\",\"width\":\"5\"}");

        // PPS09 - HLE Scores (Step 4)
        var pps09_pre = module.AddField("PPS09_pre", "Pre-intervention HLE score", FieldType.Number, 12, false, null, null);
        pps09_pre.SetSection(section4.Id);
        pps09_pre.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"hle\"]}}");
        pps09_pre.SetConfiguration("{\"group\":\"PPS09\",\"groupLabel\":\"What were this parent or carer's HLE scores pre- and post-intervention?\",\"width\":\"5\"}");

        var pps09_post = module.AddField("PPS09_post", "Post-intervention HLE score", FieldType.Number, 13, false, null, null);
        pps09_post.SetSection(section4.Id);
        pps09_post.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"hle\"]}}");
        pps09_post.SetConfiguration("{\"group\":\"PPS09\",\"width\":\"5\"}");

        // PPS10 - KPCS Scores (Step 4)
        var pps10_pre = module.AddField("PPS10_pre", "Pre-intervention KPCS score", FieldType.Number, 14, false, null, null);
        pps10_pre.SetSection(section4.Id);
        pps10_pre.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"kpcs\"]}}");
        pps10_pre.SetConfiguration("{\"group\":\"PPS10\",\"groupLabel\":\"What were this parent or carer's KPCS scores pre- and post-intervention?\",\"width\":\"5\"}");

        var pps10_post = module.AddField("PPS10_post", "Post-intervention KPCS score", FieldType.Number, 15, false, null, null);
        pps10_post.SetSection(section4.Id);
        pps10_post.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"kpcs\"]}}");
        pps10_post.SetConfiguration("{\"group\":\"PPS10\",\"width\":\"5\"}");

        // PPS11 - MORS-SF Scores (Step 4)
        var pps11_pre = module.AddField("PPS11_pre", "Pre-intervention MORS-SF score", FieldType.Number, 16, false, null, null);
        pps11_pre.SetSection(section4.Id);
        pps11_pre.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"mors_sf\"]}}");
        pps11_pre.SetConfiguration("{\"group\":\"PPS11\",\"groupLabel\":\"What were this parent or carer's MORS-SF scores pre- and post-intervention?\",\"width\":\"5\"}");

        var pps11_post = module.AddField("PPS11_post", "Post-intervention MORS-SF score", FieldType.Number, 17, false, null, null);
        pps11_post.SetSection(section4.Id);
        pps11_post.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"mors_sf\"]}}");
        pps11_post.SetConfiguration("{\"group\":\"PPS11\",\"width\":\"5\"}");

        // PPS12 - PHQ-9 Scores (Step 4)
        var pps12_pre = module.AddField("PPS12_pre", "Pre-intervention PHQ-9 score", FieldType.Number, 18, false, null, null);
        pps12_pre.SetSection(section4.Id);
        pps12_pre.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"phq9\"]}}");
        pps12_pre.SetConfiguration("{\"group\":\"PPS12\",\"groupLabel\":\"What were this parent or carer's PHQ-9 scores pre- and post-intervention?\",\"width\":\"5\"}");

        var pps12_post = module.AddField("PPS12_post", "Post-intervention PHQ-9 score", FieldType.Number, 19, false, null, null);
        pps12_post.SetSection(section4.Id);
        pps12_post.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"phq9\"]}}");
        pps12_post.SetConfiguration("{\"group\":\"PPS12\",\"width\":\"5\"}");

        // PPS13 - PSS Scores (Step 4)
        var pps13_pre = module.AddField("PPS13_pre", "Pre-intervention PSS score", FieldType.Number, 20, false, null, null);
        pps13_pre.SetSection(section4.Id);
        pps13_pre.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"pss\"]}}");
        pps13_pre.SetConfiguration("{\"group\":\"PPS13\",\"groupLabel\":\"What were this parent or carer's PSS scores pre- and post-intervention?\",\"width\":\"5\"}");

        var pps13_post = module.AddField("PPS13_post", "Post-intervention PSS score", FieldType.Number, 21, false, null, null);
        pps13_post.SetSection(section4.Id);
        pps13_post.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"pss\"]}}");
        pps13_post.SetConfiguration("{\"group\":\"PPS13\",\"width\":\"5\"}");

        // PPS14 - SWEMWBS Scores (Step 4)
        var pps14_pre = module.AddField("PPS14_pre", "Pre-intervention SWEMWBS score", FieldType.Number, 22, false, null, null);
        pps14_pre.SetSection(section4.Id);
        pps14_pre.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"swemwbs\"]}}");
        pps14_pre.SetConfiguration("{\"group\":\"PPS14\",\"groupLabel\":\"What were this parent or carer's SWEMWBS scores pre- and post-intervention?\",\"width\":\"5\"}");

        var pps14_post = module.AddField("PPS14_post", "Post-intervention SWEMWBS score", FieldType.Number, 23, false, null, null);
        pps14_post.SetSection(section4.Id);
        pps14_post.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"swemwbs\"]}}");
        pps14_post.SetConfiguration("{\"group\":\"PPS14\",\"width\":\"5\"}");

        // PPS15 - Other Scores (Step 4)
        var pps15_pre = module.AddField("PPS15_pre", "Pre-intervention 'Other' score", FieldType.Number, 24, false, null, null);
        pps15_pre.SetSection(section4.Id);
        pps15_pre.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"other\"]}}");
        pps15_pre.SetConfiguration("{\"group\":\"PPS15\",\"groupLabel\":\"What were this parent or carer's 'Other' scores pre- and post-intervention?\",\"width\":\"5\"}");

        var pps15_post = module.AddField("PPS15_post", "Post-intervention 'Other' score", FieldType.Number, 25, false, null, null);
        pps15_post.SetSection(section4.Id);
        pps15_post.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"other\"]}}");
        pps15_post.SetConfiguration("{\"group\":\"PPS15\",\"width\":\"5\"}");

        // PPS17 - Clarification Comments (Step 5)
        var pps17 = module.AddField("PPS17", "Clarification comments", FieldType.Textarea, 26, false, null, "Provide more information about breastfeeding rates (optional). Your comments from previous quarterly collections are shown for reference. Update them if needed.");
        pps17.SetSection(section5.Id);

        if (systemUserId != UserId.Empty())
        {
            module.Publish(systemUserId);
        }

        return module;
    }

    private static DataCollectionFormModule CreateBreastfeedingRatesModule(DateTime lastChangedOn, UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.BreastfeedingRates,
            5,
            "Breastfeeding rates",
            "Tell us about breastfeeding rates in your local authority over the past 3 months. This information will be included in your quarterly Management Information data collection.",
            lastChangedOn);

        // Step 1: Breastfeeding rates data
        var section1 = module.AddSection(1, "Breastfeeding Rates", "Tell us about breastfeeding rates in your local authority over the past 3 months. This information will be included in your quarterly Management Information data collection.");

        // BR01 - Do you have data on average breastfeeding rates at initiation
        var br01 = module.AddField("BR01", "Do you have data on average breastfeeding rates at initiation over the last quarter?", FieldType.Radio, 1, true, null, null);
        br01.SetSection(section1.Id);
        br01.AddOption("yes", "Yes", 1);
        br01.AddOption("no", "No", 2);

        // BR01a - Conditional number field (inline under Yes option)
        var br01a = module.AddField("BR01a", "What were the average breastfeeding rates at initiation over the last quarter?", FieldType.Number, 2, false, null, "Give an overall figure for your local authority for the last 3 months, from 1 January to 31 March 2026. Enter a whole number between 0 and 100 without a percentage sign.");
        br01a.SetSection(section1.Id);
        br01a.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"BR01\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        br01a.SetConfiguration("{\"suffix\":\"%\",\"width\":\"3\",\"min\":0,\"max\":100}");

        // BR02 - Do you have data on average breastfeeding rates at 6-8 weeks
        var br02 = module.AddField("BR02", "Do you have data on average breastfeeding rates at 6-8 weeks over the last quarter?", FieldType.Radio, 3, true, null, null);
        br02.SetSection(section1.Id);
        br02.AddOption("yes", "Yes", 1);
        br02.AddOption("no", "No", 2);

        // BR02a - Conditional number field (inline under Yes option)
        var br02a = module.AddField("BR02a", "What were the average breastfeeding rates at 6-8 weeks over the last quarter?", FieldType.Number, 4, false, null, "Give an overall figure for your local authority for the last 3 months, from 1 January to 31 March 2026. Enter a whole number between 0 and 100 without a percentage sign.");
        br02a.SetSection(section1.Id);
        br02a.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"BR02\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        br02a.SetConfiguration("{\"suffix\":\"%\",\"width\":\"3\",\"min\":0,\"max\":100}");

        // Step 2: Clarification comments
        var section2 = module.AddSection(2, "Clarification comments", "Provide any additional information about breastfeeding rates.");

        // BR03 - Clarification comments
        var br03 = module.AddField("BR03", "Clarification comments", FieldType.Textarea, 5, false, null, "Provide more information about breastfeeding rates (optional). Your comments from previous quarterly collections are shown for reference. Update them if needed.");
        br03.SetSection(section2.Id);

        if (systemUserId != UserId.Empty())
        {
            module.Publish(systemUserId);
        }

        return module;
    }

    private static async Task RecreateAssignmentsAsync(
        ApplicationDbContext context,
        List<DataCollectionFormModule> newModules,
        CancellationToken cancellationToken)
    {
        // Get all data collections that should have form module assignments
        var dataCollections = await context.DataCollections
            .ToListAsync(cancellationToken);

        foreach (var dc in dataCollections)
        {
            // Assign all form modules to each data collection
            foreach (var module in newModules)
            {
                var assignment = DataCollectionFormModuleAssignment.Create(dc.Id, module.Id);
                await context.Set<DataCollectionFormModuleAssignment>().AddAsync(assignment, cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}