using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seeders;

public static class DeliveryPlanFormModuleSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        var systemUser = await context.Users.FirstOrDefaultAsync(cancellationToken);
        var systemUserId = systemUser?.Id ?? UserId.Empty();

        var existingModules = await context.DataCollectionFormModules
            .Include(m => m.Sections)
            .Include(m => m.Fields)
            .ToListAsync(cancellationToken);

        var modulesAdded = false;

        // Delivery plan modules from the 2026 data map (Focus onwards, sections 1–9).
        var newModuleFactories = new (string Code, Func<UserId, DataCollectionFormModule> Factory)[]
        {
            (DataCollectionFormModuleCodes.Focus,              CreateFocusModule),
            (DataCollectionFormModuleCodes.DeliveryGrant,      CreateDeliveryGrantModule),
            (DataCollectionFormModuleCodes.Parenting,          CreateParentingModule),
            (DataCollectionFormModuleCodes.Hle,                CreateHleModule),
            (DataCollectionFormModuleCodes.Send,               CreateSendModule),
            (DataCollectionFormModuleCodes.PnmhAndPir,         CreatePnmhAndPirModule),
            (DataCollectionFormModuleCodes.InfantFeeding,      CreateInfantFeedingModule),
            (DataCollectionFormModuleCodes.HealthyBabiesOffer, CreateHealthyBabiesOfferModule),
            (DataCollectionFormModuleCodes.RolloverSpend,      CreateRolloverSpendModule),
        };

        foreach (var (code, factory) in newModuleFactories)
        {
            if (existingModules.All(m => m.Code != code))
            {
                await context.DataCollectionFormModules.AddAsync(factory(systemUserId), cancellationToken);
                modulesAdded = true;
            }
        }

        if (modulesAdded)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        var populators = new Dictionary<string, Action<DataCollectionFormModule>>
        {
            [DataCollectionFormModuleCodes.Focus] = PopulateFocusFields,
            [DataCollectionFormModuleCodes.DeliveryGrant] = PopulateDeliveryGrantFields,
            [DataCollectionFormModuleCodes.Parenting] = PopulateParentingFields,
            [DataCollectionFormModuleCodes.Hle] = PopulateHleFields,
            [DataCollectionFormModuleCodes.Send] = PopulateSendFields,
            [DataCollectionFormModuleCodes.PnmhAndPir] = PopulatePnmhAndPirFields,
            [DataCollectionFormModuleCodes.InfantFeeding] = PopulateInfantFeedingFields,
            [DataCollectionFormModuleCodes.HealthyBabiesOffer] = PopulateHealthyBabiesOfferFields,
            [DataCollectionFormModuleCodes.RolloverSpend] = PopulateRolloverSpendFields,
        };

        // Defined here so the AllowsMultipleActivities loop below can use it.
        var multiActivityCodes = new HashSet<string>
        {
            DataCollectionFormModuleCodes.DeliveryGrant,
            DataCollectionFormModuleCodes.Parenting,
            DataCollectionFormModuleCodes.Hle,
            DataCollectionFormModuleCodes.Send,
            DataCollectionFormModuleCodes.PnmhAndPir,
            DataCollectionFormModuleCodes.InfantFeeding,
            DataCollectionFormModuleCodes.HealthyBabiesOffer,
        };

        // Reconcile field-key and help-text renames for modules that already have submission data.
        // Matches actual fields to expected fields by display order, so code renames in the seeder
        // propagate to existing data without requiring a manual migration per change.
        foreach (var module in existingModules.Where(m => populators.ContainsKey(m.Code)))
        {
            var hasSubmissionData = await context.FormSubmissions
                .AnyAsync(fs => fs.FormModuleId == module.Id, cancellationToken);

            if (!hasSubmissionData)
            {
                continue;
            }

            var shadow = DataCollectionFormModule.Create(
                DataCollectionFormModuleId.New(), module.Code, 1, module.Code);
            populators[module.Code](shadow);

            var expectedByOrder = shadow.Fields.ToDictionary(f => f.DisplayOrder, f => (f.FieldKey, f.HelpText));

            var changed = false;
            var keyRenames = new List<(FormField Field, string NewKey)>();
            foreach (var field in module.Fields)
            {
                if (!expectedByOrder.TryGetValue(field.DisplayOrder, out var expected))
                {
                    continue;
                }

                if (field.FieldKey != expected.FieldKey)
                {
                    keyRenames.Add((field, expected.FieldKey));
                    changed = true;
                }

                if (field.HelpText != expected.HelpText)
                {
                    field.UpdateHelpText(expected.HelpText);
                    changed = true;
                }
            }

            // Legacy rows can share a DisplayOrder, which maps two different fields onto the same
            // target key here, or maps one field onto a key a different (already-correct, untouched)
            // field still holds. Either way, applying the rename would violate the unique index no
            // matter how it's staged, so leave those fields alone rather than crash the whole seeder.
            var renamingFieldIds = keyRenames.Select(r => r.Field.Id).ToHashSet();
            var keysHeldByOtherFields = module.Fields
                .Where(f => !renamingFieldIds.Contains(f.Id))
                .Select(f => f.FieldKey)
                .ToHashSet();

            var conflictingKeys = keyRenames
                .GroupBy(r => r.NewKey)
                .Where(g => g.Count() > 1 || keysHeldByOtherFields.Contains(g.Key))
                .Select(g => g.Key)
                .ToHashSet();
            keyRenames = keyRenames.Where(r => !conflictingKeys.Contains(r.NewKey)).ToList();

            if (keyRenames.Count > 0)
            {
                // Two fields can swap keys between each other. The unique index on (form_module_id,
                // field_key) is checked per-statement rather than deferred to commit, so writing the
                // target keys directly can collide with a row that hasn't been renamed yet — stage
                // through a throwaway key first so no intermediate state can violate it.
                foreach (var (field, _) in keyRenames)
                {
                    field.UpdateFieldKey($"__tmp_{field.Id}");
                }

                await context.SaveChangesAsync(cancellationToken);

                foreach (var (field, newKey) in keyRenames)
                {
                    field.UpdateFieldKey(newKey);
                }
            }

            if (changed)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }

        // Correct the type of MI modules — an earlier migration defaulted all modules to delivery-plan.
        var miCodes = new HashSet<string>
        {
            DataCollectionFormModuleCodes.HealthyBabies,
            DataCollectionFormModuleCodes.ServiceUsers,
            DataCollectionFormModuleCodes.WiderServiceUsers,
            DataCollectionFormModuleCodes.OutcomeScores,
            DataCollectionFormModuleCodes.BreastfeedingRates,
        };

        var typeCorrectionNeeded = false;
        foreach (var module in existingModules)
        {
            if (miCodes.Contains(module.Code) && module.Type != DataCollectionType.ManagementInformation)
            {
                module.UpdateType(DataCollectionType.ManagementInformation);
                typeCorrectionNeeded = true;
            }
        }

        if (typeCorrectionNeeded)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        // Ensure sections 2–8 of the delivery plan have AllowsMultipleActivities = true.
        var activityFlagNeeded = false;
        foreach (var module in existingModules)
        {
            if (multiActivityCodes.Contains(module.Code) && !module.AllowsMultipleActivities)
            {
                module.SetAllowsMultipleActivities(true);
                activityFlagNeeded = true;
            }
        }

        if (activityFlagNeeded)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    // ─── Focus 2026/2027 ─────────────────────────────────────────────────────

    private static DataCollectionFormModule CreateFocusModule(UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.Focus,
            1,
            "Focus 2026/2027",
            "Top priorities and rationale for each funded strand.",
            DateTime.UtcNow,
            type: DataCollectionType.DeliveryPlan);

        PopulateFocusFields(module);

        if (systemUserId != UserId.Empty())
        {
            module.Publish(systemUserId);
        }

        return module;
    }

    private static void PopulateFocusFields(DataCollectionFormModule module)
    {
        var sDg = module.AddSection(1, "Delivery Grant", "Delivery Grant strand priorities.");
        var sP = module.AddSection(2, "Parenting", "Parenting strand priorities.");
        var sH = module.AddSection(3, "HLE", "HLE strand priorities.");
        var sSend = module.AddSection(4, "SEND", "SEND strand priorities.");
        var sPn = module.AddSection(5, "PNMH and PIR", "PNMH & PIR strand priorities (TF2 only).");
        var sIf = module.AddSection(6, "Infant Feeding", "Infant Feeding strand priorities (TF2 only).");
        var sHbo = module.AddSection(7, "Healthy Babies Offer", "Healthy Babies Offer strand priorities (TF2 only).");

        // Priority option set for the TF1 strands (Delivery Grant, Parenting, HLE, SEND) — no "not received
        // funding" option, since these strands are always funded. Shared by F01, F04, F07, F10.
        static void AddTf1PriorityOptions(FormField field)
        {
            field.AddOption("establishing", "Establishing new services and preparing for delivery", 1);
            field.AddOption("expanding_reach", "Expanding the reach of your existing and new services to increase the range of families reached", 2);
            field.AddOption("improving", "Improving and expanding the range of services you are currently delivering in your local authority", 3);
            field.AddOption("strengthen", "Strengthen integration and connection across a wide range of family services to build a cohesive offer for families", 4);
            field.AddOption("sustaining", "Sustaining current levels of programme delivery throughout 26/27", 5);
            field.AddOption("other", "Other", 6);
        }

        // Priority option set for the TF2-only strands (PNMH & PIR, Infant Feeding, Healthy Babies Offer),
        // which additionally allow "not received funding for this strand". Shared by F13, F16, F19.
        static void AddTf2PriorityOptions(FormField field)
        {
            field.AddOption("sustaining", "Sustaining current levels of programme delivery throughout 26/27", 1);
            field.AddOption("improving", "Improving and expanding the range of services you are currently delivering in your local authority", 2);
            field.AddOption("expanding_reach", "Expanding the reach of your existing and new services to increase the range of families reached", 3);
            field.AddOption("establishing", "Establishing new services and preparing for delivery", 4);
            field.AddOption("strengthen", "Strengthen integration and connection across a wide range of family services to build a cohesive offer for families", 5);
            field.AddOption("not_received", "The LA has not received funding for this strand", 6);
            field.AddOption("other", "Other", 7);
        }

        // "in" (rather than notEquals) so the bulk-upload ConditionalRuleEvaluator and the frontend renderer agree.
        const string showUnlessNotReceived = "\"in\":[\"sustaining\",\"improving\",\"expanding_reach\",\"establishing\",\"strengthen\",\"other\"]";

        // ── Delivery Grant ──
        var f01 = module.AddField("F01", "What is the top priority for the Delivery Grant?", FieldType.Select, 1, true);
        f01.SetSection(sDg.Id);
        AddTf1PriorityOptions(f01);

        var f02 = module.AddField("F02", "Describe the top priority for the Delivery Grant.", FieldType.Text, 2, false, null, "Required if 'Other' is selected above.");
        f02.SetSection(sDg.Id);
        f02.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"F01\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var f03 = module.AddField("F03", "You may provide a short rationale for your chosen top priority for the Delivery Grant.", FieldType.Textarea, 3, false);
        f03.SetSection(sDg.Id);

        // ── Parenting ──
        var f04 = module.AddField("F04", "What is the top priority for Parenting?", FieldType.Select, 4, true);
        f04.SetSection(sP.Id);
        AddTf1PriorityOptions(f04);

        var f05 = module.AddField("F05", "Describe the top priority for Parenting.", FieldType.Text, 5, false, null, "Required if 'Other' is selected above.");
        f05.SetSection(sP.Id);
        f05.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"F04\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var f06 = module.AddField("F06", "You may provide a short rationale for your chosen top priority for Parenting.", FieldType.Textarea, 6, false);
        f06.SetSection(sP.Id);

        // ── HLE ──
        var f07 = module.AddField("F07", "What is the top priority for HLE?", FieldType.Select, 7, true);
        f07.SetSection(sH.Id);
        AddTf1PriorityOptions(f07);

        var f08 = module.AddField("F08", "Describe the top priority for HLE.", FieldType.Text, 8, false, null, "Required if 'Other' is selected above.");
        f08.SetSection(sH.Id);
        f08.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"F07\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var f09 = module.AddField("F09", "You may provide a short rationale for your chosen top priority for HLE.", FieldType.Textarea, 9, false);
        f09.SetSection(sH.Id);

        // ── SEND ──
        var f10 = module.AddField("F10", "What is the top priority for SEND?", FieldType.Select, 10, true);
        f10.SetSection(sSend.Id);
        AddTf1PriorityOptions(f10);

        var f11 = module.AddField("F11", "Describe the top priority for SEND.", FieldType.Text, 11, false, null, "Required if 'Other' is selected above.");
        f11.SetSection(sSend.Id);
        f11.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"F10\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var f12 = module.AddField("F12", "You may provide a short rationale for your chosen top priority for SEND.", FieldType.Textarea, 12, false);
        f12.SetSection(sSend.Id);

        // ── PNMH & PIR (TF2 only) ──
        var f13 = module.AddField("F13", "What is the top priority for PNMH & PIR?", FieldType.Select, 13, true);
        f13.SetSection(sPn.Id);
        AddTf2PriorityOptions(f13);

        var f14 = module.AddField("F14", "Describe the top priority for PNMH & PIR.", FieldType.Text, 14, false, null, "Required if 'Other' is selected above.");
        f14.SetSection(sPn.Id);
        f14.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"F13\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var f15 = module.AddField("F15", "You may provide a short rationale for your chosen top priority for PNMH & PIR.", FieldType.Textarea, 15, false, null, "Not required if 'The LA has not received funding for this strand' was selected.");
        f15.SetSection(sPn.Id);
        f15.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"F13\"," + showUnlessNotReceived + "}}");

        // ── Infant Feeding (TF2 only) ──
        var f16 = module.AddField("F16", "What is the top priority for Infant Feeding?", FieldType.Select, 16, true);
        f16.SetSection(sIf.Id);
        AddTf2PriorityOptions(f16);

        var f17 = module.AddField("F17", "Describe the top priority for Infant Feeding.", FieldType.Text, 17, false, null, "Required if 'Other' is selected above.");
        f17.SetSection(sIf.Id);
        f17.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"F16\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var f18 = module.AddField("F18", "You may provide a short rationale for your chosen top priority for Infant Feeding.", FieldType.Textarea, 18, false, null, "Not required if 'The LA has not received funding for this strand' was selected.");
        f18.SetSection(sIf.Id);
        f18.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"F16\"," + showUnlessNotReceived + "}}");

        // ── Healthy Babies Offer (TF2 only) ──
        var f19 = module.AddField("F19", "What is the top priority for the Healthy Babies Offer?", FieldType.Select, 19, true);
        f19.SetSection(sHbo.Id);
        AddTf2PriorityOptions(f19);

        var f20 = module.AddField("F20", "Describe the top priority for Healthy Babies Offer.", FieldType.Text, 20, false, null, "Required if 'Other' is selected above.");
        f20.SetSection(sHbo.Id);
        f20.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"F19\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var f21 = module.AddField("F21", "You may provide a short rationale for your chosen top priority for the Healthy Babies Offer.", FieldType.Textarea, 21, false, null, "Not required if 'The LA has not received funding for this strand' was selected.");
        f21.SetSection(sHbo.Id);
        f21.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"F19\"," + showUnlessNotReceived + "}}");
    }

    // ─── Delivery Grant ───────────────────────────────────────────────────────

    private static DataCollectionFormModule CreateDeliveryGrantModule(UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.DeliveryGrant,
            2,
            "Delivery Grant",
            "Activity tracking and financial reporting for Delivery Grant funded activities.",
            DateTime.UtcNow,
            type: DataCollectionType.DeliveryPlan);

        PopulateDeliveryGrantFields(module);

        if (systemUserId != UserId.Empty())
        {
            module.Publish(systemUserId);
        }

        return module;
    }

    private static void PopulateDeliveryGrantFields(DataCollectionFormModule module)
    {
        var s1 = module.AddSection(1, "Activity Information", "Core activity details.");
        var s2 = module.AddSection(2, "Activity Details", "Theme, status and delivery information.");
        var s3 = module.AddSection(3, "Funding", "Financial forecasts and actuals.");
        var s4 = module.AddSection(4, "Progress and Risks", "Activity changes and risk management.");

        var dgId = module.AddField("DG_ID", "Activity ID", FieldType.ReadOnly, 1, false, null, "Assigned automatically as a unique reference for this activity.");
        dgId.SetSection(s1.Id);

        var dgName = module.AddField("DG_NAME", "What is the activity name?", FieldType.Text, 2, true, null, "Tell us what the activity is called. Activity names must be unique.");
        dgName.SetSection(s1.Id);

        var dgAbout = module.AddField("DG_ABOUT", "What is the activity about?", FieldType.Textarea, 3, false, null, "Briefly describe what you will deliver, do, or achieve as part of this activity.");
        dgAbout.SetSection(s1.Id);

        var dg01 = module.AddField("DG01", "Select the overarching theme or area of focus", FieldType.Select, 4, true);
        dg01.SetSection(s2.Id);
        dg01.AddOption("system_leadership", "System Leadership", 1);
        dg01.AddOption("partnership_working", "Partnership working", 2);
        dg01.AddOption("outreach", "Outreach", 3);
        dg01.AddOption("best_start_sites", "Best Start Family Hub Sites", 4);
        dg01.AddOption("connections", "Connections", 5);
        dg01.AddOption("relationships", "Relationships", 6);
        dg01.AddOption("access_inclusion", "Access and Inclusion", 7);
        dg01.AddOption("staff", "Staff", 8);
        dg01.AddOption("insights", "Insights", 9);
        dg01.AddOption("data_digital", "Data and Digital", 10);
        dg01.AddOption("other", "Other", 11);

        var dg01a = module.AddField("DG01a", "Provide further details for the theme selected", FieldType.Text, 5, false, null, "Required if 'Other' is selected.");
        dg01a.SetSection(s2.Id);
        dg01a.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"DG01\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var dg02 = module.AddField("DG02", "Enter the name and description of all activities delivered within this theme", FieldType.Textarea, 6, true);
        dg02.SetSection(s2.Id);

        var dg03 = module.AddField("DG03", "Is this a new or continuing activity?", FieldType.Radio, 7, true);
        dg03.SetSection(s2.Id);
        dg03.SetConfiguration("{\"size\":\"small\"}");
        dg03.AddOption("new", "New", 1);
        dg03.AddOption("continuing", "Continuing", 2);

        var dg04 = module.AddField("DG04", "Select the activity status", FieldType.Select, 8, true);
        dg04.SetSection(s2.Id);
        dg04.AddOption("started", "Started", 1);
        dg04.AddOption("not_started", "Not started", 2);
        dg04.AddOption("delayed", "Delayed", 3);
        dg04.AddOption("completed", "Completed", 4);

        var dg05 = module.AddField("DG05", "Select the delivery status", FieldType.Select, 9, true);
        dg05.SetSection(s2.Id);
        dg05.AddOption("on_track", "On track", 1);
        dg05.AddOption("off_track", "Off track", 2);

        var dg06 = module.AddField("DG06", "Is this activity funded by other sources than Best Start Family Hubs and Healthy Babies?", FieldType.Radio, 10, true);
        dg06.SetSection(s3.Id);
        dg06.SetConfiguration("{\"size\":\"small\"}");
        dg06.AddOption("yes", "Yes", 1);
        dg06.AddOption("no", "No", 2);

        var dg06a = module.AddField("DG06a", "Total amount of additional funding", FieldType.Currency, 11, false, null, "Required if additional funding sources are present.", "0");
        dg06a.SetSection(s3.Id);
        dg06a.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"DG06\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        dg06a.SetConfiguration("{\"group\":\"DG06_extra\",\"groupLabel\":\"Additional funding details\",\"prefix\":\"£\"}");

        var dg06b = module.AddField("DG06b", "Source of additional funding", FieldType.Text, 12, false, null, "Required if additional funding sources are present.");
        dg06b.SetSection(s3.Id);
        dg06b.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"DG06\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        dg06b.SetConfiguration("{\"group\":\"DG06_extra\"}");

        var dg07 = module.AddField("DG07", "How much Delivery Grant funding are you forecasting to spend on this activity in 2026-2027?", FieldType.Currency, 13, true, null, null, "0");
        dg07.SetSection(s3.Id);
        dg07.SetConfiguration("{\"prefix\":\"£\"}");

        var dg08 = module.AddField("DG08", "Total Activity Budget (26/27)", FieldType.Calculation, 14, false, null, "DG06a + DG07");
        dg08.SetSection(s3.Id);
        dg08.SetConfiguration("{\"formula\":\"DG06a + DG07\",\"prefix\":\"£\"}");

        var dg09 = module.AddField("DG09", "What is the total amount of actual spend on this activity to date?", FieldType.Currency, 15, true, null, null, "0");
        dg09.SetSection(s3.Id);
        dg09.SetConfiguration("{\"prefix\":\"£\"}");

        var dg10 = module.AddField("DG10", "What is the amount of funding committed to this activity?", FieldType.Currency, 16, false, null, null, "0");
        dg10.SetSection(s3.Id);
        dg10.SetConfiguration("{\"prefix\":\"£\"}");

        var dg11 = module.AddField("DG11", "Planned Spend (Uncommitted)", FieldType.Calculation, 17, false, null, "DG07 - (DG09 + DG10)");
        dg11.SetSection(s3.Id);
        dg11.SetConfiguration("{\"formula\":\"DG07 - (DG09 + DG10)\",\"prefix\":\"£\"}");

        var dg12 = module.AddField("DG12", "How much funding are you forecasting to spend on this activity in 2027-2028?", FieldType.Currency, 18, true, null, null, "0");
        dg12.SetSection(s3.Id);
        dg12.SetConfiguration("{\"prefix\":\"£\"}");

        var dg13 = module.AddField("DG13", "How much funding are you forecasting to spend on this activity in 2028-2029?", FieldType.Currency, 19, true, null, null, "0");
        dg13.SetSection(s3.Id);
        dg13.SetConfiguration("{\"prefix\":\"£\"}");

        var dg14 = module.AddField("DG14", "Briefly outline what activity changed, why and when.", FieldType.Textarea, 20, false);
        dg14.SetSection(s4.Id);

        var dg15 = module.AddField("DG15", "Are there any risks associated with this activity?", FieldType.Radio, 21, true);
        dg15.SetSection(s4.Id);
        dg15.SetConfiguration("{\"size\":\"small\"}");
        dg15.AddOption("yes", "Yes", 1);
        dg15.AddOption("no", "No", 2);

        var dg15a = module.AddField("DG15a", "Outline the identified risks", FieldType.Textarea, 22, true, null, "Required if risks are present.");
        dg15a.SetSection(s4.Id);
        dg15a.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"DG15\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        dg15a.SetConfiguration("{\"group\":\"DG15_risk\",\"groupLabel\":\"Risk details\"}");

        var dg15b = module.AddField("DG15b", "Select the likelihood of this risk", FieldType.Select, 23, true, null, "Required if risks are present.");
        dg15b.SetSection(s4.Id);
        dg15b.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"DG15\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        dg15b.SetConfiguration("{\"group\":\"DG15_risk\"}");
        dg15b.AddOption("low", "Low", 1);
        dg15b.AddOption("medium", "Medium", 2);
        dg15b.AddOption("high", "High", 3);

        var dg15c = module.AddField("DG15c", "Select the impact of this risk", FieldType.Select, 24, true, null, "Required if risks are present.");
        dg15c.SetSection(s4.Id);
        dg15c.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"DG15\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        dg15c.SetConfiguration("{\"group\":\"DG15_risk\"}");
        dg15c.AddOption("low", "Low", 1);
        dg15c.AddOption("medium", "Medium", 2);
        dg15c.AddOption("high", "High", 3);

        var dg15d = module.AddField("DG15d", "Give details of the mitigations in place or planned", FieldType.Textarea, 25, true, null, "Required if risks are present.");
        dg15d.SetSection(s4.Id);
        dg15d.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"DG15\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        dg15d.SetConfiguration("{\"group\":\"DG15_risk\"}");
    }

    // ─── Parenting ────────────────────────────────────────────────────────────

    private static DataCollectionFormModule CreateParentingModule(UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.Parenting,
            3,
            "Parenting",
            "Activity tracking and financial reporting for Parenting funded activities.",
            DateTime.UtcNow,
            type: DataCollectionType.DeliveryPlan);

        PopulateParentingFields(module);

        if (systemUserId != UserId.Empty())
        {
            module.Publish(systemUserId);
        }

        return module;
    }

    private static void PopulateParentingFields(DataCollectionFormModule module)
    {
        var s1 = module.AddSection(1, "Activity Information", "Core activity details.");
        var s2 = module.AddSection(2, "Activity Details", "Theme, expectations and delivery information.");
        var s3 = module.AddSection(3, "Funding", "Financial forecasts and actuals.");
        var s4 = module.AddSection(4, "Progress and Risks", "Activity changes and risk management.");

        var pId = module.AddField("P_ID", "Activity ID", FieldType.ReadOnly, 1, false, null, "Assigned automatically as a unique reference for this activity.");
        pId.SetSection(s1.Id);

        var pName = module.AddField("P_NAME", "What is the activity name?", FieldType.Text, 2, true, null, "Tell us what the activity is called. Activity names must be unique.");
        pName.SetSection(s1.Id);

        var pAbout = module.AddField("P_ABOUT", "What is the activity about?", FieldType.Textarea, 3, false, null, "Briefly describe what you will deliver, do, or achieve as part of this activity.");
        pAbout.SetSection(s1.Id);

        var p01 = module.AddField("P01", "Select the overarching theme or area of focus", FieldType.Select, 4, true);
        p01.SetSection(s2.Id);
        p01.AddOption("ebi1", "EBI1", 1);
        p01.AddOption("ebi2", "EBI2", 2);
        p01.AddOption("ebi3", "EBI3", 3);
        p01.AddOption("ebi_extra", "EBI Extra", 4);
        p01.AddOption("alternative", "Alternative / Locally developed Programme", 5);
        p01.AddOption("peer_support", "Peer Support", 6);
        p01.AddOption("outreach", "Outreach", 7);
        p01.AddOption("partnership", "Partnership working", 8);
        p01.AddOption("staff", "Staff", 9);
        p01.AddOption("other", "Other", 10);

        var p01a = module.AddField("P01a", "Describe the theme or area of focus", FieldType.Text, 5, false, null, "Required if 'Other' is selected.");
        p01a.SetSection(s2.Id);
        p01a.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"P01\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var p02 = module.AddField("P02", "Enter the name and description of all activities delivered within this theme", FieldType.Textarea, 6, true);
        p02.SetSection(s2.Id);

        var p03 = module.AddField("P03", "Which expectations do these activities meet?", FieldType.Checkbox, 7, true, null, "Select all that apply.");
        p03.SetSection(s2.Id);
        p03.AddOption("deliver_offer", "Deliver a parenting offer 0-5", 1);
        p03.AddOption("play_reading", "Play and shared reading resources", 2);
        p03.AddOption("prioritise_ebis", "Prioritise DfE parenting menus/EBIs", 3);
        p03.AddOption("maximise_takeup", "Maximise take up", 4);
        p03.AddOption("staff_equipped", "Staff equipped to deliver", 5);

        var p04 = module.AddField("P04", "Is this a new or continuing activity?", FieldType.Radio, 8, true);
        p04.SetSection(s2.Id);
        p04.SetConfiguration("{\"size\":\"small\"}");
        p04.AddOption("new", "New", 1);
        p04.AddOption("continuing", "Continuing", 2);

        var p05 = module.AddField("P05", "Select the activity status", FieldType.Select, 9, true);
        p05.SetSection(s2.Id);
        p05.AddOption("started", "Started", 1);
        p05.AddOption("not_started", "Not started", 2);
        p05.AddOption("delayed", "Delayed", 3);
        p05.AddOption("completed", "Completed", 4);

        var p06 = module.AddField("P06", "Select the delivery status of this activity", FieldType.Select, 10, true);
        p06.SetSection(s2.Id);
        p06.AddOption("on_track", "On track", 1);
        p06.AddOption("off_track", "Off track", 2);

        var p07 = module.AddField("P07", "Is this activity receiving any additional funding from other sources?", FieldType.Radio, 11, true);
        p07.SetSection(s3.Id);
        p07.SetConfiguration("{\"size\":\"small\"}");
        p07.AddOption("yes", "Yes", 1);
        p07.AddOption("no", "No", 2);

        var p07a = module.AddField("P07a", "Total amount of additional funding", FieldType.Currency, 12, true, null, "Required if additional funding sources are present.", "0");
        p07a.SetSection(s3.Id);
        p07a.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"P07\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        p07a.SetConfiguration("{\"group\":\"P07_extra\",\"groupLabel\":\"Additional funding details\",\"prefix\":\"£\"}");

        var p07b = module.AddField("P07b", "Source of additional funding", FieldType.Text, 13, true, null, "Required if additional funding sources are present.");
        p07b.SetSection(s3.Id);
        p07b.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"P07\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        p07b.SetConfiguration("{\"group\":\"P07_extra\"}");

        var p08 = module.AddField("P08", "How much Parenting funding are you forecasting to spend in 2026-27?", FieldType.Currency, 14, true, null, "QA: Sum of all P08 must be +/- 10% of CFD04.", "0");
        p08.SetSection(s3.Id);
        p08.SetConfiguration("{\"prefix\":\"£\"}");

        var p09 = module.AddField("P09", "Total Activity Budget (26/27)", FieldType.Calculation, 15, false, null, "P07a + P08");
        p09.SetSection(s3.Id);
        p09.SetConfiguration("{\"formula\":\"P07a + P08\",\"prefix\":\"£\"}");

        var p10 = module.AddField("P10", "What is the total amount of actual spend on this activity to date?", FieldType.Currency, 16, true, null, null, "0");
        p10.SetSection(s3.Id);
        p10.SetConfiguration("{\"prefix\":\"£\",\"maxSumField\":\"P09\"}");

        var p11 = module.AddField("P11", "What is the amount of funding committed to this activity?", FieldType.Currency, 17, false, null, null, "0");
        p11.SetSection(s3.Id);
        p11.SetConfiguration("{\"prefix\":\"£\"}");

        var p12 = module.AddField("P12", "Planned Spend (Uncommitted)", FieldType.Calculation, 18, false, null, "P08 - (P10 + P11). Flags overspend if negative.");
        p12.SetSection(s3.Id);
        p12.SetConfiguration("{\"formula\":\"P08 - (P10 + P11)\",\"prefix\":\"£\",\"warnIfNegative\":true}");

        var p13 = module.AddField("P13", "How much funding are you forecasting to spend in 2027-2028?", FieldType.Currency, 19, true, null, "QA: Sum of all P13 must be +/- 10% of CFD05.", "0");
        p13.SetSection(s3.Id);
        p13.SetConfiguration("{\"prefix\":\"£\"}");

        var p14 = module.AddField("P14", "How much funding are you forecasting to spend in 2028-2029?", FieldType.Currency, 20, true, null, "QA: Sum of all P14 must be +/- 10% of CFD06.", "0");
        p14.SetSection(s3.Id);
        p14.SetConfiguration("{\"prefix\":\"£\"}");

        var p15 = module.AddField("P15", "Briefly outline what activity changed, why and when.", FieldType.Textarea, 21, false);
        p15.SetSection(s4.Id);

        var p16 = module.AddField("P16", "Are there any risks associated with this activity?", FieldType.Radio, 22, true);
        p16.SetSection(s4.Id);
        p16.SetConfiguration("{\"size\":\"small\"}");
        p16.AddOption("yes", "Yes", 1);
        p16.AddOption("no", "No", 2);

        var p16a = module.AddField("P16a", "Outline the identified risks", FieldType.Textarea, 23, false, null, "Required if risks are present.");
        p16a.SetSection(s4.Id);
        p16a.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"P16\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        p16a.SetConfiguration("{\"group\":\"P16_risk\",\"groupLabel\":\"Risk details\"}");

        var p16b = module.AddField("P16b", "Select the likelihood of this risk", FieldType.Select, 24, false, null, "Required if risks are present.");
        p16b.SetSection(s4.Id);
        p16b.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"P16\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        p16b.SetConfiguration("{\"group\":\"P16_risk\"}");
        p16b.AddOption("low", "Low", 1);
        p16b.AddOption("medium", "Medium", 2);
        p16b.AddOption("high", "High", 3);

        var p16c = module.AddField("P16c", "Select the impact of this risk", FieldType.Select, 25, false, null, "Required if risks are present.");
        p16c.SetSection(s4.Id);
        p16c.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"P16\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        p16c.SetConfiguration("{\"group\":\"P16_risk\"}");
        p16c.AddOption("low", "Low", 1);
        p16c.AddOption("medium", "Medium", 2);
        p16c.AddOption("high", "High", 3);

        var p16d = module.AddField("P16d", "Give details of the mitigations in place or planned", FieldType.Textarea, 26, false, null, "Required if risks are present.");
        p16d.SetSection(s4.Id);
        p16d.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"P16\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        p16d.SetConfiguration("{\"group\":\"P16_risk\"}");
    }

    // ─── HLE ─────────────────────────────────────────────────────────────────

    private static DataCollectionFormModule CreateHleModule(UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.Hle,
            4,
            "HLE",
            "Activity tracking and financial reporting for HLE funded activities.",
            DateTime.UtcNow,
            type: DataCollectionType.DeliveryPlan);

        PopulateHleFields(module);

        if (systemUserId != UserId.Empty())
        {
            module.Publish(systemUserId);
        }

        return module;
    }

    private static void PopulateHleFields(DataCollectionFormModule module)
    {
        var s1 = module.AddSection(1, "Activity Information", "Core activity details.");
        var s2 = module.AddSection(2, "Activity Details", "Theme, status and delivery information.");
        var s3 = module.AddSection(3, "Funding", "Financial forecasts and actuals.");
        var s4 = module.AddSection(4, "Progress and Risks", "Activity changes and risk management.");

        var hId = module.AddField("H_ID", "Activity ID", FieldType.ReadOnly, 1, false, null, "Assigned automatically as a unique reference for this activity.");
        hId.SetSection(s1.Id);

        var hName = module.AddField("H_NAME", "What is the activity name?", FieldType.Text, 2, true, null, "Tell us what the activity is called. Activity names must be unique.");
        hName.SetSection(s1.Id);

        var hAbout = module.AddField("H_ABOUT", "What is the activity about?", FieldType.Textarea, 3, false, null, "Briefly describe what you will deliver, do, or achieve as part of this activity.");
        hAbout.SetSection(s1.Id);

        var h01 = module.AddField("H01", "Select the overarching theme or area of focus", FieldType.Select, 4, true);
        h01.SetSection(s2.Id);
        h01.AddOption("ebi1", "EBI1", 1);
        h01.AddOption("ebi2", "EBI2", 2);
        h01.AddOption("ebi3", "EBI3", 3);
        h01.AddOption("ebi_extra", "EBI Extra", 4);
        h01.AddOption("alternative", "Alternative / Locally developed Programme", 5);
        h01.AddOption("peer_support", "Peer Support", 6);
        h01.AddOption("outreach", "Outreach", 7);
        h01.AddOption("partnership", "Partnership working", 8);
        h01.AddOption("staff", "Staff", 9);
        h01.AddOption("other", "Other", 10);

        var h01a = module.AddField("H01a", "Describe the theme or area of focus", FieldType.Text, 5, false, null, "Required if 'Other' is selected.");
        h01a.SetSection(s2.Id);
        h01a.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"H01\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var h02 = module.AddField("H02", "Enter the name and description of all activities delivered within this theme", FieldType.Textarea, 6, true);
        h02.SetSection(s2.Id);

        var h03 = module.AddField("H03", "Which expectations do these activities meet?", FieldType.Checkbox, 7, true, null, "Select all that apply.");
        h03.SetSection(s2.Id);
        h03.AddOption("deliver_hle", "Deliver a HLE offer 0-5", 1);
        h03.AddOption("play_reading", "Play and shared reading resources", 2);
        h03.AddOption("prioritise_ebis", "Prioritise EBIs from DfE HLE menus", 3);
        h03.AddOption("maximise_takeup", "Maximise take up of HLE", 4);
        h03.AddOption("staff_equipped", "Staff equipped to deliver", 5);

        var h04 = module.AddField("H04", "Is this a new or continuing activity?", FieldType.Radio, 8, true);
        h04.SetSection(s2.Id);
        h04.SetConfiguration("{\"size\":\"small\"}");
        h04.AddOption("new", "New", 1);
        h04.AddOption("continuing", "Continuing", 2);

        var h05 = module.AddField("H05", "Select the activity status", FieldType.Select, 9, true);
        h05.SetSection(s2.Id);
        h05.AddOption("started", "Started", 1);
        h05.AddOption("not_started", "Not started", 2);
        h05.AddOption("delayed", "Delayed", 3);
        h05.AddOption("completed", "Completed", 4);

        var h06 = module.AddField("H06", "Select the delivery status of this activity", FieldType.Select, 10, true);
        h06.SetSection(s2.Id);
        h06.AddOption("on_track", "On track", 1);
        h06.AddOption("off_track", "Off track", 2);

        var h07 = module.AddField("H07", "Is this activity receiving any additional funding from other sources?", FieldType.Radio, 11, true);
        h07.SetSection(s3.Id);
        h07.SetConfiguration("{\"size\":\"small\"}");
        h07.AddOption("yes", "Yes", 1);
        h07.AddOption("no", "No", 2);

        var h07a = module.AddField("H07a", "Total amount of additional funding", FieldType.Currency, 12, true, null, "Required if additional funding sources are present.", "0");
        h07a.SetSection(s3.Id);
        h07a.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"H07\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        h07a.SetConfiguration("{\"group\":\"H07_extra\",\"groupLabel\":\"Additional funding details\",\"prefix\":\"£\"}");

        var h07b = module.AddField("H07b", "Source of additional funding", FieldType.Text, 13, true, null, "Required if additional funding sources are present.");
        h07b.SetSection(s3.Id);
        h07b.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"H07\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        h07b.SetConfiguration("{\"group\":\"H07_extra\"}");

        var h08 = module.AddField("H08", "How much HLE funding are you forecasting to spend in 2026-27?", FieldType.Currency, 14, true, null, "QA: Sum of all H08 must be +/- 10% of CFD07.", "0");
        h08.SetSection(s3.Id);
        h08.SetConfiguration("{\"prefix\":\"£\"}");

        var h09 = module.AddField("H09", "Total Activity Budget (26/27)", FieldType.Calculation, 15, false, null, "H07a + H08");
        h09.SetSection(s3.Id);
        h09.SetConfiguration("{\"formula\":\"H07a + H08\",\"prefix\":\"£\"}");

        var h10 = module.AddField("H10", "What is the total amount of actual spend on this activity to date?", FieldType.Currency, 16, true, null, null, "0");
        h10.SetSection(s3.Id);
        h10.SetConfiguration("{\"prefix\":\"£\"}");

        var h11 = module.AddField("H11", "What is the amount of funding committed to this activity?", FieldType.Currency, 17, false, null, null, "0");
        h11.SetSection(s3.Id);
        h11.SetConfiguration("{\"prefix\":\"£\"}");

        var h12 = module.AddField("H12", "Planned Spend (Uncommitted)", FieldType.Calculation, 18, false, null, "H08 - (H10 + H11). Flags overspend if negative.");
        h12.SetSection(s3.Id);
        h12.SetConfiguration("{\"formula\":\"H08 - (H10 + H11)\",\"prefix\":\"£\"}");

        var h13 = module.AddField("H13", "How much funding are you forecasting to spend in 2027-2028?", FieldType.Currency, 19, true, null, "QA: Sum of all H13 must be +/- 10% of CFD08.", "0");
        h13.SetSection(s3.Id);
        h13.SetConfiguration("{\"prefix\":\"£\"}");

        var h14 = module.AddField("H14", "How much funding are you forecasting to spend in 2028-2029?", FieldType.Currency, 20, true, null, "QA: Sum of all H14 must be +/- 10% of CFD09.", "0");
        h14.SetSection(s3.Id);
        h14.SetConfiguration("{\"prefix\":\"£\"}");

        var h15 = module.AddField("H15", "Briefly outline what activity changed, why and when.", FieldType.Textarea, 21, false);
        h15.SetSection(s4.Id);

        var h16 = module.AddField("H16", "Are there any risks associated with this activity?", FieldType.Radio, 22, true);
        h16.SetSection(s4.Id);
        h16.SetConfiguration("{\"size\":\"small\"}");
        h16.AddOption("yes", "Yes", 1);
        h16.AddOption("no", "No", 2);

        var h16a = module.AddField("H16a", "Outline the identified risks", FieldType.Textarea, 23, true, null, "Required if risks are present.");
        h16a.SetSection(s4.Id);
        h16a.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"H16\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        h16a.SetConfiguration("{\"group\":\"H16_risk\",\"groupLabel\":\"Risk details\"}");

        var h16b = module.AddField("H16b", "Select the likelihood of this risk", FieldType.Select, 24, true, null, "Required if risks are present.");
        h16b.SetSection(s4.Id);
        h16b.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"H16\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        h16b.SetConfiguration("{\"group\":\"H16_risk\"}");
        h16b.AddOption("low", "Low", 1);
        h16b.AddOption("medium", "Medium", 2);
        h16b.AddOption("high", "High", 3);

        var h16c = module.AddField("H16c", "Select the impact of this risk", FieldType.Select, 25, true, null, "Required if risks are present.");
        h16c.SetSection(s4.Id);
        h16c.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"H16\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        h16c.SetConfiguration("{\"group\":\"H16_risk\"}");
        h16c.AddOption("low", "Low", 1);
        h16c.AddOption("medium", "Medium", 2);
        h16c.AddOption("high", "High", 3);

        var h16d = module.AddField("H16d", "Give details of the mitigations in place or planned", FieldType.Textarea, 26, true, null, "Required if risks are present.");
        h16d.SetSection(s4.Id);
        h16d.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"H16\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        h16d.SetConfiguration("{\"group\":\"H16_risk\"}");
    }

    // ─── SEND ─────────────────────────────────────────────────────────────────

    private static DataCollectionFormModule CreateSendModule(UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.Send,
            5,
            "SEND",
            "Activity tracking and financial reporting for SEND funded activities.",
            DateTime.UtcNow,
            type: DataCollectionType.DeliveryPlan);

        PopulateSendFields(module);

        if (systemUserId != UserId.Empty())
        {
            module.Publish(systemUserId);
        }

        return module;
    }

    private static void PopulateSendFields(DataCollectionFormModule module)
    {
        var s1 = module.AddSection(1, "Activity Information", "Core activity details.");
        var s2 = module.AddSection(2, "Activity Details", "Theme, status and delivery information.");
        var s3 = module.AddSection(3, "Funding", "Financial forecasts and actuals.");
        var s4 = module.AddSection(4, "Progress and Risks", "Activity changes and risk management.");

        var sendId = module.AddField("SEND_ID", "Activity ID", FieldType.ReadOnly, 1, false, null, "Assigned automatically as a unique reference for this activity.");
        sendId.SetSection(s1.Id);

        var sendName = module.AddField("SEND_NAME", "What is the activity name?", FieldType.Text, 2, true, null, "Tell us what the activity is called. Activity names must be unique.");
        sendName.SetSection(s1.Id);

        var sendAbout = module.AddField("SEND_ABOUT", "What is the activity about?", FieldType.Textarea, 3, false, null, "Briefly describe what you will deliver, do, or achieve as part of this activity.");
        sendAbout.SetSection(s1.Id);

        var send01 = module.AddField("SEND01", "Select the overarching theme or area of focus", FieldType.Select, 4, true);
        send01.SetSection(s2.Id);
        send01.AddOption("appoint_bsips", "Appoint BSIPS", 1);
        send01.AddOption("ebi1", "EBI 1 (prescribed list)", 2);
        send01.AddOption("ebi_extra", "Extra EBI (prescribed list)", 3);
        send01.AddOption("vcf_local", "VCF/locally developed interventions", 4);
        send01.AddOption("other", "Other", 5);

        var send01a = module.AddField("SEND01a", "Provide further details for the theme selected", FieldType.Text, 5, false, null, "Required if 'Other' is selected.");
        send01a.SetSection(s2.Id);
        send01a.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SEND01\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var send02 = module.AddField("SEND02", "Enter the name and description of all activities delivered within this funded strand", FieldType.Textarea, 6, true);
        send02.SetSection(s2.Id);

        var send03 = module.AddField("SEND03", "Which expectations do these activities meet?", FieldType.Checkbox, 7, true, null, "Select all that apply.");
        send03.SetSection(s2.Id);
        send03.AddOption("appoint_bsips", "Appoint BSIPs", 1);
        send03.AddOption("build_workforce", "Build workforce capability", 2);
        send03.AddOption("identify_need", "Identify emerging additional need", 3);
        send03.AddOption("prepare_ebi", "Prepare EBI/child interventions", 4);
        send03.AddOption("strengthen_partnerships", "Strengthen SEND partnerships", 5);
        send03.AddOption("strategic_role", "Strategic/system role", 6);

        var send04 = module.AddField("SEND04", "Is this a new or continuing activity?", FieldType.Radio, 8, true);
        send04.SetSection(s2.Id);
        send04.SetConfiguration("{\"size\":\"small\"}");
        send04.AddOption("new", "New", 1);
        send04.AddOption("continuing", "Continuing", 2);

        var send05 = module.AddField("SEND05", "Select the activity status", FieldType.Select, 9, true);
        send05.SetSection(s2.Id);
        send05.AddOption("started", "Started", 1);
        send05.AddOption("not_started", "Not started", 2);
        send05.AddOption("delayed", "Delayed", 3);
        send05.AddOption("completed", "Completed", 4);

        var send06 = module.AddField("SEND06", "Select the delivery status of this activity", FieldType.Select, 10, true);
        send06.SetSection(s2.Id);
        send06.AddOption("on_track", "On track", 1);
        send06.AddOption("off_track", "Off track", 2);

        var send07 = module.AddField("SEND07", "Is this activity receiving any additional funding from other sources?", FieldType.Radio, 11, true);
        send07.SetSection(s3.Id);
        send07.SetConfiguration("{\"size\":\"small\"}");
        send07.AddOption("yes", "Yes", 1);
        send07.AddOption("no", "No", 2);

        var send07a = module.AddField("SEND07a", "Total amount of additional funding", FieldType.Currency, 12, true, null, "Required if additional funding sources are present.", "0");
        send07a.SetSection(s3.Id);
        send07a.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SEND07\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        send07a.SetConfiguration("{\"group\":\"SEND07_extra\",\"groupLabel\":\"Additional funding details\",\"prefix\":\"£\"}");

        var send07b = module.AddField("SEND07b", "Source of additional funding", FieldType.Text, 13, true, null, "Required if additional funding sources are present.");
        send07b.SetSection(s3.Id);
        send07b.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SEND07\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        send07b.SetConfiguration("{\"group\":\"SEND07_extra\"}");

        var send08 = module.AddField("SEND08", "How much SEND funding are you forecasting to spend in 2026-27?", FieldType.Currency, 14, true, null, "QA: Sum of all SEND08 must be +/- 10% of CFD10.", "0");
        send08.SetSection(s3.Id);
        send08.SetConfiguration("{\"prefix\":\"£\"}");

        var send09 = module.AddField("SEND09", "Total Activity Budget (26/27)", FieldType.Calculation, 15, false, null, "SEND07a + SEND08");
        send09.SetSection(s3.Id);
        send09.SetConfiguration("{\"formula\":\"SEND07a + SEND08\",\"prefix\":\"£\"}");

        var send10 = module.AddField("SEND10", "What is the total amount of actual spend on this activity to date?", FieldType.Currency, 16, true, null, null, "0");
        send10.SetSection(s3.Id);
        send10.SetConfiguration("{\"prefix\":\"£\",\"maxSumField\":\"SEND09\"}");

        var send11 = module.AddField("SEND11", "What is the amount of funding committed to this activity?", FieldType.Currency, 17, false, null, null, "0");
        send11.SetSection(s3.Id);
        send11.SetConfiguration("{\"prefix\":\"£\"}");

        var send12 = module.AddField("SEND12", "Planned Spend (Uncommitted)", FieldType.Calculation, 18, false, null, "SEND08 - (SEND10 + SEND11). Flags overspend if negative.");
        send12.SetSection(s3.Id);
        send12.SetConfiguration("{\"formula\":\"SEND08 - (SEND10 + SEND11)\",\"prefix\":\"£\",\"warnIfNegative\":true}");

        var send13 = module.AddField("SEND13", "How much funding are you forecasting to spend in 2027-2028?", FieldType.Currency, 19, true, null, "QA: Sum of all SEND13 must be +/- 10% of CFD11.", "0");
        send13.SetSection(s3.Id);
        send13.SetConfiguration("{\"prefix\":\"£\"}");

        var send14 = module.AddField("SEND14", "How much funding are you forecasting to spend in 2028-2029?", FieldType.Currency, 20, true, null, "QA: Sum of all SEND14 must be +/- 10% of CFD12.", "0");
        send14.SetSection(s3.Id);
        send14.SetConfiguration("{\"prefix\":\"£\"}");

        var send15 = module.AddField("SEND15", "Briefly outline what activity changed, why and when.", FieldType.Textarea, 21, false);
        send15.SetSection(s4.Id);

        var send16 = module.AddField("SEND16", "Are there any risks associated with this activity?", FieldType.Radio, 22, true);
        send16.SetSection(s4.Id);
        send16.SetConfiguration("{\"size\":\"small\"}");
        send16.AddOption("yes", "Yes", 1);
        send16.AddOption("no", "No", 2);

        var send16a = module.AddField("SEND16a", "Outline the identified risks", FieldType.Textarea, 23, false, null, "Required if risks are present.");
        send16a.SetSection(s4.Id);
        send16a.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SEND16\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        send16a.SetConfiguration("{\"group\":\"SEND16_risk\",\"groupLabel\":\"Risk details\"}");

        var send16b = module.AddField("SEND16b", "Select the likelihood of this risk", FieldType.Select, 24, false, null, "Required if risks are present.");
        send16b.SetSection(s4.Id);
        send16b.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SEND16\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        send16b.SetConfiguration("{\"group\":\"SEND16_risk\"}");
        send16b.AddOption("low", "Low", 1);
        send16b.AddOption("medium", "Medium", 2);
        send16b.AddOption("high", "High", 3);

        var send16c = module.AddField("SEND16c", "Select the impact of this risk", FieldType.Select, 25, false, null, "Required if risks are present.");
        send16c.SetSection(s4.Id);
        send16c.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SEND16\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        send16c.SetConfiguration("{\"group\":\"SEND16_risk\"}");
        send16c.AddOption("low", "Low", 1);
        send16c.AddOption("medium", "Medium", 2);
        send16c.AddOption("high", "High", 3);

        var send16d = module.AddField("SEND16d", "Give details of the mitigations in place or planned", FieldType.Textarea, 26, false, null, "Required if risks are present.");
        send16d.SetSection(s4.Id);
        send16d.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SEND16\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        send16d.SetConfiguration("{\"group\":\"SEND16_risk\"}");
    }

    // ─── PNMH and PIR (TF2 only) ─────────────────────────────────────────────

    private static DataCollectionFormModule CreatePnmhAndPirModule(UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.PnmhAndPir,
            6,
            "PNMH and PIR",
            "Activity tracking for Perinatal Mental Health and Parent-Infant Relationships (TF2 LAs only).",
            DateTime.UtcNow,
            type: DataCollectionType.DeliveryPlan);

        PopulatePnmhAndPirFields(module);

        if (systemUserId != UserId.Empty())
        {
            module.Publish(systemUserId);
        }

        return module;
    }

    private static void PopulatePnmhAndPirFields(DataCollectionFormModule module)
    {
        var s1 = module.AddSection(1, "Activity Information", "Core activity details.");
        var s2 = module.AddSection(2, "Activity Details", "Activity type, expectations and status.");
        var s3 = module.AddSection(3, "Dates and Status", "Planned and actual activity dates.");
        var s4 = module.AddSection(4, "Funding", "Financial forecasts and actuals.");
        var s5 = module.AddSection(5, "Progress and Strand Risk", "Activity progress and strand-level risk.");

        var pnId = module.AddField("PN_ID", "Activity ID", FieldType.ReadOnly, 1, false, null, "Assigned automatically as a unique reference for this activity.");
        pnId.SetSection(s1.Id);

        var pnName = module.AddField("PN_NAME", "What is the activity name?", FieldType.Text, 2, true, null, "Tell us what the activity is called. Activity names must be unique.");
        pnName.SetSection(s1.Id);

        var pnAbout = module.AddField("PN_ABOUT", "What is the activity about?", FieldType.Textarea, 3, false, null, "Briefly describe what you will deliver, do, or achieve as part of this activity.");
        pnAbout.SetSection(s1.Id);

        var pn01 = module.AddField("PN01", "Provide the name and a brief description of the activity.", FieldType.Textarea, 4, true);
        pn01.SetSection(s2.Id);

        var pn02 = module.AddField("PN02", "Select the activity type", FieldType.Select, 5, true);
        pn02.SetSection(s2.Id);
        pn02.AddOption("training", "Training", 1);
        pn02.AddOption("service_delivery", "Service Delivery", 2);
        pn02.AddOption("staff", "Staff", 3);
        pn02.AddOption("other", "Other", 4);

        var pn03 = module.AddField("PN03", "Which expectations does this activity meet?", FieldType.Checkbox, 6, true, null, "Multi-select.");
        pn03.SetSection(s2.Id);
        pn03.AddOption("safe_inclusive", "Ensure BSFHs provide safe, inclusive and supportive environments", 1);
        pn03.AddOption("co_locate", "Co-locate PNMH and PIR support", 2);
        pn03.AddOption("build_on_mature", "Build on mature PNMH and PIR systems", 3);
        pn03.AddOption("clear_info", "Provide clear information and advice", 4);
        pn03.AddOption("high_quality", "Provide high quality, evidence-based support", 5);
        pn03.AddOption("shared_vision", "Build a shared system wide vision", 6);
        pn03.AddOption("empower_workforce", "Empower the workforce through training/supervision", 7);

        var pn04 = module.AddField("PN04", "Is this a new or continuing activity?", FieldType.Radio, 7, true);
        pn04.SetSection(s2.Id);
        pn04.SetConfiguration("{\"size\":\"small\"}");
        pn04.AddOption("new", "New", 1);
        pn04.AddOption("continuing", "Continuing", 2);

        var pn05 = module.AddField("PN05", "Provide the planned start date for this activity", FieldType.Text, 8, true, "DD/MM/YYYY");
        pn05.SetSection(s3.Id);

        var pn06 = module.AddField("PN06", "Provide the actual date from which this activity commenced", FieldType.Text, 9, false, "DD/MM/YYYY");
        pn06.SetSection(s3.Id);

        var pn07 = module.AddField("PN07", "Provide the date you expect this activity to be completed", FieldType.Text, 10, true, "DD/MM/YYYY", "Cannot be a date before PN05.");
        pn07.SetSection(s3.Id);

        var pn08 = module.AddField("PN08", "Provide the date this activity was completed", FieldType.Text, 11, false, "DD/MM/YYYY", "Cannot be in the future.");
        pn08.SetSection(s3.Id);

        var pn09 = module.AddField("PN09", "Select the activity status", FieldType.Select, 12, true);
        pn09.SetSection(s3.Id);
        pn09.AddOption("started", "Started", 1);
        pn09.AddOption("not_started", "Not started", 2);
        pn09.AddOption("delayed", "Delayed", 3);
        pn09.AddOption("completed", "Completed", 4);

        var pn10 = module.AddField("PN10", "Select the delivery status of this activity", FieldType.Select, 13, true);
        pn10.SetSection(s3.Id);
        pn10.AddOption("on_track", "On track", 1);
        pn10.AddOption("off_track", "Off track", 2);

        var pn11 = module.AddField("PN11", "Is this activity receiving any additional funding from other sources?", FieldType.Radio, 14, true);
        pn11.SetSection(s4.Id);
        pn11.SetConfiguration("{\"size\":\"small\"}");
        pn11.AddOption("yes", "Yes", 1);
        pn11.AddOption("no", "No", 2);

        var pn11a = module.AddField("PN11a", "Total amount of additional funding", FieldType.Currency, 15, true, null, "Required if additional funding sources are present.", "0");
        pn11a.SetSection(s4.Id);
        pn11a.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PN11\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        pn11a.SetConfiguration("{\"group\":\"PN11_extra\",\"groupLabel\":\"Additional funding details\",\"prefix\":\"£\"}");

        var pn11b = module.AddField("PN11b", "Source of additional funding", FieldType.Text, 16, true, null, "Required if additional funding sources are present.");
        pn11b.SetSection(s4.Id);
        pn11b.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PN11\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        pn11b.SetConfiguration("{\"group\":\"PN11_extra\"}");

        var pn12 = module.AddField("PN12", "How much PNMH & PIR funding are you forecasting to spend in 2026-27?", FieldType.Currency, 17, true, null, "QA: Sum of all PN12 must be +/- 10% of CFD13.", "0");
        pn12.SetSection(s4.Id);
        pn12.SetConfiguration("{\"prefix\":\"£\"}");

        var pn13 = module.AddField("PN13", "Total Activity Budget (26/27)", FieldType.Calculation, 18, false, null, "PN11a + PN12");
        pn13.SetSection(s4.Id);
        pn13.SetConfiguration("{\"formula\":\"PN11a + PN12\",\"prefix\":\"£\"}");

        var pn14 = module.AddField("PN14", "What is the total amount of actual spend on this activity to date?", FieldType.Currency, 19, true, null, null, "0");
        pn14.SetSection(s4.Id);
        pn14.SetConfiguration("{\"prefix\":\"£\"}");

        var pn15 = module.AddField("PN15", "What is the amount of funding committed to this activity?", FieldType.Currency, 20, false, null, null, "0");
        pn15.SetSection(s4.Id);
        pn15.SetConfiguration("{\"prefix\":\"£\"}");

        var pn16 = module.AddField("PN16", "Planned Spend (Uncommitted)", FieldType.Calculation, 21, false, null, "PN12 - (PN14 + PN15). Flags overspend if negative.");
        pn16.SetSection(s4.Id);
        pn16.SetConfiguration("{\"formula\":\"PN12 - (PN14 + PN15)\",\"prefix\":\"£\"}");

        var pn17 = module.AddField("PN17", "How much funding are you forecasting to spend in 2027-2028?", FieldType.Currency, 22, true, null, "QA: Sum of all PN17 must be +/- 10% of CFD14.", "0");
        pn17.SetSection(s4.Id);
        pn17.SetConfiguration("{\"prefix\":\"£\"}");

        var pn18 = module.AddField("PN18", "How much funding are you forecasting to spend in 2028-2029?", FieldType.Currency, 23, true, null, "QA: Sum of all PN18 must be +/- 10% of CFD15.", "0");
        pn18.SetSection(s4.Id);
        pn18.SetConfiguration("{\"prefix\":\"£\"}");

        var pn19 = module.AddField("PN19", "Briefly outline what activity changed, why and when.", FieldType.Textarea, 24, false);
        pn19.SetSection(s5.Id);

        var pn20 = module.AddField("PN20", "Provide a narrative of how this activity is progressing.", FieldType.Textarea, 25, false, null, "Optional. Max 200 words.");
        pn20.SetSection(s5.Id);

        var pn21 = module.AddField("PN21", "What are the risks to successful delivery of this strand?", FieldType.Checkbox, 26, true, null, "Strand-level risk. Only needs to be completed once per strand.");
        pn21.SetSection(s5.Id);
        pn21.AddOption("recruitment_delays", "Recruitment delays", 1);
        pn21.AddOption("procurement_delays", "Delays to procurement or start of contracts", 2);
        pn21.AddOption("s114_financial", "S114 notice or financial challenges", 3);
        pn21.AddOption("spend_delay_rollover", "Previous spend delay or rollover", 4);
        pn21.AddOption("wider_challenges", "Wider challenges/issues within LA", 5);
        pn21.AddOption("other", "Other", 6);
        pn21.AddOption("none_na", "None / NA", 7);
    }

    // ─── Infant Feeding (TF2 only) ────────────────────────────────────────────

    private static DataCollectionFormModule CreateInfantFeedingModule(UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.InfantFeeding,
            7,
            "Infant Feeding",
            "Activity tracking for Infant Feeding funded activities (TF2 LAs only).",
            DateTime.UtcNow,
            type: DataCollectionType.DeliveryPlan);

        PopulateInfantFeedingFields(module);

        if (systemUserId != UserId.Empty())
        {
            module.Publish(systemUserId);
        }

        return module;
    }

    private static void PopulateInfantFeedingFields(DataCollectionFormModule module)
    {
        var s1 = module.AddSection(1, "Activity Information", "Core activity details.");
        var s2 = module.AddSection(2, "Activity Details", "Activity type, expectations and status.");
        var s3 = module.AddSection(3, "Dates and Status", "Planned and actual activity dates.");
        var s4 = module.AddSection(4, "Funding", "Financial forecasts and actuals.");
        var s5 = module.AddSection(5, "Progress and Strand Risk", "Activity progress and strand-level risk.");

        var ifId = module.AddField("IF_ID", "Activity ID", FieldType.ReadOnly, 1, false, null, "Assigned automatically as a unique reference for this activity.");
        ifId.SetSection(s1.Id);

        var ifName = module.AddField("IF_NAME", "What is the activity name?", FieldType.Text, 2, true, null, "Tell us what the activity is called. Activity names must be unique.");
        ifName.SetSection(s1.Id);

        var ifAbout = module.AddField("IF_ABOUT", "What is the activity about?", FieldType.Textarea, 3, false, null, "Briefly describe what you will deliver, do, or achieve as part of this activity.");
        ifAbout.SetSection(s1.Id);

        var if01 = module.AddField("IF01", "Provide the name and a brief description of the activity.", FieldType.Textarea, 4, true);
        if01.SetSection(s2.Id);

        var if02 = module.AddField("IF02", "Select the activity type", FieldType.Select, 5, true);
        if02.SetSection(s2.Id);
        if02.AddOption("training", "Training", 1);
        if02.AddOption("service_delivery", "Service Delivery", 2);
        if02.AddOption("staff", "Staff", 3);
        if02.AddOption("other", "Other", 4);

        var if03 = module.AddField("IF03", "Which expectations does this activity meet?", FieldType.Checkbox, 6, true, null, "Multi-select.");
        if03.SetSection(s2.Id);
        if03.AddOption("national_guidance", "Support aligned with national guidance", 1);
        if03.AddOption("co_locate", "Co-locate support", 2);
        if03.AddOption("build_on_mature", "Build on mature offers", 3);
        if03.AddOption("clear_info", "Clear evidence-based info", 4);
        if03.AddOption("high_quality", "High quality accessible support", 5);
        if03.AddOption("joined_up", "Joined up local system", 6);

        var if04 = module.AddField("IF04", "Is this a new or continuing activity?", FieldType.Radio, 7, true);
        if04.SetSection(s2.Id);
        if04.SetConfiguration("{\"size\":\"small\"}");
        if04.AddOption("new", "New", 1);
        if04.AddOption("continuing", "Continuing", 2);

        var if05 = module.AddField("IF05", "Provide the planned start date for this activity", FieldType.Text, 8, true, "DD/MM/YYYY");
        if05.SetSection(s3.Id);

        var if06 = module.AddField("IF06", "Provide the actual date from which this activity commenced", FieldType.Text, 9, false, "DD/MM/YYYY");
        if06.SetSection(s3.Id);

        var if07 = module.AddField("IF07", "Provide the date you expect this activity to be completed", FieldType.Text, 10, true, "DD/MM/YYYY", "Cannot be a date before IF05.");
        if07.SetSection(s3.Id);

        var if08 = module.AddField("IF08", "Provide the date this activity was completed", FieldType.Text, 11, false, "DD/MM/YYYY", "Cannot be in the future.");
        if08.SetSection(s3.Id);

        var if09 = module.AddField("IF09", "Select the activity status", FieldType.Select, 12, true);
        if09.SetSection(s3.Id);
        if09.AddOption("started", "Started", 1);
        if09.AddOption("not_started", "Not started", 2);
        if09.AddOption("delayed", "Delayed", 3);
        if09.AddOption("completed", "Completed", 4);

        var if10 = module.AddField("IF10", "Select the delivery status of this activity", FieldType.Select, 13, true);
        if10.SetSection(s3.Id);
        if10.AddOption("on_track", "On track", 1);
        if10.AddOption("off_track", "Off track", 2);

        var if11 = module.AddField("IF11", "Is this activity receiving any additional funding from other sources?", FieldType.Radio, 14, true);
        if11.SetSection(s4.Id);
        if11.SetConfiguration("{\"size\":\"small\"}");
        if11.AddOption("yes", "Yes", 1);
        if11.AddOption("no", "No", 2);

        var if11a = module.AddField("IF11a", "Total amount of additional funding", FieldType.Currency, 15, true, null, "Required if additional funding sources are present.", "0");
        if11a.SetSection(s4.Id);
        if11a.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"IF11\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        if11a.SetConfiguration("{\"group\":\"IF11_extra\",\"groupLabel\":\"Additional funding details\",\"prefix\":\"£\"}");

        var if11b = module.AddField("IF11b", "Source of additional funding", FieldType.Text, 16, true, null, "Required if additional funding sources are present.");
        if11b.SetSection(s4.Id);
        if11b.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"IF11\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        if11b.SetConfiguration("{\"group\":\"IF11_extra\"}");

        var if12 = module.AddField("IF12", "How much Infant Feeding funding are you forecasting to spend in 2026-27?", FieldType.Currency, 17, true, null, "QA: Sum of all IF12 must be +/- 10% of CFD16.", "0");
        if12.SetSection(s4.Id);
        if12.SetConfiguration("{\"prefix\":\"£\"}");

        var if13 = module.AddField("IF13", "Total Activity Budget (26/27)", FieldType.Calculation, 18, false, null, "IF11a + IF12");
        if13.SetSection(s4.Id);
        if13.SetConfiguration("{\"formula\":\"IF11a + IF12\",\"prefix\":\"£\"}");

        var if14 = module.AddField("IF14", "What is the total amount of actual spend on this activity to date?", FieldType.Currency, 19, true, null, null, "0");
        if14.SetSection(s4.Id);
        if14.SetConfiguration("{\"prefix\":\"£\",\"maxSumField\":\"IF13\"}");

        var if15 = module.AddField("IF15", "What is the amount of funding committed to this activity?", FieldType.Currency, 20, false, null, null, "0");
        if15.SetSection(s4.Id);
        if15.SetConfiguration("{\"prefix\":\"£\"}");

        var if16 = module.AddField("IF16", "Planned Spend (Uncommitted)", FieldType.Calculation, 21, false, null, "IF12 - (IF14 + IF15). Flags overspend if negative.");
        if16.SetSection(s4.Id);
        if16.SetConfiguration("{\"formula\":\"IF12 - (IF14 + IF15)\",\"prefix\":\"£\",\"warnIfNegative\":true}");

        var if17 = module.AddField("IF17", "How much funding are you forecasting to spend in 2027-2028?", FieldType.Currency, 22, true, null, "QA: Sum of all IF17 must be +/- 10% of CFD17.", "0");
        if17.SetSection(s4.Id);
        if17.SetConfiguration("{\"prefix\":\"£\"}");

        var if18 = module.AddField("IF18", "How much funding are you forecasting to spend in 2028-2029?", FieldType.Currency, 23, true, null, "QA: Sum of all IF18 must be +/- 10% of CFD18.", "0");
        if18.SetSection(s4.Id);
        if18.SetConfiguration("{\"prefix\":\"£\"}");

        var if19 = module.AddField("IF19", "Briefly outline what activity changed, why and when.", FieldType.Textarea, 24, false);
        if19.SetSection(s5.Id);

        var if20 = module.AddField("IF20", "Provide a narrative of how this activity is progressing.", FieldType.Textarea, 25, false, null, "Optional. No PII.");
        if20.SetSection(s5.Id);

        var if21 = module.AddField("IF21", "What are the risks to successful delivery of this strand?", FieldType.Checkbox, 26, true, null, "Strand-level risk. Only needs to be completed once per strand.");
        if21.SetSection(s5.Id);
        if21.AddOption("recruitment_delays", "Recruitment delays", 1);
        if21.AddOption("procurement_delays", "Delays to procurement or start of contracts", 2);
        if21.AddOption("s114_financial", "S114 notice or financial challenges", 3);
        if21.AddOption("spend_delay_rollover", "Previous spend delay or rollover", 4);
        if21.AddOption("wider_challenges", "Wider challenges/issues within LA", 5);
        if21.AddOption("other", "Other", 6);
        if21.AddOption("none_na", "None / NA", 7);

        var if21a = module.AddField("IF21a", "Describe the strand risk in more detail", FieldType.Textarea, 27, false, null, "Required if risks are identified.");
        if21a.SetSection(s5.Id);
        if21a.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"IF21\",\"notEquals\":\"none_na\"}}");

        var if21b = module.AddField("IF21b", "Select the likelihood of this strand risk", FieldType.Select, 28, false, null, "Required if risks are identified.");
        if21b.SetSection(s5.Id);
        if21b.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"IF21\",\"notEquals\":\"none_na\"}}");
        if21b.AddOption("low", "Low", 1);
        if21b.AddOption("medium", "Medium", 2);
        if21b.AddOption("high", "High", 3);

        var if21c = module.AddField("IF21c", "Select the impact of this strand risk", FieldType.Select, 29, false, null, "Required if risks are identified.");
        if21c.SetSection(s5.Id);
        if21c.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"IF21\",\"notEquals\":\"none_na\"}}");
        if21c.AddOption("low", "Low", 1);
        if21c.AddOption("medium", "Medium", 2);
        if21c.AddOption("high", "High", 3);

        var if21d = module.AddField("IF21d", "Describe the mitigation actions in place or planned", FieldType.Textarea, 30, false, null, "Required if risks are identified.");
        if21d.SetSection(s5.Id);
        if21d.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"IF21\",\"notEquals\":\"none_na\"}}");
    }

    // ─── Healthy Babies Offer (TF2 only) ─────────────────────────────────────

    private static DataCollectionFormModule CreateHealthyBabiesOfferModule(UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.HealthyBabiesOffer,
            8,
            "Healthy Babies Offer",
            "Activity tracking for Healthy Babies Offer funded activities (TF2 LAs only).",
            DateTime.UtcNow,
            type: DataCollectionType.DeliveryPlan);

        PopulateHealthyBabiesOfferFields(module);

        if (systemUserId != UserId.Empty())
        {
            module.Publish(systemUserId);
        }

        return module;
    }

    private static void PopulateHealthyBabiesOfferFields(DataCollectionFormModule module)
    {
        var s1 = module.AddSection(1, "Activity Information", "Core activity details.");
        var s2 = module.AddSection(2, "Activity Details", "Activity type, expectations and status.");
        var s3 = module.AddSection(3, "Dates and Status", "Planned and actual activity dates.");
        var s4 = module.AddSection(4, "Funding", "Financial forecasts and actuals.");
        var s5 = module.AddSection(5, "Progress and Strand Risk", "Activity progress and strand-level risk.");

        var hboId = module.AddField("HBO_ID", "Activity ID", FieldType.ReadOnly, 1, false, null, "Assigned automatically as a unique reference for this activity.");
        hboId.SetSection(s1.Id);

        var hboName = module.AddField("HBO_NAME", "What is the activity name?", FieldType.Text, 2, true, null, "Tell us what the activity is called. Activity names must be unique.");
        hboName.SetSection(s1.Id);

        var hboAbout = module.AddField("HBO_ABOUT", "What is the activity about?", FieldType.Textarea, 3, false, null, "Briefly describe what you will deliver, do, or achieve as part of this activity.");
        hboAbout.SetSection(s1.Id);

        var hbo01 = module.AddField("HBO01", "Provide the name and a brief description of the activity.", FieldType.Textarea, 4, true);
        hbo01.SetSection(s2.Id);

        var hbo02 = module.AddField("HBO02", "Select the activity type", FieldType.Select, 5, true);
        hbo02.SetSection(s2.Id);
        hbo02.AddOption("training", "Training", 1);
        hbo02.AddOption("service_delivery", "Service Delivery", 2);
        hbo02.AddOption("staff", "Staff", 3);
        hbo02.AddOption("other", "Other", 4);

        var hbo03 = module.AddField("HBO03", "Which expectations does this activity meet?", FieldType.Checkbox, 6, true, null, "Multi-select.");
        hbo03.SetSection(s2.Id);
        hbo03.AddOption("entitlements", "Families understand entitlements", 1);
        hbo03.AddOption("co_design", "Co-design integrated pathways", 2);
        hbo03.AddOption("involve_parents", "Involve parents/carers in development", 3);
        hbo03.AddOption("accessible_online", "Accessible online space", 4);
        hbo03.AddOption("hard_copy", "Hard-copy version prior to birth", 5);

        var hbo04 = module.AddField("HBO04", "Is this a new or continuing activity?", FieldType.Radio, 7, true);
        hbo04.SetSection(s2.Id);
        hbo04.SetConfiguration("{\"size\":\"small\"}");
        hbo04.AddOption("new", "New", 1);
        hbo04.AddOption("continuing", "Continuing", 2);

        var hbo05 = module.AddField("HBO05", "Provide the planned start date for this activity", FieldType.Text, 8, true, "DD/MM/YYYY");
        hbo05.SetSection(s3.Id);

        var hbo06 = module.AddField("HBO06", "Provide the actual date from which this activity commenced", FieldType.Text, 9, false, "DD/MM/YYYY");
        hbo06.SetSection(s3.Id);

        var hbo07 = module.AddField("HBO07", "Provide the date you expect this activity to be completed", FieldType.Text, 10, true, "DD/MM/YYYY", "Cannot be a date before HBO05.");
        hbo07.SetSection(s3.Id);

        var hbo08 = module.AddField("HBO08", "Provide the date this activity was completed", FieldType.Text, 11, false, "DD/MM/YYYY", "Cannot be in the future.");
        hbo08.SetSection(s3.Id);

        var hbo09 = module.AddField("HBO09", "Select the activity status", FieldType.Select, 12, true);
        hbo09.SetSection(s3.Id);
        hbo09.AddOption("started", "Started", 1);
        hbo09.AddOption("not_started", "Not started", 2);
        hbo09.AddOption("delayed", "Delayed", 3);
        hbo09.AddOption("completed", "Completed", 4);

        var hbo10 = module.AddField("HBO10", "Select the delivery status of this activity", FieldType.Select, 13, true);
        hbo10.SetSection(s3.Id);
        hbo10.AddOption("on_track", "On track", 1);
        hbo10.AddOption("off_track", "Off track", 2);

        var hbo11 = module.AddField("HBO11", "Is this activity receiving any additional funding from other sources?", FieldType.Radio, 14, true);
        hbo11.SetSection(s4.Id);
        hbo11.SetConfiguration("{\"size\":\"small\"}");
        hbo11.AddOption("yes", "Yes", 1);
        hbo11.AddOption("no", "No", 2);

        var hbo11a = module.AddField("HBO11a", "Total amount of additional funding", FieldType.Currency, 15, true, null, "Required if additional funding sources are present.", "0");
        hbo11a.SetSection(s4.Id);
        hbo11a.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"HBO11\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        hbo11a.SetConfiguration("{\"group\":\"HBO11_extra\",\"groupLabel\":\"Additional funding details\",\"prefix\":\"£\"}");

        var hbo11b = module.AddField("HBO11b", "Source of additional funding", FieldType.Text, 16, true, null, "Required if additional funding sources are present.");
        hbo11b.SetSection(s4.Id);
        hbo11b.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"HBO11\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        hbo11b.SetConfiguration("{\"group\":\"HBO11_extra\"}");

        var hbo12 = module.AddField("HBO12", "How much Healthy Babies Offer funding are you forecasting to spend in 2026-27?", FieldType.Currency, 17, true, null, "QA: Sum of all HBO12 must be +/- 10% of CFD19.", "0");
        hbo12.SetSection(s4.Id);
        hbo12.SetConfiguration("{\"prefix\":\"£\"}");

        var hbo13 = module.AddField("HBO13", "Total Activity Budget (26/27)", FieldType.Calculation, 18, false, null, "HBO11a + HBO12");
        hbo13.SetSection(s4.Id);
        hbo13.SetConfiguration("{\"formula\":\"HBO11a + HBO12\",\"prefix\":\"£\"}");

        var hbo14 = module.AddField("HBO14", "What is the total amount of actual spend on this activity to date?", FieldType.Currency, 19, true, null, null, "0");
        hbo14.SetSection(s4.Id);
        hbo14.SetConfiguration("{\"prefix\":\"£\"}");

        var hbo15 = module.AddField("HBO15", "What is the amount of funding committed to this activity?", FieldType.Currency, 20, false, null, null, "0");
        hbo15.SetSection(s4.Id);
        hbo15.SetConfiguration("{\"prefix\":\"£\"}");

        var hbo16 = module.AddField("HBO16", "Planned Spend (Uncommitted)", FieldType.Calculation, 21, false, null, "HBO12 - (HBO14 + HBO15). Flags overspend if negative.");
        hbo16.SetSection(s4.Id);
        hbo16.SetConfiguration("{\"formula\":\"HBO12 - (HBO14 + HBO15)\",\"prefix\":\"£\"}");

        var hbo17 = module.AddField("HBO17", "How much funding are you forecasting to spend in 2027-2028?", FieldType.Currency, 22, true, null, "QA: Sum of all HBO17 must be +/- 10% of CFD20.", "0");
        hbo17.SetSection(s4.Id);
        hbo17.SetConfiguration("{\"prefix\":\"£\"}");

        var hbo18 = module.AddField("HBO18", "How much funding are you forecasting to spend in 2028-2029?", FieldType.Currency, 23, true, null, "QA: Sum of all HBO18 must be +/- 10% of CFD21.", "0");
        hbo18.SetSection(s4.Id);
        hbo18.SetConfiguration("{\"prefix\":\"£\"}");

        var hbo19 = module.AddField("HBO19", "Briefly outline what activity changed, why and when.", FieldType.Textarea, 24, false);
        hbo19.SetSection(s5.Id);

        var hbo20 = module.AddField("HBO20", "Provide a narrative of how this activity is progressing.", FieldType.Textarea, 25, false, null, "Optional. No PII.");
        hbo20.SetSection(s5.Id);

        var hbo21 = module.AddField("HBO21", "What are the risks to successful delivery of this strand?", FieldType.Checkbox, 26, true, null, "Strand-level risk. Only needs to be completed once per strand.");
        hbo21.SetSection(s5.Id);
        hbo21.AddOption("recruitment_delays", "Recruitment delays", 1);
        hbo21.AddOption("procurement_delays", "Delays to procurement or start of contracts", 2);
        hbo21.AddOption("s114_financial", "S114 notice or financial challenges", 3);
        hbo21.AddOption("spend_delay_rollover", "Previous spend delay or rollover", 4);
        hbo21.AddOption("wider_challenges", "Wider challenges/issues within LA", 5);
        hbo21.AddOption("other", "Other", 6);
        hbo21.AddOption("none_na", "None / NA", 7);
    }

    // ─── Rollover Spend ───────────────────────────────────────────────────────

    private static DataCollectionFormModule CreateRolloverSpendModule(UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.RolloverSpend,
            9,
            "Rollover Spend",
            "Tracking of approved rollover spend from previous years.",
            DateTime.UtcNow,
            type: DataCollectionType.DeliveryPlan);

        PopulateRolloverSpendFields(module);

        if (systemUserId != UserId.Empty())
        {
            module.Publish(systemUserId);
        }

        return module;
    }

    private static void PopulateRolloverSpendFields(DataCollectionFormModule module)
    {
        var s1 = module.AddSection(1, "Activity Information", "Core activity details.");
        var s2 = module.AddSection(2, "Financials", "Rollover spend tracking.");

        var ro01 = module.AddField("RO01", "Select the strand this activity refers to", FieldType.Select, 1, true);
        ro01.SetSection(s1.Id);
        ro01.AddOption("delivery_grant", "Delivery Grant", 1);
        ro01.AddOption("parenting", "Parenting", 2);
        ro01.AddOption("hle", "HLE", 3);
        ro01.AddOption("send", "SEND", 4);
        ro01.AddOption("pnmh_pir", "PNMH & PIR", 5);
        ro01.AddOption("infant_feeding", "Infant Feeding", 6);
        ro01.AddOption("hbo", "HBO", 7);

        var ro02 = module.AddField("RO02", "Provide the name and a brief description of the activity", FieldType.Textarea, 2, true);
        ro02.SetSection(s1.Id);

        var ro03 = module.AddField("RO03", "Select the activity type", FieldType.Select, 3, true);
        ro03.SetSection(s1.Id);
        ro03.AddOption("training", "Training", 1);
        ro03.AddOption("service_delivery", "Service Delivery", 2);
        ro03.AddOption("staff", "Staff", 3);
        ro03.AddOption("other", "Other", 4);

        var ro04 = module.AddField("RO04", "Provide the planned start date for this activity", FieldType.Text, 4, true, "DD/MM/YYYY");
        ro04.SetSection(s1.Id);

        var ro05 = module.AddField("RO05", "Provide the actual date from which this activity commenced", FieldType.Text, 5, false, "DD/MM/YYYY");
        ro05.SetSection(s1.Id);

        var ro06 = module.AddField("RO06", "Provide the date you expect this activity to be completed", FieldType.Text, 6, false, "DD/MM/YYYY");
        ro06.SetSection(s1.Id);

        var ro07 = module.AddField("RO07", "Provide the date this activity was completed", FieldType.Text, 7, false, "DD/MM/YYYY");
        ro07.SetSection(s1.Id);

        var ro08 = module.AddField("RO08", "What is the status of this activity?", FieldType.Select, 8, true);
        ro08.SetSection(s1.Id);
        ro08.AddOption("on_track", "On track", 1);
        ro08.AddOption("off_track", "Off track", 2);

        var ro09 = module.AddField("RO09", "Select the year of this rollover", FieldType.Select, 9, true);
        ro09.SetSection(s2.Id);
        ro09.AddOption("2025_26", "2025-26", 1);

        var ro10 = module.AddField("RO10", "Provide the total amount of 2025-26 funding planned for this activity", FieldType.Currency, 10, true, null, null, "0");
        ro10.SetSection(s2.Id);
        ro10.SetConfiguration("{\"prefix\":\"£\"}");

        var ro11 = module.AddField("RO11", "Provide the total amount of actual spend on this activity to date", FieldType.Currency, 11, true, null, null, "0");
        ro11.SetSection(s2.Id);
        ro11.SetConfiguration("{\"prefix\":\"£\",\"maxSumField\":\"RO10\"}");

        var ro12 = module.AddField("RO12", "Provide the amount of funding that is committed to this activity", FieldType.Currency, 12, false, null, null, "0");
        ro12.SetSection(s2.Id);
        ro12.SetConfiguration("{\"prefix\":\"£\"}");

        var ro13 = module.AddField("RO13", "2025-26 Planned Spend", FieldType.Calculation, 13, false, null, "RO10 - (RO11 + RO12). Flags overspend if negative.");
        ro13.SetSection(s2.Id);
        ro13.SetConfiguration("{\"formula\":\"RO10 - (RO11 + RO12)\",\"prefix\":\"£\",\"warnIfNegative\":true}");

        var ro14 = module.AddField("RO14", "Provide a narrative of how this activity is progressing", FieldType.Textarea, 14, false);
        ro14.SetSection(s2.Id);
    }
}