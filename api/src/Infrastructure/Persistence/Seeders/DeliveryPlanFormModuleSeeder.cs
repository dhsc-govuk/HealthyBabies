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
            .Include(m => m.Fields).ThenInclude(f => f.Options)
            .ToListAsync(cancellationToken);

        var modulesAdded = false;

        // Delivery plan modules from the 2026 data map (Focus onwards, sections 1–12).
        // Field codes follow 'Delivery Plan Data Map 3007', last updated 01.07.2026 by Jasmine.
        var newModuleFactories = new (string Code, Func<UserId, DataCollectionFormModule> Factory)[]
        {
            (DataCollectionFormModuleCodes.Focus,                  CreateFocusModule),
            (DataCollectionFormModuleCodes.DeliveryGrant,          CreateDeliveryGrantModule),
            (DataCollectionFormModuleCodes.Parenting,              CreateParentingModule),
            (DataCollectionFormModuleCodes.Hle,                    CreateHleModule),
            (DataCollectionFormModuleCodes.Send,                   CreateSendModule),
            (DataCollectionFormModuleCodes.PnmhAndPir,             CreatePnmhAndPirModule),
            (DataCollectionFormModuleCodes.PnmhAndPirRisk,         CreatePnmhAndPirRiskModule),
            (DataCollectionFormModuleCodes.InfantFeeding,          CreateInfantFeedingModule),
            (DataCollectionFormModuleCodes.InfantFeedingRisk,      CreateInfantFeedingRiskModule),
            (DataCollectionFormModuleCodes.HealthyBabiesOffer,     CreateHealthyBabiesOfferModule),
            (DataCollectionFormModuleCodes.HealthyBabiesOfferRisk, CreateHealthyBabiesOfferRiskModule),
            (DataCollectionFormModuleCodes.RolloverSpend,          CreateRolloverSpendModule),
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

        // Keep delivery-plan module definitions (sections, fields, options) in sync with the seeder on
        // every start, without ever deleting or bulk-recreating existing rows. Fields are matched to
        // the seeder's definition by FieldKey — stable once RepairDeliveryPlanFieldKeyIdentity has run —
        // and updated only where a property differs; a field the seeder no longer defines is removed
        // only if nothing has answered it yet, otherwise it's left alone. A no-op run costs one read
        // per module.
        var populators = new Dictionary<string, Action<DataCollectionFormModule>>
        {
            [DataCollectionFormModuleCodes.Focus] = PopulateFocusFields,
            [DataCollectionFormModuleCodes.DeliveryGrant] = PopulateDeliveryGrantFields,
            [DataCollectionFormModuleCodes.Parenting] = PopulateParentingFields,
            [DataCollectionFormModuleCodes.Hle] = PopulateHleFields,
            [DataCollectionFormModuleCodes.Send] = PopulateSendFields,
            [DataCollectionFormModuleCodes.PnmhAndPir] = PopulatePnmhAndPirFields,
            [DataCollectionFormModuleCodes.PnmhAndPirRisk] = PopulatePnmhAndPirRiskFields,
            [DataCollectionFormModuleCodes.InfantFeeding] = PopulateInfantFeedingFields,
            [DataCollectionFormModuleCodes.InfantFeedingRisk] = PopulateInfantFeedingRiskFields,
            [DataCollectionFormModuleCodes.HealthyBabiesOffer] = PopulateHealthyBabiesOfferFields,
            [DataCollectionFormModuleCodes.HealthyBabiesOfferRisk] = PopulateHealthyBabiesOfferRiskFields,
            [DataCollectionFormModuleCodes.RolloverSpend] = PopulateRolloverSpendFields,
        };

        // Defined here so both the upsert loop and the AllowsMultipleActivities loop below can use it.
        var multiActivityCodes = new HashSet<string>
        {
            DataCollectionFormModuleCodes.DeliveryGrant,
            DataCollectionFormModuleCodes.Parenting,
            DataCollectionFormModuleCodes.Hle,
            DataCollectionFormModuleCodes.Send,
            DataCollectionFormModuleCodes.PnmhAndPir,
            DataCollectionFormModuleCodes.InfantFeeding,
            DataCollectionFormModuleCodes.HealthyBabiesOffer,
            DataCollectionFormModuleCodes.RolloverSpend,
        };

        foreach (var module in existingModules.Where(m => populators.ContainsKey(m.Code)))
        {
            // The real module has Status=Published so AddSection/AddField would throw. Build a shadow
            // module (Status=Draft) with the same ID so any newly-created sections/fields already carry
            // the correct FormModuleId FK and can be inserted directly.
            var shadow = DataCollectionFormModule.Create(module.Id, module.Code, module.SectionNumber, module.Name);
            populators[module.Code](shadow);

            var changed = false;

            var liveSectionsByNumber = module.Sections.ToDictionary(s => s.SectionNumber);
            foreach (var expectedSection in shadow.Sections)
            {
                if (liveSectionsByNumber.TryGetValue(expectedSection.SectionNumber, out var liveSection))
                {
                    if (liveSection.Title != expectedSection.Title || liveSection.Description != expectedSection.Description
                        || liveSection.HelpText != expectedSection.HelpText || liveSection.HelpUrl != expectedSection.HelpUrl)
                    {
                        liveSection.UpdateDetails(expectedSection.Title, expectedSection.Description, expectedSection.HelpText, expectedSection.HelpUrl);
                        changed = true;
                    }

                    // Sections are matched by number regardless of IsActive, so a number the seeder
                    // reintroduces lands on the row that was deactivated when it was last retired.
                    // Without this it would settle correct but permanently invisible. Deliberately
                    // not mirrored for fields: an admin can deactivate a field through
                    // UpdateFormFieldCommand, and since DeleteFormFieldCommand refuses to delete one
                    // that has answers, that toggle is their only way to retire an answered question
                    // — reactivating here would undo it on every restart. Sections have no such
                    // competing writer.
                    if (!liveSection.IsActive)
                    {
                        liveSection.Activate();
                        changed = true;
                    }
                }
                else
                {
                    context.FormSections.Add(expectedSection);
                    liveSectionsByNumber[expectedSection.SectionNumber] = expectedSection;
                    changed = true;
                }
            }

            var liveFieldsByKey = module.Fields.ToDictionary(f => f.FieldKey);
            var expectedFieldsByKey = shadow.Fields.ToDictionary(f => f.FieldKey);

            foreach (var expected in shadow.Fields)
            {
                FormSectionId? expectedSectionId = null;
                if (expected.FormSectionId is { } shadowSectionId)
                {
                    var sectionNumber = shadow.Sections.First(s => s.Id == shadowSectionId).SectionNumber;
                    expectedSectionId = liveSectionsByNumber[sectionNumber].Id;
                }

                if (!liveFieldsByKey.TryGetValue(expected.FieldKey, out var live))
                {
                    expected.SetSection(expectedSectionId);
                    context.FormFields.Add(expected);
                    changed = true;
                    continue;
                }

                if (live.Label != expected.Label || live.DisplayOrder != expected.DisplayOrder || live.IsRequired != expected.IsRequired
                    || live.Placeholder != expected.Placeholder || live.HelpText != expected.HelpText || live.DefaultValue != expected.DefaultValue)
                {
                    live.UpdateDetails(expected.Label, expected.DisplayOrder, expected.IsRequired, expected.Placeholder, expected.HelpText, expected.DefaultValue);
                    changed = true;
                }

                if (live.FieldType != expected.FieldType)
                {
                    live.SetFieldType(expected.FieldType);
                    changed = true;
                }

                if (live.ValidationRules != expected.ValidationRules)
                {
                    live.SetValidationRules(expected.ValidationRules);
                    changed = true;
                }

                if (live.ConditionalRules != expected.ConditionalRules)
                {
                    live.SetConditionalRules(expected.ConditionalRules);
                    changed = true;
                }

                if (live.Configuration != expected.Configuration)
                {
                    live.SetConfiguration(expected.Configuration);
                    changed = true;
                }

                if (live.FormSectionId != expectedSectionId)
                {
                    live.SetSection(expectedSectionId);
                    changed = true;
                }

                var liveOptionsByValue = live.Options.ToDictionary(o => o.Value);
                foreach (var expectedOption in expected.Options)
                {
                    if (liveOptionsByValue.TryGetValue(expectedOption.Value, out var liveOption))
                    {
                        if (liveOption.Label != expectedOption.Label || liveOption.DisplayOrder != expectedOption.DisplayOrder || liveOption.IsDefault != expectedOption.IsDefault)
                        {
                            liveOption.UpdateDetails(expectedOption.Value, expectedOption.Label, expectedOption.DisplayOrder, expectedOption.IsDefault);
                            changed = true;
                        }
                    }
                    else
                    {
                        live.AddOption(expectedOption.Value, expectedOption.Label, expectedOption.DisplayOrder, expectedOption.IsDefault);
                        changed = true;
                    }
                }
            }

            // A field the seeder no longer defines is removed once nothing has answered it, and
            // deactivated — not removed — once something has, so the answers stay in the database
            // while the question stops being asked. Retaining it active is what kept the
            // pre-BLUM-7369 strand risk question on every PNMH/Infant Feeding/Healthy Babies
            // activity form: it was required, so every completed activity pinned it (BLUM-7564).
            foreach (var live in module.Fields.Where(f => !expectedFieldsByKey.ContainsKey(f.FieldKey)))
            {
                var hasValues = await context.FormFieldValues.IgnoreQueryFilters().AnyAsync(v => v.FormFieldId == live.Id, cancellationToken);
                if (!hasValues)
                {
                    context.FormFieldOptions.RemoveRange(live.Options);
                    context.FormFields.Remove(live);
                    changed = true;
                }
                else if (live.IsActive)
                {
                    live.Deactivate();
                    changed = true;
                }
            }

            // A section number the seeder no longer defines (e.g. two sections merged into one) is
            // removed once no field points to it any more — fields are reassigned above first — and
            // deactivated once the only fields left pointing at it are retired ones. Without the
            // second branch the form keeps a final wizard step with a stale heading and nothing in
            // it. The remove branch stays keyed on any field, active or not: FormField.FormSectionId
            // is OnDelete(SetNull), so deleting the section would strip the retained answer's
            // section context.
            var expectedSectionNumbers = shadow.Sections.Select(s => s.SectionNumber).ToHashSet();
            foreach (var orphanedSection in module.Sections.Where(s => !expectedSectionNumbers.Contains(s.SectionNumber)))
            {
                if (!module.Fields.Any(f => f.FormSectionId == orphanedSection.Id))
                {
                    context.FormSections.Remove(orphanedSection);
                    changed = true;
                }
                else if (orphanedSection.IsActive)
                {
                    orphanedSection.Deactivate();
                    changed = true;
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

        // Renumber sections 7–12 to make room for the new strand-level risk modules, inserted next to
        // their strand (PNMH and PIR Risk after PNMH and PIR, etc).
        var targetSectionNumbers = new Dictionary<string, int>
        {
            [DataCollectionFormModuleCodes.InfantFeeding] = 8,
            [DataCollectionFormModuleCodes.HealthyBabiesOffer] = 10,
            [DataCollectionFormModuleCodes.RolloverSpend] = 12,
        };

        var sectionNumberChanged = false;
        foreach (var module in existingModules)
        {
            if (targetSectionNumbers.TryGetValue(module.Code, out var targetSectionNumber) && module.SectionNumber != targetSectionNumber)
            {
                module.UpdateSectionNumber(targetSectionNumber);
                sectionNumberChanged = true;
            }
        }

        if (sectionNumberChanged)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        // Pair each strand with its standalone risk section. BLUM-7369 moved the strand-level risk
        // question into modules of its own, but a module only reaches a submission through an
        // assignment, and only an admin picking modules in the wizard writes those — so on a round
        // created before the risk modules existed the question simply vanished. Done here rather
        // than in an EF migration because migrations run before the seeder, so on an environment
        // that has not yet seeded the risk modules a migration would find nothing to assign.
        var riskModuleCodesByStrandCode = new Dictionary<string, string>
        {
            [DataCollectionFormModuleCodes.PnmhAndPir] = DataCollectionFormModuleCodes.PnmhAndPirRisk,
            [DataCollectionFormModuleCodes.InfantFeeding] = DataCollectionFormModuleCodes.InfantFeedingRisk,
            [DataCollectionFormModuleCodes.HealthyBabiesOffer] = DataCollectionFormModuleCodes.HealthyBabiesOfferRisk,
        };

        // Re-read rather than reuse existingModules: modules created by the factory loop at the top
        // of this method are not in that list, so a first run would find no risk modules to pair.
        var moduleIdsByCode = await context.DataCollectionFormModules
            .AsNoTracking()
            .ToDictionaryAsync(m => m.Code, m => m.Id, cancellationToken);

        var strandRiskPairs = riskModuleCodesByStrandCode
            .Where(pair => moduleIdsByCode.ContainsKey(pair.Key) && moduleIdsByCode.ContainsKey(pair.Value))
            .Select(pair => (StrandId: moduleIdsByCode[pair.Key], RiskId: moduleIdsByCode[pair.Value]))
            .ToList();

        if (strandRiskPairs.Count > 0)
        {
            // Soft-deleted rounds are excluded by DataCollection's global query filter. Finished
            // ones are skipped explicitly: a section nobody can still complete would sit at "Not
            // started" forever on the DHSC review. IsClosed alone is too narrow a test for that —
            // it is only the explicit admin flag, while what actually stops an LA is the deadline
            // (GetEffectiveEndDate: the round's EndDate, or that LA's override where one is set).
            // A round whose deadline is later extended or reopened picks the pairing up on the next
            // start, since this runs on every boot.
            // Compared against the start of today, not the current instant, because the deadline day
            // itself stays open (IsClosedFor). DateTime.UtcNow.Date keeps Kind=Utc, which the
            // timestamptz columns require.
            var startOfToday = DateTime.UtcNow.Date;
            var openDeliveryPlans = await context.DataCollections
                .Include(dc => dc.FormModuleAssignments)
                .Where(dc => dc.Type == DataCollectionType.DeliveryPlan
                             && !dc.IsClosed
                             && (dc.EndDate >= startOfToday
                                 || dc.LocalAuthorities.Any(la => la.EndDate != null && la.EndDate >= startOfToday)))
                .ToListAsync(cancellationToken);

            var assignmentsAdded = false;
            foreach (var dataCollection in openDeliveryPlans)
            {
                foreach (var (strandId, riskId) in strandRiskPairs)
                {
                    if (dataCollection.FormModuleAssignments.Any(a => a.FormModuleId == strandId)
                        && dataCollection.FormModuleAssignments.All(a => a.FormModuleId != riskId))
                    {
                        dataCollection.AssignFormModule(riskId);
                        assignmentsAdded = true;
                    }
                }
            }

            if (assignmentsAdded)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
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

        var f02 = module.AddField("F02", "Describe the top priority for the Delivery Grant.", FieldType.Text, 2, true, null, "Required if 'Other' is selected above.");
        f02.SetSection(sDg.Id);
        f02.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"F01\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var f03 = module.AddField("F03", "You may provide a short rationale for your chosen top priority for the Delivery Grant.", FieldType.Textarea, 3, false);
        f03.SetSection(sDg.Id);

        // ── Parenting ──
        var f04 = module.AddField("F04", "What is the top priority for Parenting?", FieldType.Select, 4, true);
        f04.SetSection(sP.Id);
        AddTf1PriorityOptions(f04);

        var f05 = module.AddField("F05", "Describe the top priority for Parenting.", FieldType.Text, 5, true, null, "Required if 'Other' is selected above.");
        f05.SetSection(sP.Id);
        f05.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"F04\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var f06 = module.AddField("F06", "You may provide a short rationale for your chosen top priority for Parenting.", FieldType.Textarea, 6, false);
        f06.SetSection(sP.Id);

        // ── HLE ──
        var f07 = module.AddField("F07", "What is the top priority for HLE?", FieldType.Select, 7, true);
        f07.SetSection(sH.Id);
        AddTf1PriorityOptions(f07);

        var f08 = module.AddField("F08", "Describe the top priority for HLE.", FieldType.Text, 8, true, null, "Required if 'Other' is selected above.");
        f08.SetSection(sH.Id);
        f08.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"F07\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var f09 = module.AddField("F09", "You may provide a short rationale for your chosen top priority for HLE.", FieldType.Textarea, 9, false);
        f09.SetSection(sH.Id);

        // ── SEND ──
        var f10 = module.AddField("F10", "What is the top priority for SEND?", FieldType.Select, 10, true);
        f10.SetSection(sSend.Id);
        AddTf1PriorityOptions(f10);

        var f11 = module.AddField("F11", "Describe the top priority for SEND.", FieldType.Text, 11, true, null, "Required if 'Other' is selected above.");
        f11.SetSection(sSend.Id);
        f11.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"F10\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var f12 = module.AddField("F12", "You may provide a short rationale for your chosen top priority for SEND.", FieldType.Textarea, 12, false);
        f12.SetSection(sSend.Id);

        // ── PNMH & PIR (TF2 only) ──
        var f13 = module.AddField("F13", "What is the top priority for PNMH & PIR?", FieldType.Select, 13, true);
        f13.SetSection(sPn.Id);
        AddTf2PriorityOptions(f13);

        var f14 = module.AddField("F14", "Describe the top priority for PNMH & PIR.", FieldType.Text, 14, true, null, "Required if 'Other' is selected above.");
        f14.SetSection(sPn.Id);
        f14.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"F13\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var f15 = module.AddField("F15", "You may provide a short rationale for your chosen top priority for PNMH & PIR.", FieldType.Textarea, 15, false, null, "Not required if 'The LA has not received funding for this strand' was selected.");
        f15.SetSection(sPn.Id);
        f15.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"F13\"," + showUnlessNotReceived + "}}");

        // ── Infant Feeding (TF2 only) ──
        var f16 = module.AddField("F16", "What is the top priority for Infant Feeding?", FieldType.Select, 16, true);
        f16.SetSection(sIf.Id);
        AddTf2PriorityOptions(f16);

        var f17 = module.AddField("F17", "Describe the top priority for Infant Feeding.", FieldType.Text, 17, true, null, "Required if 'Other' is selected above.");
        f17.SetSection(sIf.Id);
        f17.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"F16\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var f18 = module.AddField("F18", "You may provide a short rationale for your chosen top priority for Infant Feeding.", FieldType.Textarea, 18, false, null, "Not required if 'The LA has not received funding for this strand' was selected.");
        f18.SetSection(sIf.Id);
        f18.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"F16\"," + showUnlessNotReceived + "}}");

        // ── Healthy Babies Offer (TF2 only) ──
        var f19 = module.AddField("F19", "What is the top priority for the Healthy Babies Offer?", FieldType.Select, 19, true);
        f19.SetSection(sHbo.Id);
        AddTf2PriorityOptions(f19);

        var f20 = module.AddField("F20", "Describe the top priority for Healthy Babies Offer.", FieldType.Text, 20, true, null, "Required if 'Other' is selected above.");
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
        // Activity ID has no section of its own — a single read-only auto-generated field isn't worth
        // its own wizard step, so it's the first field of Activity Details instead.
        var s2 = module.AddSection(1, "Activity Details", "Theme, status and delivery information.");
        var s3 = module.AddSection(2, "Funding", "Financial forecasts and actuals.");
        var s4 = module.AddSection(3, "Progress and Risks", "Activity changes and risk management.");

        var dg01 = module.AddField("DG01", "Activity ID", FieldType.ReadOnly, 1, false, null, "Assigned automatically as a unique reference for this activity.");
        dg01.SetSection(s2.Id);

        var dg02 = module.AddField("DG02", "Select the overarching theme or area of focus", FieldType.Select, 2, true);
        dg02.SetSection(s2.Id);
        dg02.AddOption("system_leadership", "System Leadership", 1);
        dg02.AddOption("partnership_working", "Partnership working", 2);
        dg02.AddOption("outreach", "Outreach", 3);
        dg02.AddOption("best_start_sites", "Best Start Family Hub Sites", 4);
        dg02.AddOption("connections", "Connections", 5);
        dg02.AddOption("relationships", "Relationships", 6);
        dg02.AddOption("access_inclusion", "Access and Inclusion", 7);
        dg02.AddOption("staff", "Staff", 8);
        dg02.AddOption("insights", "Insights", 9);
        dg02.AddOption("data_digital", "Data and Digital", 10);
        dg02.AddOption("other", "Other", 11);

        var dg03 = module.AddField("DG03", "Provide further details for the theme selected", FieldType.Text, 3, true, null, "Required if 'Other' is selected.");
        dg03.SetSection(s2.Id);
        dg03.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"DG02\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var dg04 = module.AddField("DG04", "Enter the name and description of all activities delivered within this theme", FieldType.Textarea, 4, true);
        dg04.SetSection(s2.Id);

        var dg05 = module.AddField("DG05", "Is this a new or continuing activity?", FieldType.Radio, 5, true);
        dg05.SetSection(s2.Id);
        dg05.SetConfiguration("{\"size\":\"small\"}");
        dg05.AddOption("new", "New", 1);
        dg05.AddOption("continuing", "Continuing", 2);

        var dg05_1 = module.AddField("DG05.1", "Is this activity funding service(s)?", FieldType.Radio, 6, true);
        dg05_1.SetSection(s2.Id);
        dg05_1.SetConfiguration("{\"size\":\"small\"}");
        dg05_1.AddOption("yes", "Yes", 1);
        dg05_1.AddOption("no", "No", 2);

        var dg05_2 = module.AddField("DG05.2", "Which service is the activity funding?", FieldType.Checkbox, 7, true, null, "Select every service this activity funds.");
        dg05_2.SetSection(s2.Id);
        dg05_2.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"DG05.1\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        dg05_2.SetConfiguration("{\"dynamicOptions\":\"services\"}");

        var dg06 = module.AddField("DG06", "Select the activity status", FieldType.Select, 8, true);
        dg06.SetSection(s2.Id);
        dg06.AddOption("started", "Started", 1);
        dg06.AddOption("not_started", "Not started", 2);
        dg06.AddOption("delayed", "Delayed", 3);
        dg06.AddOption("completed", "Completed", 4);

        var dg07 = module.AddField("DG07", "Select the delivery status", FieldType.Select, 9, true);
        dg07.SetSection(s2.Id);
        dg07.AddOption("on_track", "On track", 1);
        dg07.AddOption("off_track", "Off track", 2);

        var dg08 = module.AddField("DG08", "Is this activity funded by other sources than Best Start Family Hubs and Healthy Babies?", FieldType.Radio, 10, true);
        dg08.SetSection(s3.Id);
        dg08.SetConfiguration("{\"size\":\"small\"}");
        dg08.AddOption("yes", "Yes", 1);
        dg08.AddOption("no", "No", 2);

        var dg09 = module.AddField("DG09", "Total amount of additional funding", FieldType.Currency, 11, true, null, "Required if additional funding sources are present.", "0");
        dg09.SetSection(s3.Id);
        dg09.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"DG08\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        dg09.SetConfiguration("{\"group\":\"DG08_extra\",\"groupLabel\":\"Additional funding details\",\"prefix\":\"£\"}");

        var dg10 = module.AddField("DG10", "Source of additional funding", FieldType.Text, 12, true, null, "Required if additional funding sources are present.");
        dg10.SetSection(s3.Id);
        dg10.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"DG08\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        dg10.SetConfiguration("{\"group\":\"DG08_extra\"}");

        var dg11 = module.AddField("DG11", "How much Delivery Grant funding are you forecasting to spend on this activity in 2026-2027?", FieldType.Currency, 13, true, null, null, "0");
        dg11.SetSection(s3.Id);
        dg11.SetConfiguration("{\"prefix\":\"£\"}");

        var dg12 = module.AddField("DG12", "Total Activity Budget (26/27)", FieldType.Calculation, 14, false, null, "DG09 + DG11");
        dg12.SetSection(s3.Id);
        dg12.SetConfiguration("{\"formula\":\"DG09 + DG11\",\"prefix\":\"£\"}");

        var dg13 = module.AddField("DG13", "What is the total amount of actual spend on this activity to date?", FieldType.Currency, 15, true, null, null, "0");
        dg13.SetSection(s3.Id);
        dg13.SetConfiguration("{\"prefix\":\"£\"}");

        var dg14 = module.AddField("DG14", "What is the amount of funding committed to this activity?", FieldType.Currency, 16, false, null, null, "0");
        dg14.SetSection(s3.Id);
        dg14.SetConfiguration("{\"prefix\":\"£\"}");

        var dg15 = module.AddField("DG15", "Planned Spend (Uncommitted)", FieldType.Calculation, 17, false, null, "DG11 - (DG13 + DG14)");
        dg15.SetSection(s3.Id);
        dg15.SetConfiguration("{\"formula\":\"DG11 - (DG13 + DG14)\",\"prefix\":\"£\"}");

        var dg16 = module.AddField("DG16", "How much funding are you forecasting to spend on this activity in 2027-2028?", FieldType.Currency, 18, true, null, null, "0");
        dg16.SetSection(s3.Id);
        dg16.SetConfiguration("{\"prefix\":\"£\"}");

        var dg17 = module.AddField("DG17", "How much funding are you forecasting to spend on this activity in 2028-2029?", FieldType.Currency, 19, true, null, null, "0");
        dg17.SetSection(s3.Id);
        dg17.SetConfiguration("{\"prefix\":\"£\"}");

        var dg18 = module.AddField("DG18", "Briefly outline what activity changed, why and when.", FieldType.Textarea, 20, false);
        dg18.SetSection(s4.Id);

        var dg19 = module.AddField("DG19", "Are there any risks associated with this activity?", FieldType.Radio, 21, true);
        dg19.SetSection(s4.Id);
        dg19.SetConfiguration("{\"size\":\"small\"}");
        dg19.AddOption("yes", "Yes", 1);
        dg19.AddOption("no", "No", 2);

        var dg20 = module.AddField("DG20", "Outline the identified risks", FieldType.Textarea, 22, true, null, "Required if risks are present.");
        dg20.SetSection(s4.Id);
        dg20.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"DG19\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        dg20.SetConfiguration("{\"group\":\"DG19_risk\",\"groupLabel\":\"Risk details\"}");

        var dg21 = module.AddField("DG21", "Select the likelihood of this risk", FieldType.Select, 23, true, null, "Required if risks are present.");
        dg21.SetSection(s4.Id);
        dg21.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"DG19\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        dg21.SetConfiguration("{\"group\":\"DG19_risk\"}");
        dg21.AddOption("low", "Low", 1);
        dg21.AddOption("medium", "Medium", 2);
        dg21.AddOption("high", "High", 3);

        var dg22 = module.AddField("DG22", "Select the impact of this risk", FieldType.Select, 24, true, null, "Required if risks are present.");
        dg22.SetSection(s4.Id);
        dg22.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"DG19\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        dg22.SetConfiguration("{\"group\":\"DG19_risk\"}");
        dg22.AddOption("low", "Low", 1);
        dg22.AddOption("medium", "Medium", 2);
        dg22.AddOption("high", "High", 3);

        var dg23 = module.AddField("DG23", "Give details of the mitigations in place or planned", FieldType.Textarea, 25, true, null, "Required if risks are present.");
        dg23.SetSection(s4.Id);
        dg23.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"DG19\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        dg23.SetConfiguration("{\"group\":\"DG19_risk\"}");
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
        // Activity ID has no section of its own — a single read-only auto-generated field isn't worth
        // its own wizard step, so it's the first field of Activity Details instead.
        var s2 = module.AddSection(1, "Activity Details", "Theme, expectations and delivery information.");
        var s3 = module.AddSection(2, "Funding", "Financial forecasts and actuals.");
        var s4 = module.AddSection(3, "Progress and Risks", "Activity changes and risk management.");

        var p01 = module.AddField("P01", "Activity ID", FieldType.ReadOnly, 1, false, null, "Assigned automatically as a unique reference for this activity.");
        p01.SetSection(s2.Id);

        var p02 = module.AddField("P02", "Select the overarching theme or area of focus", FieldType.Select, 2, true);
        p02.SetSection(s2.Id);
        p02.AddOption("ebi1", "EBI1", 1);
        p02.AddOption("ebi2", "EBI2", 2);
        p02.AddOption("ebi3", "EBI3", 3);
        p02.AddOption("ebi_extra", "EBI Extra", 4);
        p02.AddOption("alternative", "Alternative / Locally developed Programme", 5);
        p02.AddOption("peer_support", "Peer Support", 6);
        p02.AddOption("outreach", "Outreach", 7);
        p02.AddOption("partnership", "Partnership working", 8);
        p02.AddOption("staff", "Staff", 9);
        p02.AddOption("other", "Other", 10);

        var p03 = module.AddField("P03", "Describe the theme or area of focus", FieldType.Text, 3, true, null, "Required if 'Other' is selected.");
        p03.SetSection(s2.Id);
        p03.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"P02\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var p04 = module.AddField("P04", "Enter the name and description of all activities delivered within this theme", FieldType.Textarea, 4, true);
        p04.SetSection(s2.Id);

        var p05 = module.AddField("P05", "Which expectations do these activities meet?", FieldType.Checkbox, 5, true, null, "Select all that apply.");
        p05.SetSection(s2.Id);
        p05.AddOption("deliver_offer", "Deliver a parenting offer 0-5", 1);
        p05.AddOption("play_reading", "Play and shared reading resources", 2);
        p05.AddOption("prioritise_ebis", "Prioritise DfE parenting menus/EBIs", 3);
        p05.AddOption("maximise_takeup", "Maximise take up", 4);
        p05.AddOption("staff_equipped", "Staff equipped to deliver", 5);

        var p05_1 = module.AddField("P05.1", "Is this activity funding service(s)?", FieldType.Radio, 6, true);
        p05_1.SetSection(s2.Id);
        p05_1.SetConfiguration("{\"size\":\"small\"}");
        p05_1.AddOption("yes", "Yes", 1);
        p05_1.AddOption("no", "No", 2);

        var p05_2 = module.AddField("P05.2", "Which service is the activity funding?", FieldType.Checkbox, 7, true, null, "Select every service this activity funds.");
        p05_2.SetSection(s2.Id);
        p05_2.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"P05.1\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        p05_2.SetConfiguration("{\"dynamicOptions\":\"services\"}");

        var p06 = module.AddField("P06", "Is this a new or continuing activity?", FieldType.Radio, 8, true);
        p06.SetSection(s2.Id);
        p06.SetConfiguration("{\"size\":\"small\"}");
        p06.AddOption("new", "New", 1);
        p06.AddOption("continuing", "Continuing", 2);

        var p07 = module.AddField("P07", "Select the activity status", FieldType.Select, 9, true);
        p07.SetSection(s2.Id);
        p07.AddOption("started", "Started", 1);
        p07.AddOption("not_started", "Not started", 2);
        p07.AddOption("delayed", "Delayed", 3);
        p07.AddOption("completed", "Completed", 4);

        var p08 = module.AddField("P08", "Select the delivery status of this activity", FieldType.Select, 10, true);
        p08.SetSection(s2.Id);
        p08.AddOption("on_track", "On track", 1);
        p08.AddOption("off_track", "Off track", 2);

        var p09 = module.AddField("P09", "Is this activity receiving any additional funding from other sources?", FieldType.Radio, 11, true);
        p09.SetSection(s3.Id);
        p09.SetConfiguration("{\"size\":\"small\"}");
        p09.AddOption("yes", "Yes", 1);
        p09.AddOption("no", "No", 2);

        var p10 = module.AddField("P10", "Total amount of additional funding", FieldType.Currency, 12, true, null, "Required if additional funding sources are present.", "0");
        p10.SetSection(s3.Id);
        p10.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"P09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        p10.SetConfiguration("{\"group\":\"P09_extra\",\"groupLabel\":\"Additional funding details\",\"prefix\":\"£\"}");

        var p11 = module.AddField("P11", "Source of additional funding", FieldType.Text, 13, true, null, "Required if additional funding sources are present.");
        p11.SetSection(s3.Id);
        p11.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"P09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        p11.SetConfiguration("{\"group\":\"P09_extra\"}");

        var p12 = module.AddField("P12", "How much Parenting funding are you forecasting to spend in 2026-27?", FieldType.Currency, 14, true, null, null, "0");
        p12.SetSection(s3.Id);
        p12.SetConfiguration("{\"prefix\":\"£\"}");

        var p13 = module.AddField("P13", "Total Activity Budget (26/27)", FieldType.Calculation, 15, false, null, "P10 + P12");
        p13.SetSection(s3.Id);
        p13.SetConfiguration("{\"formula\":\"P10 + P12\",\"prefix\":\"£\"}");

        var p14 = module.AddField("P14", "What is the total amount of actual spend on this activity to date?", FieldType.Currency, 16, true, null, null, "0");
        p14.SetSection(s3.Id);
        p14.SetConfiguration("{\"prefix\":\"£\",\"maxSumField\":\"P13\"}");

        var p15 = module.AddField("P15", "What is the amount of funding committed to this activity?", FieldType.Currency, 17, false, null, null, "0");
        p15.SetSection(s3.Id);
        p15.SetConfiguration("{\"prefix\":\"£\"}");

        var p16 = module.AddField("P16", "Planned Spend (Uncommitted)", FieldType.Calculation, 18, false, null, "P12 - (P14 + P15). Flags overspend if negative.");
        p16.SetSection(s3.Id);
        p16.SetConfiguration("{\"formula\":\"P12 - (P14 + P15)\",\"prefix\":\"£\",\"warnIfNegative\":true}");

        var p17 = module.AddField("P17", "How much funding are you forecasting to spend in 2027-2028?", FieldType.Currency, 19, true, null, null, "0");
        p17.SetSection(s3.Id);
        p17.SetConfiguration("{\"prefix\":\"£\"}");

        var p18 = module.AddField("P18", "How much funding are you forecasting to spend in 2028-2029?", FieldType.Currency, 20, true, null, null, "0");
        p18.SetSection(s3.Id);
        p18.SetConfiguration("{\"prefix\":\"£\"}");

        var p19 = module.AddField("P19", "Briefly outline what activity changed, why and when.", FieldType.Textarea, 21, false);
        p19.SetSection(s4.Id);

        var p20 = module.AddField("P20", "Are there any risks associated with this activity?", FieldType.Radio, 22, true);
        p20.SetSection(s4.Id);
        p20.SetConfiguration("{\"size\":\"small\"}");
        p20.AddOption("yes", "Yes", 1);
        p20.AddOption("no", "No", 2);

        var p21 = module.AddField("P21", "Outline the identified risks", FieldType.Textarea, 23, true, null, "Required if risks are present.");
        p21.SetSection(s4.Id);
        p21.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"P20\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        p21.SetConfiguration("{\"group\":\"P20_risk\",\"groupLabel\":\"Risk details\"}");

        var p22 = module.AddField("P22", "Select the likelihood of this risk", FieldType.Select, 24, true, null, "Required if risks are present.");
        p22.SetSection(s4.Id);
        p22.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"P20\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        p22.SetConfiguration("{\"group\":\"P20_risk\"}");
        p22.AddOption("low", "Low", 1);
        p22.AddOption("medium", "Medium", 2);
        p22.AddOption("high", "High", 3);

        var p23 = module.AddField("P23", "Select the impact of this risk", FieldType.Select, 25, true, null, "Required if risks are present.");
        p23.SetSection(s4.Id);
        p23.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"P20\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        p23.SetConfiguration("{\"group\":\"P20_risk\"}");
        p23.AddOption("low", "Low", 1);
        p23.AddOption("medium", "Medium", 2);
        p23.AddOption("high", "High", 3);

        var p24 = module.AddField("P24", "Give details of the mitigations in place or planned", FieldType.Textarea, 26, true, null, "Required if risks are present.");
        p24.SetSection(s4.Id);
        p24.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"P20\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        p24.SetConfiguration("{\"group\":\"P20_risk\"}");
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
        // Activity ID has no section of its own — a single read-only auto-generated field isn't worth
        // its own wizard step, so it's the first field of Activity Details instead.
        var s2 = module.AddSection(1, "Activity Details", "Theme, status and delivery information.");
        var s3 = module.AddSection(2, "Funding", "Financial forecasts and actuals.");
        var s4 = module.AddSection(3, "Progress and Risks", "Activity changes and risk management.");

        var h01 = module.AddField("H01", "Activity ID", FieldType.ReadOnly, 1, false, null, "Assigned automatically as a unique reference for this activity.");
        h01.SetSection(s2.Id);

        var h02 = module.AddField("H02", "Select the overarching theme or area of focus", FieldType.Select, 2, true);
        h02.SetSection(s2.Id);
        h02.AddOption("ebi1", "EBI1", 1);
        h02.AddOption("ebi2", "EBI2", 2);
        h02.AddOption("ebi3", "EBI3", 3);
        h02.AddOption("ebi_extra", "EBI Extra", 4);
        h02.AddOption("alternative", "Alternative / Locally developed Programme", 5);
        h02.AddOption("peer_support", "Peer Support", 6);
        h02.AddOption("outreach", "Outreach", 7);
        h02.AddOption("partnership", "Partnership working", 8);
        h02.AddOption("staff", "Staff", 9);
        h02.AddOption("other", "Other", 10);

        var h03 = module.AddField("H03", "Describe the theme or area of focus", FieldType.Text, 3, true, null, "Required if 'Other' is selected.");
        h03.SetSection(s2.Id);
        h03.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"H02\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var h04 = module.AddField("H04", "Enter the name and description of all activities delivered within this theme", FieldType.Textarea, 4, true);
        h04.SetSection(s2.Id);

        var h05 = module.AddField("H05", "Which expectations do these activities meet?", FieldType.Checkbox, 5, true, null, "Select all that apply.");
        h05.SetSection(s2.Id);
        h05.AddOption("deliver_hle", "Deliver a HLE offer 0-5", 1);
        h05.AddOption("play_reading", "Play and shared reading resources", 2);
        h05.AddOption("prioritise_ebis", "Prioritise EBIs from DfE HLE menus", 3);
        h05.AddOption("maximise_takeup", "Maximise take up of HLE", 4);
        h05.AddOption("staff_equipped", "Staff equipped to deliver", 5);

        var h05_1 = module.AddField("H05.1", "Is this activity funding service(s)?", FieldType.Radio, 6, true);
        h05_1.SetSection(s2.Id);
        h05_1.SetConfiguration("{\"size\":\"small\"}");
        h05_1.AddOption("yes", "Yes", 1);
        h05_1.AddOption("no", "No", 2);

        var h05_2 = module.AddField("H05.2", "Which service is the activity funding?", FieldType.Checkbox, 7, true, null, "Select every service this activity funds.");
        h05_2.SetSection(s2.Id);
        h05_2.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"H05.1\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        h05_2.SetConfiguration("{\"dynamicOptions\":\"services\"}");

        var h06 = module.AddField("H06", "Is this a new or continuing activity?", FieldType.Radio, 8, true);
        h06.SetSection(s2.Id);
        h06.SetConfiguration("{\"size\":\"small\"}");
        h06.AddOption("new", "New", 1);
        h06.AddOption("continuing", "Continuing", 2);

        var h07 = module.AddField("H07", "Select the activity status", FieldType.Select, 9, true);
        h07.SetSection(s2.Id);
        h07.AddOption("started", "Started", 1);
        h07.AddOption("not_started", "Not started", 2);
        h07.AddOption("delayed", "Delayed", 3);
        h07.AddOption("completed", "Completed", 4);

        var h08 = module.AddField("H08", "Select the delivery status of this activity", FieldType.Select, 10, true);
        h08.SetSection(s2.Id);
        h08.AddOption("on_track", "On track", 1);
        h08.AddOption("off_track", "Off track", 2);

        var h09 = module.AddField("H09", "Is this activity receiving any additional funding from other sources?", FieldType.Radio, 11, true);
        h09.SetSection(s3.Id);
        h09.SetConfiguration("{\"size\":\"small\"}");
        h09.AddOption("yes", "Yes", 1);
        h09.AddOption("no", "No", 2);

        var h10 = module.AddField("H10", "Total amount of additional funding", FieldType.Currency, 12, true, null, "Required if additional funding sources are present.", "0");
        h10.SetSection(s3.Id);
        h10.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"H09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        h10.SetConfiguration("{\"group\":\"H09_extra\",\"groupLabel\":\"Additional funding details\",\"prefix\":\"£\"}");

        var h11 = module.AddField("H11", "Source of additional funding", FieldType.Text, 13, true, null, "Required if additional funding sources are present.");
        h11.SetSection(s3.Id);
        h11.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"H09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        h11.SetConfiguration("{\"group\":\"H09_extra\"}");

        var h12 = module.AddField("H12", "How much HLE funding are you forecasting to spend in 2026-27?", FieldType.Currency, 14, true, null, null, "0");
        h12.SetSection(s3.Id);
        h12.SetConfiguration("{\"prefix\":\"£\"}");

        var h13 = module.AddField("H13", "Total Activity Budget (26/27)", FieldType.Calculation, 15, false, null, "H10 + H12");
        h13.SetSection(s3.Id);
        h13.SetConfiguration("{\"formula\":\"H10 + H12\",\"prefix\":\"£\"}");

        var h14 = module.AddField("H14", "What is the total amount of actual spend on this activity to date?", FieldType.Currency, 16, true, null, null, "0");
        h14.SetSection(s3.Id);
        h14.SetConfiguration("{\"prefix\":\"£\"}");

        var h15 = module.AddField("H15", "What is the amount of funding committed to this activity?", FieldType.Currency, 17, false, null, null, "0");
        h15.SetSection(s3.Id);
        h15.SetConfiguration("{\"prefix\":\"£\"}");

        var h16 = module.AddField("H16", "Planned Spend (Uncommitted)", FieldType.Calculation, 18, false, null, "H12 - (H14 + H15). Flags overspend if negative.");
        h16.SetSection(s3.Id);
        h16.SetConfiguration("{\"formula\":\"H12 - (H14 + H15)\",\"prefix\":\"£\"}");

        var h17 = module.AddField("H17", "How much funding are you forecasting to spend in 2027-2028?", FieldType.Currency, 19, true, null, null, "0");
        h17.SetSection(s3.Id);
        h17.SetConfiguration("{\"prefix\":\"£\"}");

        var h18 = module.AddField("H18", "How much funding are you forecasting to spend in 2028-2029?", FieldType.Currency, 20, true, null, null, "0");
        h18.SetSection(s3.Id);
        h18.SetConfiguration("{\"prefix\":\"£\"}");

        var h19 = module.AddField("H19", "Briefly outline what activity changed, why and when.", FieldType.Textarea, 21, false);
        h19.SetSection(s4.Id);

        var h20 = module.AddField("H20", "Are there any risks associated with this activity?", FieldType.Radio, 22, true);
        h20.SetSection(s4.Id);
        h20.SetConfiguration("{\"size\":\"small\"}");
        h20.AddOption("yes", "Yes", 1);
        h20.AddOption("no", "No", 2);

        var h21 = module.AddField("H21", "Outline the identified risks", FieldType.Textarea, 23, true, null, "Required if risks are present.");
        h21.SetSection(s4.Id);
        h21.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"H20\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        h21.SetConfiguration("{\"group\":\"H20_risk\",\"groupLabel\":\"Risk details\"}");

        var h22 = module.AddField("H22", "Select the likelihood of this risk", FieldType.Select, 24, true, null, "Required if risks are present.");
        h22.SetSection(s4.Id);
        h22.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"H20\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        h22.SetConfiguration("{\"group\":\"H20_risk\"}");
        h22.AddOption("low", "Low", 1);
        h22.AddOption("medium", "Medium", 2);
        h22.AddOption("high", "High", 3);

        var h23 = module.AddField("H23", "Select the impact of this risk", FieldType.Select, 25, true, null, "Required if risks are present.");
        h23.SetSection(s4.Id);
        h23.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"H20\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        h23.SetConfiguration("{\"group\":\"H20_risk\"}");
        h23.AddOption("low", "Low", 1);
        h23.AddOption("medium", "Medium", 2);
        h23.AddOption("high", "High", 3);

        var h24 = module.AddField("H24", "Give details of the mitigations in place or planned", FieldType.Textarea, 26, true, null, "Required if risks are present.");
        h24.SetSection(s4.Id);
        h24.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"H20\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        h24.SetConfiguration("{\"group\":\"H20_risk\"}");
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
        // Activity ID has no section of its own — a single read-only auto-generated field isn't worth
        // its own wizard step, so it's the first field of Activity Details instead.
        var s2 = module.AddSection(1, "Activity Details", "Theme, status and delivery information.");
        var s3 = module.AddSection(2, "Funding", "Financial forecasts and actuals.");
        var s4 = module.AddSection(3, "Progress and Risks", "Activity changes and risk management.");

        var send01 = module.AddField("SEND01", "Activity ID", FieldType.ReadOnly, 1, false, null, "Assigned automatically as a unique reference for this activity.");
        send01.SetSection(s2.Id);

        var send02 = module.AddField("SEND02", "Select the overarching theme or area of focus", FieldType.Select, 2, true);
        send02.SetSection(s2.Id);
        send02.AddOption("appoint_bsips", "Appoint BSIPS", 1);
        send02.AddOption("ebi1", "EBI 1 (prescribed list)", 2);
        send02.AddOption("ebi_extra", "Extra EBI (prescribed list)", 3);
        send02.AddOption("vcf_local", "VCF/locally developed interventions", 4);
        send02.AddOption("other", "Other", 5);

        var send03 = module.AddField("SEND03", "Provide further details for the theme selected", FieldType.Text, 3, true, null, "Required if 'Other' is selected.");
        send03.SetSection(s2.Id);
        send03.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SEND02\",\"equals\":\"other\"},\"displayInline\":true,\"parentOption\":\"other\"}");

        var send04 = module.AddField("SEND04", "Enter the name and description of all activities delivered within this funded strand", FieldType.Textarea, 4, true);
        send04.SetSection(s2.Id);

        var send05 = module.AddField("SEND05", "Which expectations do these activities meet?", FieldType.Checkbox, 5, true, null, "Select all that apply.");
        send05.SetSection(s2.Id);
        send05.AddOption("appoint_bsips", "Appoint BSIPs", 1);
        send05.AddOption("build_workforce", "Build workforce capability", 2);
        send05.AddOption("identify_need", "Identify emerging additional need", 3);
        send05.AddOption("prepare_ebi", "Prepare EBI/child interventions", 4);
        send05.AddOption("strengthen_partnerships", "Strengthen SEND partnerships", 5);
        send05.AddOption("strategic_role", "Strategic/system role", 6);

        var send05_1 = module.AddField("SEND05.1", "Is this activity funding service(s)?", FieldType.Radio, 6, true);
        send05_1.SetSection(s2.Id);
        send05_1.SetConfiguration("{\"size\":\"small\"}");
        send05_1.AddOption("yes", "Yes", 1);
        send05_1.AddOption("no", "No", 2);

        var send05_2 = module.AddField("SEND05.2", "Which service is the activity funding?", FieldType.Checkbox, 7, true, null, "Select every service this activity funds.");
        send05_2.SetSection(s2.Id);
        send05_2.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SEND05.1\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        send05_2.SetConfiguration("{\"dynamicOptions\":\"services\"}");

        var send06 = module.AddField("SEND06", "Is this a new or continuing activity?", FieldType.Radio, 8, true);
        send06.SetSection(s2.Id);
        send06.SetConfiguration("{\"size\":\"small\"}");
        send06.AddOption("new", "New", 1);
        send06.AddOption("continuing", "Continuing", 2);

        var send07 = module.AddField("SEND07", "Select the activity status", FieldType.Select, 9, true);
        send07.SetSection(s2.Id);
        send07.AddOption("started", "Started", 1);
        send07.AddOption("not_started", "Not started", 2);
        send07.AddOption("delayed", "Delayed", 3);
        send07.AddOption("completed", "Completed", 4);

        var send08 = module.AddField("SEND08", "Select the delivery status of this activity", FieldType.Select, 10, true);
        send08.SetSection(s2.Id);
        send08.AddOption("on_track", "On track", 1);
        send08.AddOption("off_track", "Off track", 2);

        var send09 = module.AddField("SEND09", "Is this activity receiving any additional funding from other sources?", FieldType.Radio, 11, true);
        send09.SetSection(s3.Id);
        send09.SetConfiguration("{\"size\":\"small\"}");
        send09.AddOption("yes", "Yes", 1);
        send09.AddOption("no", "No", 2);

        var send10 = module.AddField("SEND10", "Total amount of additional funding", FieldType.Currency, 12, true, null, "Required if additional funding sources are present.", "0");
        send10.SetSection(s3.Id);
        send10.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SEND09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        send10.SetConfiguration("{\"group\":\"SEND09_extra\",\"groupLabel\":\"Additional funding details\",\"prefix\":\"£\"}");

        var send11 = module.AddField("SEND11", "Source of additional funding", FieldType.Text, 13, true, null, "Required if additional funding sources are present.");
        send11.SetSection(s3.Id);
        send11.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SEND09\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        send11.SetConfiguration("{\"group\":\"SEND09_extra\"}");

        var send12 = module.AddField("SEND12", "How much SEND funding are you forecasting to spend in 2026-27?", FieldType.Currency, 14, true, null, null, "0");
        send12.SetSection(s3.Id);
        send12.SetConfiguration("{\"prefix\":\"£\"}");

        var send13 = module.AddField("SEND13", "Total Activity Budget (26/27)", FieldType.Calculation, 15, false, null, "SEND10 + SEND12");
        send13.SetSection(s3.Id);
        send13.SetConfiguration("{\"formula\":\"SEND10 + SEND12\",\"prefix\":\"£\"}");

        var send14 = module.AddField("SEND14", "What is the total amount of actual spend on this activity to date?", FieldType.Currency, 16, true, null, null, "0");
        send14.SetSection(s3.Id);
        send14.SetConfiguration("{\"prefix\":\"£\",\"maxSumField\":\"SEND13\"}");

        var send15 = module.AddField("SEND15", "What is the amount of funding committed to this activity?", FieldType.Currency, 17, false, null, null, "0");
        send15.SetSection(s3.Id);
        send15.SetConfiguration("{\"prefix\":\"£\"}");

        var send16 = module.AddField("SEND16", "Planned Spend (Uncommitted)", FieldType.Calculation, 18, false, null, "SEND12 - (SEND14 + SEND15). Flags overspend if negative.");
        send16.SetSection(s3.Id);
        send16.SetConfiguration("{\"formula\":\"SEND12 - (SEND14 + SEND15)\",\"prefix\":\"£\",\"warnIfNegative\":true}");

        var send17 = module.AddField("SEND17", "How much funding are you forecasting to spend in 2027-2028?", FieldType.Currency, 19, true, null, null, "0");
        send17.SetSection(s3.Id);
        send17.SetConfiguration("{\"prefix\":\"£\"}");

        var send18 = module.AddField("SEND18", "How much funding are you forecasting to spend in 2028-2029?", FieldType.Currency, 20, true, null, null, "0");
        send18.SetSection(s3.Id);
        send18.SetConfiguration("{\"prefix\":\"£\"}");

        var send19 = module.AddField("SEND19", "Briefly outline what activity changed, why and when.", FieldType.Textarea, 21, false);
        send19.SetSection(s4.Id);

        var send20 = module.AddField("SEND20", "Are there any risks associated with this activity?", FieldType.Radio, 22, true);
        send20.SetSection(s4.Id);
        send20.SetConfiguration("{\"size\":\"small\"}");
        send20.AddOption("yes", "Yes", 1);
        send20.AddOption("no", "No", 2);

        var send21 = module.AddField("SEND21", "Outline the identified risks", FieldType.Textarea, 23, true, null, "Required if risks are present.");
        send21.SetSection(s4.Id);
        send21.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SEND20\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        send21.SetConfiguration("{\"group\":\"SEND20_risk\",\"groupLabel\":\"Risk details\"}");

        var send22 = module.AddField("SEND22", "Select the likelihood of this risk", FieldType.Select, 24, true, null, "Required if risks are present.");
        send22.SetSection(s4.Id);
        send22.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SEND20\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        send22.SetConfiguration("{\"group\":\"SEND20_risk\"}");
        send22.AddOption("low", "Low", 1);
        send22.AddOption("medium", "Medium", 2);
        send22.AddOption("high", "High", 3);

        var send23 = module.AddField("SEND23", "Select the impact of this risk", FieldType.Select, 25, true, null, "Required if risks are present.");
        send23.SetSection(s4.Id);
        send23.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SEND20\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        send23.SetConfiguration("{\"group\":\"SEND20_risk\"}");
        send23.AddOption("low", "Low", 1);
        send23.AddOption("medium", "Medium", 2);
        send23.AddOption("high", "High", 3);

        var send24 = module.AddField("SEND24", "Give details of the mitigations in place or planned", FieldType.Textarea, 26, true, null, "Required if risks are present.");
        send24.SetSection(s4.Id);
        send24.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"SEND20\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        send24.SetConfiguration("{\"group\":\"SEND20_risk\"}");
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
        // Activity ID has no section of its own — a single read-only auto-generated field isn't worth
        // its own wizard step, so it's the first field of Activity Details instead.
        var s2 = module.AddSection(1, "Activity Details", "Activity type, expectations and status.");
        var s3 = module.AddSection(2, "Dates and Status", "Planned and actual activity dates.");
        var s4 = module.AddSection(3, "Funding", "Financial forecasts and actuals.");
        var s5 = module.AddSection(4, "Progress", "Activity progress.");

        var pn01 = module.AddField("PN01", "Activity ID", FieldType.ReadOnly, 1, false, null, "Assigned automatically as a unique reference for this activity.");
        pn01.SetSection(s2.Id);

        var pn02 = module.AddField("PN02", "Provide the name and a brief description of the activity.", FieldType.Textarea, 2, true);
        pn02.SetSection(s2.Id);

        var pn03 = module.AddField("PN03", "Select the activity type", FieldType.Select, 3, true);
        pn03.SetSection(s2.Id);
        pn03.AddOption("training", "Training", 1);
        pn03.AddOption("service_delivery", "Service Delivery", 2);
        pn03.AddOption("staff", "Staff", 3);
        pn03.AddOption("other", "Other", 4);

        var pn04 = module.AddField("PN04", "Which expectations does this activity meet?", FieldType.Checkbox, 4, true, null, "Multi-select.");
        pn04.SetSection(s2.Id);
        pn04.AddOption("safe_inclusive", "Ensure BSFHs provide safe, inclusive and supportive environments", 1);
        pn04.AddOption("co_locate", "Co-locate PNMH and PIR support", 2);
        pn04.AddOption("build_on_mature", "Build on mature PNMH and PIR systems", 3);
        pn04.AddOption("clear_info", "Provide clear information and advice", 4);
        pn04.AddOption("high_quality", "Provide high quality, evidence-based support", 5);
        pn04.AddOption("shared_vision", "Build a shared system wide vision", 6);
        pn04.AddOption("empower_workforce", "Empower the workforce through training/supervision", 7);

        var pn05 = module.AddField("PN05", "Is this a new or continuing activity?", FieldType.Radio, 5, true);
        pn05.SetSection(s2.Id);
        pn05.SetConfiguration("{\"size\":\"small\"}");
        pn05.AddOption("new", "New", 1);
        pn05.AddOption("continuing", "Continuing", 2);

        var pn05_1 = module.AddField("PN05.1", "Is this activity funding service(s)?", FieldType.Radio, 6, true);
        pn05_1.SetSection(s2.Id);
        pn05_1.SetConfiguration("{\"size\":\"small\"}");
        pn05_1.AddOption("yes", "Yes", 1);
        pn05_1.AddOption("no", "No", 2);

        var pn05_2 = module.AddField("PN05.2", "Which service is the activity funding?", FieldType.Checkbox, 7, true, null, "Select every service this activity funds.");
        pn05_2.SetSection(s2.Id);
        pn05_2.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PN05.1\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        pn05_2.SetConfiguration("{\"dynamicOptions\":\"services\"}");

        var pn06 = module.AddField("PN06", "Provide the planned start date for this activity", FieldType.Date, 8, true, "DD/MM/YYYY");
        pn06.SetSection(s3.Id);

        var pn07 = module.AddField("PN07", "Provide the actual date from which this activity commenced", FieldType.Date, 9, false, "DD/MM/YYYY");
        pn07.SetSection(s3.Id);
        pn07.SetConfiguration("{\"dateRule\":\"not_in_future\"}");

        var pn08 = module.AddField("PN08", "Provide the date you expect this activity to be completed", FieldType.Date, 10, true, "DD/MM/YYYY", "Cannot be a date before PN06.");
        pn08.SetSection(s3.Id);
        pn08.SetConfiguration("{\"notBefore\":\"PN06\"}");

        var pn09 = module.AddField("PN09", "Provide the date this activity was completed", FieldType.Date, 11, false, "DD/MM/YYYY", "Cannot be in the future.");
        pn09.SetSection(s3.Id);
        pn09.SetConfiguration("{\"dateRule\":\"not_in_future\"}");

        var pn10 = module.AddField("PN10", "Select the activity status", FieldType.Select, 12, true);
        pn10.SetSection(s3.Id);
        pn10.AddOption("started", "Started", 1);
        pn10.AddOption("not_started", "Not started", 2);
        pn10.AddOption("delayed", "Delayed", 3);
        pn10.AddOption("completed", "Completed", 4);

        var pn11 = module.AddField("PN11", "Select the delivery status of this activity", FieldType.Select, 13, true);
        pn11.SetSection(s3.Id);
        pn11.AddOption("on_track", "On track", 1);
        pn11.AddOption("off_track", "Off track", 2);

        var pn12 = module.AddField("PN12", "Is this activity receiving any additional funding from other sources?", FieldType.Radio, 14, true);
        pn12.SetSection(s4.Id);
        pn12.SetConfiguration("{\"size\":\"small\"}");
        pn12.AddOption("yes", "Yes", 1);
        pn12.AddOption("no", "No", 2);

        var pn13 = module.AddField("PN13", "Total amount of additional funding", FieldType.Currency, 15, true, null, "Required if additional funding sources are present.", "0");
        pn13.SetSection(s4.Id);
        pn13.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PN12\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        pn13.SetConfiguration("{\"group\":\"PN12_extra\",\"groupLabel\":\"Additional funding details\",\"prefix\":\"£\"}");

        var pn14 = module.AddField("PN14", "Source of additional funding", FieldType.Text, 16, true, null, "Required if additional funding sources are present.");
        pn14.SetSection(s4.Id);
        pn14.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"PN12\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        pn14.SetConfiguration("{\"group\":\"PN12_extra\"}");

        var pn15 = module.AddField("PN15", "How much PNMH & PIR funding are you forecasting to spend in 2026-27?", FieldType.Currency, 17, true, null, null, "0");
        pn15.SetSection(s4.Id);
        pn15.SetConfiguration("{\"prefix\":\"£\"}");

        var pn16 = module.AddField("PN16", "Total Activity Budget (26/27)", FieldType.Calculation, 18, false, null, "PN13 + PN15");
        pn16.SetSection(s4.Id);
        pn16.SetConfiguration("{\"formula\":\"PN13 + PN15\",\"prefix\":\"£\"}");

        var pn17 = module.AddField("PN17", "What is the total amount of actual spend on this activity to date?", FieldType.Currency, 19, true, null, null, "0");
        pn17.SetSection(s4.Id);
        pn17.SetConfiguration("{\"prefix\":\"£\"}");

        var pn18 = module.AddField("PN18", "What is the amount of funding committed to this activity?", FieldType.Currency, 20, false, null, null, "0");
        pn18.SetSection(s4.Id);
        pn18.SetConfiguration("{\"prefix\":\"£\"}");

        var pn19 = module.AddField("PN19", "Planned Spend (Uncommitted)", FieldType.Calculation, 21, false, null, "PN15 - (PN17 + PN18). Flags overspend if negative.");
        pn19.SetSection(s4.Id);
        pn19.SetConfiguration("{\"formula\":\"PN15 - (PN17 + PN18)\",\"prefix\":\"£\"}");

        var pn20 = module.AddField("PN20", "How much funding are you forecasting to spend in 2027-2028?", FieldType.Currency, 22, true, null, null, "0");
        pn20.SetSection(s4.Id);
        pn20.SetConfiguration("{\"prefix\":\"£\"}");

        var pn21 = module.AddField("PN21", "How much funding are you forecasting to spend in 2028-2029?", FieldType.Currency, 23, true, null, null, "0");
        pn21.SetSection(s4.Id);
        pn21.SetConfiguration("{\"prefix\":\"£\"}");

        var pn22 = module.AddField("PN22", "Briefly outline what activity changed, why and when.", FieldType.Textarea, 24, false);
        pn22.SetSection(s5.Id);

        var pn23 = module.AddField("PN23", "Provide a narrative of how this activity is progressing.", FieldType.Textarea, 25, false, null, "Optional. Max 200 words.");
        pn23.SetSection(s5.Id);
    }

    // ─── PNMH and PIR strand level risk (TF2 only) ───────────────────────────

    private static DataCollectionFormModule CreatePnmhAndPirRiskModule(UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.PnmhAndPirRisk,
            7,
            "PNMH and PIR strand level risk",
            "Strand-level risk for PNMH and PIR (TF2 LAs only). Only needs to be completed once per strand.",
            DateTime.UtcNow,
            type: DataCollectionType.DeliveryPlan);

        PopulatePnmhAndPirRiskFields(module);

        if (systemUserId != UserId.Empty())
        {
            module.Publish(systemUserId);
        }

        return module;
    }

    private static void PopulatePnmhAndPirRiskFields(DataCollectionFormModule module)
    {
        var s1 = module.AddSection(1, "Strand Risk", "Risks to successful delivery of this strand.");

        var pn24 = module.AddField("PN24", "What are the risks to successful delivery of this strand?", FieldType.Checkbox, 1, true, null, "Only needs to be completed once per strand.");
        pn24.SetSection(s1.Id);
        pn24.AddOption("recruitment_delays", "Recruitment delays", 1);
        pn24.AddOption("procurement_delays", "Delays to procurement or start of contracts", 2);
        pn24.AddOption("s114_financial", "S114 notice or financial challenges", 3);
        pn24.AddOption("spend_delay_rollover", "Previous spend delay or rollover", 4);
        pn24.AddOption("wider_challenges", "Wider challenges/issues within LA", 5);
        pn24.AddOption("other", "Other", 6);
        pn24.AddOption("none_na", "None / NA", 7);
    }

    // ─── Infant Feeding (TF2 only) ────────────────────────────────────────────

    private static DataCollectionFormModule CreateInfantFeedingModule(UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.InfantFeeding,
            8,
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
        // Activity ID has no section of its own — a single read-only auto-generated field isn't worth
        // its own wizard step, so it's the first field of Activity Details instead.
        var s2 = module.AddSection(1, "Activity Details", "Activity type, expectations and status.");
        var s3 = module.AddSection(2, "Dates and Status", "Planned and actual activity dates.");
        var s4 = module.AddSection(3, "Funding", "Financial forecasts and actuals.");
        var s5 = module.AddSection(4, "Progress", "Activity progress.");

        var if01 = module.AddField("IF01", "Activity ID", FieldType.ReadOnly, 1, false, null, "Assigned automatically as a unique reference for this activity.");
        if01.SetSection(s2.Id);

        var if02 = module.AddField("IF02", "Provide the name and a brief description of the activity.", FieldType.Textarea, 2, true);
        if02.SetSection(s2.Id);

        var if03 = module.AddField("IF03", "Select the activity type", FieldType.Select, 3, true);
        if03.SetSection(s2.Id);
        if03.AddOption("training", "Training", 1);
        if03.AddOption("service_delivery", "Service Delivery", 2);
        if03.AddOption("staff", "Staff", 3);
        if03.AddOption("other", "Other", 4);

        var if04 = module.AddField("IF04", "Which expectations does this activity meet?", FieldType.Checkbox, 4, true, null, "Multi-select.");
        if04.SetSection(s2.Id);
        if04.AddOption("national_guidance", "Support aligned with national guidance", 1);
        if04.AddOption("co_locate", "Co-locate support", 2);
        if04.AddOption("build_on_mature", "Build on mature offers", 3);
        if04.AddOption("clear_info", "Clear evidence-based info", 4);
        if04.AddOption("high_quality", "High quality accessible support", 5);
        if04.AddOption("joined_up", "Joined up local system", 6);

        var if05 = module.AddField("IF05", "Is this a new or continuing activity?", FieldType.Radio, 5, true);
        if05.SetSection(s2.Id);
        if05.SetConfiguration("{\"size\":\"small\"}");
        if05.AddOption("new", "New", 1);
        if05.AddOption("continuing", "Continuing", 2);

        var if05_1 = module.AddField("IF05.1", "Is this activity funding service(s)?", FieldType.Radio, 6, true);
        if05_1.SetSection(s2.Id);
        if05_1.SetConfiguration("{\"size\":\"small\"}");
        if05_1.AddOption("yes", "Yes", 1);
        if05_1.AddOption("no", "No", 2);

        var if05_2 = module.AddField("IF05.2", "Which service is the activity funding?", FieldType.Checkbox, 7, true, null, "Select every service this activity funds.");
        if05_2.SetSection(s2.Id);
        if05_2.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"IF05.1\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        if05_2.SetConfiguration("{\"dynamicOptions\":\"services\"}");

        var if06 = module.AddField("IF06", "Provide the planned start date for this activity", FieldType.Date, 8, true, "DD/MM/YYYY");
        if06.SetSection(s3.Id);

        var if07 = module.AddField("IF07", "Provide the actual date from which this activity commenced", FieldType.Date, 9, false, "DD/MM/YYYY");
        if07.SetSection(s3.Id);
        if07.SetConfiguration("{\"dateRule\":\"not_in_future\"}");

        var if08 = module.AddField("IF08", "Provide the date you expect this activity to be completed", FieldType.Date, 10, true, "DD/MM/YYYY", "Cannot be a date before IF06.");
        if08.SetSection(s3.Id);
        if08.SetConfiguration("{\"notBefore\":\"IF06\"}");

        var if09 = module.AddField("IF09", "Provide the date this activity was completed", FieldType.Date, 11, false, "DD/MM/YYYY", "Cannot be in the future.");
        if09.SetSection(s3.Id);
        if09.SetConfiguration("{\"dateRule\":\"not_in_future\"}");

        var if10 = module.AddField("IF10", "Select the activity status", FieldType.Select, 12, true);
        if10.SetSection(s3.Id);
        if10.AddOption("started", "Started", 1);
        if10.AddOption("not_started", "Not started", 2);
        if10.AddOption("delayed", "Delayed", 3);
        if10.AddOption("completed", "Completed", 4);

        var if11 = module.AddField("IF11", "Select the delivery status of this activity", FieldType.Select, 13, true);
        if11.SetSection(s3.Id);
        if11.AddOption("on_track", "On track", 1);
        if11.AddOption("off_track", "Off track", 2);

        var if12 = module.AddField("IF12", "Is this activity receiving any additional funding from other sources?", FieldType.Radio, 14, true);
        if12.SetSection(s4.Id);
        if12.SetConfiguration("{\"size\":\"small\"}");
        if12.AddOption("yes", "Yes", 1);
        if12.AddOption("no", "No", 2);

        var if13 = module.AddField("IF13", "Total amount of additional funding", FieldType.Currency, 15, true, null, "Required if additional funding sources are present.", "0");
        if13.SetSection(s4.Id);
        if13.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"IF12\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        if13.SetConfiguration("{\"group\":\"IF12_extra\",\"groupLabel\":\"Additional funding details\",\"prefix\":\"£\"}");

        var if14 = module.AddField("IF14", "Source of additional funding", FieldType.Text, 16, true, null, "Required if additional funding sources are present.");
        if14.SetSection(s4.Id);
        if14.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"IF12\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        if14.SetConfiguration("{\"group\":\"IF12_extra\"}");

        var if15 = module.AddField("IF15", "How much Infant Feeding funding are you forecasting to spend in 2026-27?", FieldType.Currency, 17, true, null, null, "0");
        if15.SetSection(s4.Id);
        if15.SetConfiguration("{\"prefix\":\"£\"}");

        var if16 = module.AddField("IF16", "Total Activity Budget (26/27)", FieldType.Calculation, 18, false, null, "IF13 + IF15");
        if16.SetSection(s4.Id);
        if16.SetConfiguration("{\"formula\":\"IF13 + IF15\",\"prefix\":\"£\"}");

        var if17 = module.AddField("IF17", "What is the total amount of actual spend on this activity to date?", FieldType.Currency, 19, true, null, null, "0");
        if17.SetSection(s4.Id);
        if17.SetConfiguration("{\"prefix\":\"£\",\"maxSumField\":\"IF16\"}");

        var if18 = module.AddField("IF18", "What is the amount of funding committed to this activity?", FieldType.Currency, 20, false, null, null, "0");
        if18.SetSection(s4.Id);
        if18.SetConfiguration("{\"prefix\":\"£\"}");

        var if19 = module.AddField("IF19", "Planned Spend (Uncommitted)", FieldType.Calculation, 21, false, null, "IF15 - (IF17 + IF18). Flags overspend if negative.");
        if19.SetSection(s4.Id);
        if19.SetConfiguration("{\"formula\":\"IF15 - (IF17 + IF18)\",\"prefix\":\"£\",\"warnIfNegative\":true}");

        var if20 = module.AddField("IF20", "How much funding are you forecasting to spend in 2027-2028?", FieldType.Currency, 22, true, null, null, "0");
        if20.SetSection(s4.Id);
        if20.SetConfiguration("{\"prefix\":\"£\"}");

        var if21 = module.AddField("IF21", "How much funding are you forecasting to spend in 2028-2029?", FieldType.Currency, 23, true, null, null, "0");
        if21.SetSection(s4.Id);
        if21.SetConfiguration("{\"prefix\":\"£\"}");

        var if22 = module.AddField("IF22", "Briefly outline what activity changed, why and when.", FieldType.Textarea, 24, false);
        if22.SetSection(s5.Id);

        var if23 = module.AddField("IF23", "Provide a narrative of how this activity is progressing.", FieldType.Textarea, 25, false, null, "Optional. No PII.");
        if23.SetSection(s5.Id);
    }

    // ─── Infant Feeding strand level risk (TF2 only) ─────────────────────────

    private static DataCollectionFormModule CreateInfantFeedingRiskModule(UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.InfantFeedingRisk,
            9,
            "Infant Feeding strand level risk",
            "Strand-level risk for Infant Feeding (TF2 LAs only). Only needs to be completed once per strand.",
            DateTime.UtcNow,
            type: DataCollectionType.DeliveryPlan);

        PopulateInfantFeedingRiskFields(module);

        if (systemUserId != UserId.Empty())
        {
            module.Publish(systemUserId);
        }

        return module;
    }

    private static void PopulateInfantFeedingRiskFields(DataCollectionFormModule module)
    {
        var s1 = module.AddSection(1, "Strand Risk", "Risks to successful delivery of this strand.");

        var if24 = module.AddField("IF24", "What are the risks to successful delivery of this strand?", FieldType.Checkbox, 1, true, null, "Only needs to be completed once per strand.");
        if24.SetSection(s1.Id);
        if24.AddOption("recruitment_delays", "Recruitment delays", 1);
        if24.AddOption("procurement_delays", "Delays to procurement or start of contracts", 2);
        if24.AddOption("s114_financial", "S114 notice or financial challenges", 3);
        if24.AddOption("spend_delay_rollover", "Previous spend delay or rollover", 4);
        if24.AddOption("wider_challenges", "Wider challenges/issues within LA", 5);
        if24.AddOption("other", "Other", 6);
        if24.AddOption("none_na", "None / NA", 7);
    }

    // ─── Healthy Babies Offer (TF2 only) ─────────────────────────────────────

    private static DataCollectionFormModule CreateHealthyBabiesOfferModule(UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.HealthyBabiesOffer,
            10,
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
        // Activity ID has no section of its own — a single read-only auto-generated field isn't worth
        // its own wizard step, so it's the first field of Activity Details instead.
        var s2 = module.AddSection(1, "Activity Details", "Activity type, expectations and status.");
        var s3 = module.AddSection(2, "Dates and Status", "Planned and actual activity dates.");
        var s4 = module.AddSection(3, "Funding", "Financial forecasts and actuals.");
        var s5 = module.AddSection(4, "Progress", "Activity progress.");

        var hbo01 = module.AddField("HBO01", "Activity ID", FieldType.ReadOnly, 1, false, null, "Assigned automatically as a unique reference for this activity.");
        hbo01.SetSection(s2.Id);

        var hbo02 = module.AddField("HBO02", "Provide the name and a brief description of the activity.", FieldType.Textarea, 2, true);
        hbo02.SetSection(s2.Id);

        var hbo03 = module.AddField("HBO03", "Select the activity type", FieldType.Select, 3, true);
        hbo03.SetSection(s2.Id);
        hbo03.AddOption("training", "Training", 1);
        hbo03.AddOption("service_delivery", "Service Delivery", 2);
        hbo03.AddOption("staff", "Staff", 3);
        hbo03.AddOption("other", "Other", 4);

        var hbo04 = module.AddField("HBO04", "Which expectations does this activity meet?", FieldType.Checkbox, 4, true, null, "Multi-select.");
        hbo04.SetSection(s2.Id);
        hbo04.AddOption("entitlements", "Families understand entitlements", 1);
        hbo04.AddOption("co_design", "Co-design integrated pathways", 2);
        hbo04.AddOption("involve_parents", "Involve parents/carers in development", 3);
        hbo04.AddOption("accessible_online", "Accessible online space", 4);
        hbo04.AddOption("hard_copy", "Hard-copy version prior to birth", 5);

        var hbo05 = module.AddField("HBO05", "Is this a new or continuing activity?", FieldType.Radio, 5, true);
        hbo05.SetSection(s2.Id);
        hbo05.SetConfiguration("{\"size\":\"small\"}");
        hbo05.AddOption("new", "New", 1);
        hbo05.AddOption("continuing", "Continuing", 2);

        var hbo05_1 = module.AddField("HBO05.1", "Is this activity funding service(s)?", FieldType.Radio, 6, true);
        hbo05_1.SetSection(s2.Id);
        hbo05_1.SetConfiguration("{\"size\":\"small\"}");
        hbo05_1.AddOption("yes", "Yes", 1);
        hbo05_1.AddOption("no", "No", 2);

        var hbo05_2 = module.AddField("HBO05.2", "Which service is the activity funding?", FieldType.Checkbox, 7, true, null, "Select every service this activity funds.");
        hbo05_2.SetSection(s2.Id);
        hbo05_2.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"HBO05.1\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        hbo05_2.SetConfiguration("{\"dynamicOptions\":\"services\"}");

        var hbo06 = module.AddField("HBO06", "Provide the planned start date for this activity", FieldType.Date, 8, true, "DD/MM/YYYY");
        hbo06.SetSection(s3.Id);

        var hbo07 = module.AddField("HBO07", "Provide the actual date from which this activity commenced", FieldType.Date, 9, false, "DD/MM/YYYY");
        hbo07.SetSection(s3.Id);
        hbo07.SetConfiguration("{\"dateRule\":\"not_in_future\"}");

        var hbo08 = module.AddField("HBO08", "Provide the date you expect this activity to be completed", FieldType.Date, 10, true, "DD/MM/YYYY", "Cannot be a date before HBO06.");
        hbo08.SetSection(s3.Id);
        hbo08.SetConfiguration("{\"notBefore\":\"HBO06\"}");

        var hbo09 = module.AddField("HBO09", "Provide the date this activity was completed", FieldType.Date, 11, false, "DD/MM/YYYY", "Cannot be in the future.");
        hbo09.SetSection(s3.Id);
        hbo09.SetConfiguration("{\"dateRule\":\"not_in_future\"}");

        var hbo10 = module.AddField("HBO10", "Select the activity status", FieldType.Select, 12, true);
        hbo10.SetSection(s3.Id);
        hbo10.AddOption("started", "Started", 1);
        hbo10.AddOption("not_started", "Not started", 2);
        hbo10.AddOption("delayed", "Delayed", 3);
        hbo10.AddOption("completed", "Completed", 4);

        var hbo11 = module.AddField("HBO11", "Select the delivery status of this activity", FieldType.Select, 13, true);
        hbo11.SetSection(s3.Id);
        hbo11.AddOption("on_track", "On track", 1);
        hbo11.AddOption("off_track", "Off track", 2);

        var hbo12 = module.AddField("HBO12", "Is this activity receiving any additional funding from other sources?", FieldType.Radio, 14, true);
        hbo12.SetSection(s4.Id);
        hbo12.SetConfiguration("{\"size\":\"small\"}");
        hbo12.AddOption("yes", "Yes", 1);
        hbo12.AddOption("no", "No", 2);

        var hbo13 = module.AddField("HBO13", "Total amount of additional funding", FieldType.Currency, 15, true, null, "Required if additional funding sources are present.", "0");
        hbo13.SetSection(s4.Id);
        hbo13.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"HBO12\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        hbo13.SetConfiguration("{\"group\":\"HBO12_extra\",\"groupLabel\":\"Additional funding details\",\"prefix\":\"£\"}");

        var hbo14 = module.AddField("HBO14", "Source of additional funding", FieldType.Text, 16, true, null, "Required if additional funding sources are present.");
        hbo14.SetSection(s4.Id);
        hbo14.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"HBO12\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        hbo14.SetConfiguration("{\"group\":\"HBO12_extra\"}");

        var hbo15 = module.AddField("HBO15", "How much Healthy Babies Offer funding are you forecasting to spend in 2026-27?", FieldType.Currency, 17, true, null, null, "0");
        hbo15.SetSection(s4.Id);
        hbo15.SetConfiguration("{\"prefix\":\"£\"}");

        var hbo16 = module.AddField("HBO16", "Total Activity Budget (26/27)", FieldType.Calculation, 18, false, null, "HBO13 + HBO15");
        hbo16.SetSection(s4.Id);
        hbo16.SetConfiguration("{\"formula\":\"HBO13 + HBO15\",\"prefix\":\"£\"}");

        var hbo17 = module.AddField("HBO17", "What is the total amount of actual spend on this activity to date?", FieldType.Currency, 19, true, null, null, "0");
        hbo17.SetSection(s4.Id);
        hbo17.SetConfiguration("{\"prefix\":\"£\"}");

        var hbo18 = module.AddField("HBO18", "What is the amount of funding committed to this activity?", FieldType.Currency, 20, false, null, null, "0");
        hbo18.SetSection(s4.Id);
        hbo18.SetConfiguration("{\"prefix\":\"£\"}");

        var hbo19 = module.AddField("HBO19", "Planned Spend (Uncommitted)", FieldType.Calculation, 21, false, null, "HBO15 - (HBO17 + HBO18). Flags overspend if negative.");
        hbo19.SetSection(s4.Id);
        hbo19.SetConfiguration("{\"formula\":\"HBO15 - (HBO17 + HBO18)\",\"prefix\":\"£\"}");

        var hbo20 = module.AddField("HBO20", "How much funding are you forecasting to spend in 2027-2028?", FieldType.Currency, 22, true, null, null, "0");
        hbo20.SetSection(s4.Id);
        hbo20.SetConfiguration("{\"prefix\":\"£\"}");

        var hbo21 = module.AddField("HBO21", "How much funding are you forecasting to spend in 2028-2029?", FieldType.Currency, 23, true, null, null, "0");
        hbo21.SetSection(s4.Id);
        hbo21.SetConfiguration("{\"prefix\":\"£\"}");

        var hbo22 = module.AddField("HBO22", "Briefly outline what activity changed, why and when.", FieldType.Textarea, 24, false);
        hbo22.SetSection(s5.Id);

        var hbo23 = module.AddField("HBO23", "Provide a narrative of how this activity is progressing.", FieldType.Textarea, 25, false, null, "Optional. No PII.");
        hbo23.SetSection(s5.Id);
    }

    // ─── Healthy Babies Offer strand level risk (TF2 only) ───────────────────

    private static DataCollectionFormModule CreateHealthyBabiesOfferRiskModule(UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.HealthyBabiesOfferRisk,
            11,
            "Healthy Babies Offer strand level risk",
            "Strand-level risk for Healthy Babies Offer (TF2 LAs only). Only needs to be completed once per strand.",
            DateTime.UtcNow,
            type: DataCollectionType.DeliveryPlan);

        PopulateHealthyBabiesOfferRiskFields(module);

        if (systemUserId != UserId.Empty())
        {
            module.Publish(systemUserId);
        }

        return module;
    }

    private static void PopulateHealthyBabiesOfferRiskFields(DataCollectionFormModule module)
    {
        var s1 = module.AddSection(1, "Strand Risk", "Risks to successful delivery of this strand.");

        var hbo24 = module.AddField("HBO24", "What are the risks to successful delivery of this strand?", FieldType.Checkbox, 1, true, null, "Only needs to be completed once per strand.");
        hbo24.SetSection(s1.Id);
        hbo24.AddOption("recruitment_delays", "Recruitment delays", 1);
        hbo24.AddOption("procurement_delays", "Delays to procurement or start of contracts", 2);
        hbo24.AddOption("s114_financial", "S114 notice or financial challenges", 3);
        hbo24.AddOption("spend_delay_rollover", "Previous spend delay or rollover", 4);
        hbo24.AddOption("wider_challenges", "Wider challenges/issues within LA", 5);
        hbo24.AddOption("other", "Other", 6);
        hbo24.AddOption("none_na", "None / NA", 7);
    }

    // ─── Rollover Spend ───────────────────────────────────────────────────────

    private static DataCollectionFormModule CreateRolloverSpendModule(UserId systemUserId)
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            DataCollectionFormModuleCodes.RolloverSpend,
            12,
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

        var ro03_1 = module.AddField("RO03.1", "Is this activity funding service(s)?", FieldType.Radio, 4, true);
        ro03_1.SetSection(s1.Id);
        ro03_1.SetConfiguration("{\"size\":\"small\"}");
        ro03_1.AddOption("yes", "Yes", 1);
        ro03_1.AddOption("no", "No", 2);

        var ro03_2 = module.AddField("RO03.2", "Which service is the activity funding?", FieldType.Checkbox, 5, true, null, "Select every service this activity funds.");
        ro03_2.SetSection(s1.Id);
        ro03_2.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"RO03.1\",\"equals\":\"yes\"},\"displayInline\":true,\"parentOption\":\"yes\"}");
        ro03_2.SetConfiguration("{\"dynamicOptions\":\"services\"}");

        var ro04 = module.AddField("RO04", "Provide the planned start date for this activity", FieldType.Date, 6, true, "DD/MM/YYYY");
        ro04.SetSection(s1.Id);

        var ro05 = module.AddField("RO05", "Provide the actual date from which this activity commenced", FieldType.Date, 7, false, "DD/MM/YYYY");
        ro05.SetSection(s1.Id);
        ro05.SetConfiguration("{\"dateRule\":\"not_in_future\"}");

        var ro06 = module.AddField("RO06", "Provide the date you expect this activity to be completed", FieldType.Date, 8, false, "DD/MM/YYYY");
        ro06.SetSection(s1.Id);
        ro06.SetConfiguration("{\"notBefore\":\"RO04\"}");

        var ro07 = module.AddField("RO07", "Provide the date this activity was completed", FieldType.Date, 9, false, "DD/MM/YYYY");
        ro07.SetSection(s1.Id);
        ro07.SetConfiguration("{\"dateRule\":\"not_in_future\"}");

        var ro08 = module.AddField("RO08", "What is the status of this activity?", FieldType.Select, 10, true);
        ro08.SetSection(s1.Id);
        ro08.AddOption("on_track", "On track", 1);
        ro08.AddOption("off_track", "Off track", 2);

        var ro09 = module.AddField("RO09", "Select the year of this rollover", FieldType.Select, 11, true);
        ro09.SetSection(s2.Id);
        ro09.AddOption("2025_26", "2025-26", 1);

        var ro10 = module.AddField("RO10", "Provide the total amount of 2025-26 funding planned for this activity", FieldType.Currency, 12, true, null, null, "0");
        ro10.SetSection(s2.Id);
        ro10.SetConfiguration("{\"prefix\":\"£\"}");

        var ro11 = module.AddField("RO11", "Provide the total amount of actual spend on this activity to date", FieldType.Currency, 13, true, null, null, "0");
        ro11.SetSection(s2.Id);
        ro11.SetConfiguration("{\"prefix\":\"£\",\"maxSumField\":\"RO10\"}");

        var ro12 = module.AddField("RO12", "Provide the amount of funding that is committed to this activity", FieldType.Currency, 14, false, null, null, "0");
        ro12.SetSection(s2.Id);
        ro12.SetConfiguration("{\"prefix\":\"£\"}");

        var ro13 = module.AddField("RO13", "2025-26 Planned Spend", FieldType.Calculation, 15, false, null, "RO10 - (RO11 + RO12). Flags overspend if negative.");
        ro13.SetSection(s2.Id);
        ro13.SetConfiguration("{\"formula\":\"RO10 - (RO11 + RO12)\",\"prefix\":\"£\",\"warnIfNegative\":true}");

        var ro14 = module.AddField("RO14", "Provide a narrative of how this activity is progressing", FieldType.Textarea, 16, false);
        ro14.SetSection(s2.Id);
    }
}