using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Organisations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.DataCollections;

/// <summary>
/// Covers the seeder bug from HANDOFF-BLUM-7429.md §3: once a delivery-plan module has submission
/// data, the old reconciler renamed field keys but never reconciled Configuration/ConditionalRules,
/// so a stale calculation formula survived indefinitely and every dependent field rendered £0.00.
///
/// Also covers BLUM-7564: a retired field that had been answered was retained active, so a question
/// the seeder had already stopped defining kept rendering, along with the section it sat in.
/// </summary>
public class DeliveryPlanFormModuleSeederTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    // No grouping constant exists for these, and DataCollectionFormModuleCodes mixes them in with the
    // management-information module codes, which this seeder does not own.
    private static readonly string[] DeliveryPlanModuleCodes =
    [
        DataCollectionFormModuleCodes.Focus,
        DataCollectionFormModuleCodes.DeliveryGrant,
        DataCollectionFormModuleCodes.Parenting,
        DataCollectionFormModuleCodes.Hle,
        DataCollectionFormModuleCodes.Send,
        DataCollectionFormModuleCodes.PnmhAndPir,
        DataCollectionFormModuleCodes.InfantFeeding,
        DataCollectionFormModuleCodes.HealthyBabiesOffer,
        DataCollectionFormModuleCodes.RolloverSpend,
        DataCollectionFormModuleCodes.PnmhAndPirRisk,
        DataCollectionFormModuleCodes.InfantFeedingRisk,
        DataCollectionFormModuleCodes.HealthyBabiesOfferRisk,
    ];

    public async Task InitializeAsync()
    {
        await ClearAllTablesAsync();
        await SeedApplicationDataAsync();
    }

    public async Task DisposeAsync()
    {
        await ClearAllTablesAsync();
    }

    [Fact]
    public async Task SeedAsync_WhenConfigurationDriftsFromDefinition_RestoresItOnAModuleWithSubmissionData()
    {
        var module = await Context.DataCollectionFormModules
            .Include(m => m.Fields)
            .SingleAsync(m => m.Code == DataCollectionFormModuleCodes.DeliveryGrant);

        var dg11 = module.Fields.Single(f => f.FieldKey == "DG11");
        var dg12 = module.Fields.Single(f => f.FieldKey == "DG12");
        var correctConfiguration = dg12.Configuration;

        // A module with submission data is what put the old reconciler on the buggy code path
        // instead of the destructive rebuild path, which would have recreated the field correctly.
        var activity = FormSubmission.Create(module.Id, entityType: "Activity", name: "Activity 1");
        activity.SetFieldValue(dg11, "100000");
        Context.FormSubmissions.Add(activity);

        // Simulate the historic bug: the rename landed correctly on FieldKey, but the calculation
        // formula referencing the pre-rename codes was never brought forward.
        dg12.SetConfiguration("{\"formula\":\"DG06a + DG07\",\"prefix\":\"£\"}");
        await SaveChangesAsync();

        await SeedApplicationDataAsync();

        var reloaded = await Context.FormFields.SingleAsync(f => f.Id == dg12.Id);
        reloaded.Configuration.Should().Be(correctConfiguration);
    }

    [Fact]
    public async Task SeedAsync_WhenNothingHasDrifted_LeavesExistingFieldIdsUntouched()
    {
        // The old rebuild path deleted and recreated every field/section on a fresh module, which
        // would churn ids on every redeploy until the first submission landed. The replacement upsert
        // must never do that, drifted or not.
        var before = await Context.FormFields
            .Where(f => f.FormModule!.Code == DataCollectionFormModuleCodes.DeliveryGrant)
            .OrderBy(f => f.FieldKey)
            .Select(f => f.Id)
            .ToListAsync();

        await SeedApplicationDataAsync();

        var after = await Context.FormFields
            .Where(f => f.FormModule!.Code == DataCollectionFormModuleCodes.DeliveryGrant)
            .OrderBy(f => f.FieldKey)
            .Select(f => f.Id)
            .ToListAsync();

        after.Should().Equal(before);
    }

    [Fact]
    public async Task SeedAsync_WhenARetiredFieldsOnlyAnswerBelongsToASoftDeletedSubmission_KeepsTheFieldButDeactivatesIt()
    {
        var module = await Context.DataCollectionFormModules
            .Include(m => m.Fields)
            .SingleAsync(m => m.Code == DataCollectionFormModuleCodes.DeliveryGrant);
        var anySectionId = module.Fields.First().FormSectionId;

        // Not part of any current populator, so the upsert treats it as retired on the next run.
        var shadow = DataCollectionFormModule.Create(module.Id, module.Code, module.SectionNumber, module.Name);
        var retiredField = shadow.AddField("DG_LEGACY_TEST", "A question the current schema no longer asks", FieldType.Text, 999);
        retiredField.SetSection(anySectionId);
        Context.FormFields.Add(retiredField);
        await SaveChangesAsync();

        // Deleted before the first save, in the same tracked instance: BaseIntegrationTest's
        // SaveChangesAsync clears the change tracker, so mutating a submission after an earlier save
        // on it wouldn't be tracked as a change to persist.
        var deletedBy = await Context.Users.FirstAsync();
        var submission = FormSubmission.Create(module.Id, entityType: "Activity", name: "Activity 1");
        submission.SetFieldValue(retiredField, "an old answer");
        submission.Delete(deletedBy.Id);
        Context.FormSubmissions.Add(submission);
        await SaveChangesAsync();

        // Confirms the premise: the value is real, but FormFieldValue's global query filter
        // (!FormSubmission.IsDeleted) hides it as soon as its parent submission is soft-deleted.
        (await Context.FormSubmissions.IgnoreQueryFilters().SingleAsync(s => s.Id == submission.Id)).IsDeleted.Should().BeTrue();
        (await Context.FormFieldValues.AnyAsync(v => v.FormFieldId == retiredField.Id)).Should().BeFalse();

        await SeedApplicationDataAsync();

        // The seeder probes with IgnoreQueryFilters(), so the hidden answer still counts as one: the
        // row is kept in case the submission is restored, but the question stops being asked.
        var reloaded = await Context.FormFields.SingleAsync(f => f.Id == retiredField.Id);
        reloaded.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SeedAsync_WhenASectionsContentDrifts_RestoresItsTitleAndDescription()
    {
        var module = await Context.DataCollectionFormModules
            .Include(m => m.Sections)
            .SingleAsync(m => m.Code == DataCollectionFormModuleCodes.DeliveryGrant);

        var section1 = module.Sections.Single(s => s.SectionNumber == 1);
        var correctTitle = section1.Title;
        var correctDescription = section1.Description;

        section1.UpdateDetails("Stale Title", "Stale description", null, null);
        await SaveChangesAsync();

        await SeedApplicationDataAsync();

        var reloaded = await Context.FormSections.SingleAsync(s => s.Id == section1.Id);
        reloaded.Title.Should().Be(correctTitle);
        reloaded.Description.Should().Be(correctDescription);
    }

    [Fact]
    public async Task SeedAsync_WhenASectionNumberIsNoLongerDefined_RemovesTheOrphanedSection()
    {
        // Regression: merging "Activity Information" (a standalone section holding only the
        // read-only Activity ID field) into "Activity Details" dropped a section number from every
        // activity module. The now-unreferenced section must not linger as an empty wizard step.
        var module = await Context.DataCollectionFormModules
            .SingleAsync(m => m.Code == DataCollectionFormModuleCodes.DeliveryGrant);

        var shadow = DataCollectionFormModule.Create(module.Id, module.Code, module.SectionNumber, module.Name);
        var orphanSection = shadow.AddSection(4, "No longer defined", null);
        Context.FormSections.Add(orphanSection);
        await SaveChangesAsync();

        await SeedApplicationDataAsync();

        var stillExists = await Context.FormSections.AnyAsync(s => s.Id == orphanSection.Id);
        stillExists.Should().BeFalse();
    }

    [Fact]
    public async Task SeedAsync_WhenARetiredFieldHasAnswers_DeactivatesItInsteadOfRemovingIt()
    {
        // BLUM-7564: BLUM-7369 moved the strand-level risk question into modules of its own, but the
        // required question left behind in each activity module had been answered on every completed
        // activity, so the "remove only if unanswered" branch retained it — active, and still on the
        // form. Retiring a question must stop it being asked without orphaning the old answers.
        var module = await Context.DataCollectionFormModules
            .Include(m => m.Fields)
            .SingleAsync(m => m.Code == DataCollectionFormModuleCodes.DeliveryGrant);
        var anySectionId = module.Fields.First().FormSectionId;

        var shadow = DataCollectionFormModule.Create(module.Id, module.Code, module.SectionNumber, module.Name);
        var retiredField = shadow.AddField("DG_LEGACY_TEST", "A question the current schema no longer asks", FieldType.Text, 999, isRequired: true);
        retiredField.SetSection(anySectionId);
        Context.FormFields.Add(retiredField);
        await SaveChangesAsync();

        var activity = FormSubmission.Create(module.Id, entityType: "Activity", name: "Activity 1");
        activity.SetFieldValue(retiredField, "an old answer");
        Context.FormSubmissions.Add(activity);
        await SaveChangesAsync();

        await SeedApplicationDataAsync();

        var reloaded = await Context.FormFields.SingleAsync(f => f.Id == retiredField.Id);
        reloaded.IsActive.Should().BeFalse();
        (await Context.FormFieldValues.AnyAsync(v => v.FormFieldId == retiredField.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task SeedAsync_WhenAnOrphanedSectionsOnlyFieldIsRetired_DeactivatesTheSection()
    {
        // Deactivating the field alone was not enough: the section it sat in is no longer defined
        // either, and the orphan-removal branch refuses to delete a section a field still points at.
        // Left active it renders as a final wizard step with a stale heading and nothing in it.
        var module = await Context.DataCollectionFormModules
            .SingleAsync(m => m.Code == DataCollectionFormModuleCodes.DeliveryGrant);

        var shadow = DataCollectionFormModule.Create(module.Id, module.Code, module.SectionNumber, module.Name);
        var orphanSection = shadow.AddSection(9, "Progress and Strand Risk", "A section the current schema no longer defines.");
        var retiredField = shadow.AddField("DG_LEGACY_TEST", "A question the current schema no longer asks", FieldType.Text, 999);
        retiredField.SetSection(orphanSection.Id);
        Context.FormSections.Add(orphanSection);
        Context.FormFields.Add(retiredField);
        await SaveChangesAsync();

        var activity = FormSubmission.Create(module.Id, entityType: "Activity", name: "Activity 1");
        activity.SetFieldValue(retiredField, "an old answer");
        Context.FormSubmissions.Add(activity);
        await SaveChangesAsync();

        await SeedApplicationDataAsync();

        var reloaded = await Context.FormSections.SingleAsync(s => s.Id == orphanSection.Id);
        reloaded.IsActive.Should().BeFalse("the retained field still points at it, and the FK is SetNull on delete");
    }

    [Fact]
    public async Task SeedAsync_WhenAFieldTheSeederStillDefinesWasDeactivated_LeavesItInactive()
    {
        // DeleteFormFieldCommand refuses to delete a field that has answers, so deactivating it
        // through UpdateFormFieldCommand is an admin's only way to retire an answered question. The
        // seeder reconciles every other property of these modules on each boot, but must not undo
        // that one — otherwise the question comes back on every restart.
        var dg11 = await Context.FormFields
            .SingleAsync(f => f.FieldKey == "DG11" && f.FormModule!.Code == DataCollectionFormModuleCodes.DeliveryGrant);
        dg11.Deactivate();
        await SaveChangesAsync();

        await SeedApplicationDataAsync();

        (await Context.FormFields.SingleAsync(f => f.Id == dg11.Id)).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task SeedAsync_WhenASectionTheSeederStillDefinesIsInactive_ReactivatesIt()
    {
        var section = await Context.FormSections
            .SingleAsync(s => s.SectionNumber == 1 && s.FormModule!.Code == DataCollectionFormModuleCodes.DeliveryGrant);
        section.Deactivate();
        await SaveChangesAsync();

        await SeedApplicationDataAsync();

        (await Context.FormSections.SingleAsync(s => s.Id == section.Id)).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SeedAsync_WhenADeliveryPlanHasAStrandButNotItsRiskSection_AssignsTheRiskSection()
    {
        // BLUM-7369 gave each of the three strands a standalone risk section, but a module only
        // reaches a submission through an assignment — so on a round created before those modules
        // existed the strand-level risk question had nowhere to appear.
        var deliveryPlan = await AddDeliveryPlanWithAsync(DataCollectionFormModuleCodes.PnmhAndPir);

        await SeedApplicationDataAsync();

        (await AssignedCodesAsync(deliveryPlan.Id)).Should().Contain(DataCollectionFormModuleCodes.PnmhAndPirRisk);
    }

    [Fact]
    public async Task SeedAsync_WhenADeliveryPlanDoesNotHaveTheStrand_DoesNotAssignItsRiskSection()
    {
        var deliveryPlan = await AddDeliveryPlanWithAsync(DataCollectionFormModuleCodes.DeliveryGrant);

        await SeedApplicationDataAsync();

        (await AssignedCodesAsync(deliveryPlan.Id)).Should().NotContain(DataCollectionFormModuleCodes.PnmhAndPirRisk);
    }

    [Fact]
    public async Task SeedAsync_WhenADeliveryPlansDeadlineHasPassed_DoesNotAssignTheRiskSection()
    {
        // IsClosed is only the explicit admin flag. What actually stops an LA completing a section
        // is the deadline, so a round that simply ran out of time must be skipped too.
        var deliveryPlan = await AddDeliveryPlanWithAsync(DataCollectionFormModuleCodes.PnmhAndPir, endDate: DateTime.UtcNow.AddDays(-1));

        await SeedApplicationDataAsync();

        (await AssignedCodesAsync(deliveryPlan.Id)).Should().NotContain(DataCollectionFormModuleCodes.PnmhAndPirRisk);
    }

    [Fact]
    public async Task SeedAsync_WhenALocalAuthorityHasAnExtensionBeyondAPassedDeadline_AssignsTheRiskSection()
    {
        // GetEffectiveEndDate prefers the LA's own end date, so one live extension keeps the round
        // completable even though the collection-level deadline has gone.
        var organisation = OrganisationsData.MainOrganisation;
        Context.Organisations.Add(organisation);
        await SaveChangesAsync();

        var deliveryPlan = await AddDeliveryPlanWithAsync(
            DataCollectionFormModuleCodes.PnmhAndPir,
            endDate: DateTime.UtcNow.AddDays(-1),
            extendedFor: (organisation.Id, DateTime.UtcNow.AddDays(30)));

        await SeedApplicationDataAsync();

        (await AssignedCodesAsync(deliveryPlan.Id)).Should().Contain(DataCollectionFormModuleCodes.PnmhAndPirRisk);
    }

    [Fact]
    public async Task SeedAsync_WhenADeliveryPlanIsClosed_DoesNotAssignTheRiskSection()
    {
        // A section nobody can still complete would sit at "Not started" forever on the DHSC review
        // of a finished round.
        var deliveryPlan = await AddDeliveryPlanWithAsync(DataCollectionFormModuleCodes.HealthyBabiesOffer, close: true);

        await SeedApplicationDataAsync();

        (await AssignedCodesAsync(deliveryPlan.Id)).Should().NotContain(DataCollectionFormModuleCodes.HealthyBabiesOfferRisk);
    }

    [Fact]
    public async Task SeedAsync_RunTwice_DoesNotDuplicateTheBackfilledRiskAssignment()
    {
        // data_collection_form_module_assignments is uniquely indexed on (data_collection_id,
        // form_module_id), so a non-idempotent backfill is a startup crash, not a duplicate row.
        var deliveryPlan = await AddDeliveryPlanWithAsync(DataCollectionFormModuleCodes.InfantFeeding);

        await SeedApplicationDataAsync();
        await SeedApplicationDataAsync();

        var riskModuleId = await ModuleIdAsync(DataCollectionFormModuleCodes.InfantFeedingRisk);
        (await Context.DataCollectionFormModuleAssignments
                .CountAsync(a => a.DataCollectionId == deliveryPlan.Id && a.FormModuleId == riskModuleId))
            .Should().Be(1);
    }

    // Everything is staged before the single save on purpose: BaseIntegrationTest's SaveChangesAsync
    // clears the change tracker, so mutating the aggregate afterwards would not be persisted.
    private async Task<DataCollection> AddDeliveryPlanWithAsync(
        string moduleCode,
        bool close = false,
        DateTime? endDate = null,
        (OrganisationId LocalAuthorityId, DateTime? EndDate)? extendedFor = null)
    {
        var dataCollection = DataCollection.New(
            DataCollectionId.New(),
            $"Delivery Plan {moduleCode}",
            null,
            DateTime.UtcNow.AddDays(-30),
            endDate ?? DateTime.UtcNow.AddDays(30),
            type: DataCollectionType.DeliveryPlan);

        dataCollection.AssignFormModule(await ModuleIdAsync(moduleCode));

        if (extendedFor is { } extension)
        {
            dataCollection.AssignLocalAuthority(extension.LocalAuthorityId);
            dataCollection.LocalAuthorities.Single().UpdateEndDate(extension.EndDate);
        }

        if (close)
        {
            dataCollection.Close();
        }

        Context.DataCollections.Add(dataCollection);
        await SaveChangesAsync();

        return dataCollection;
    }

    private async Task<DataCollectionFormModuleId> ModuleIdAsync(string moduleCode) =>
        (await Context.DataCollectionFormModules.SingleAsync(m => m.Code == moduleCode)).Id;

    private async Task<List<string>> AssignedCodesAsync(DataCollectionId dataCollectionId) =>
        await Context.DataCollectionFormModuleAssignments
            .Where(a => a.DataCollectionId == dataCollectionId)
            .Select(a => a.FormModule.Code)
            .ToListAsync();

    [Fact]
    public async Task SeedAsync_RunTwice_DoesNotDuplicateOptions()
    {
        // Regression: the upsert used to load module.Fields without their Options, so every option
        // looked "missing" on every run and got re-added — duplicating every select/radio/checkbox
        // option on every seeder run (i.e. every app start).
        var before = await Context.FormFieldOptions
            .Where(o => o.FormField!.FormModule!.Code == DataCollectionFormModuleCodes.DeliveryGrant)
            .CountAsync();

        await SeedApplicationDataAsync();
        await SeedApplicationDataAsync();

        var after = await Context.FormFieldOptions
            .Where(o => o.FormField!.FormModule!.Code == DataCollectionFormModuleCodes.DeliveryGrant)
            .CountAsync();

        after.Should().Be(before);
    }

    [Fact]
    public async Task SeedAsync_EveryFieldPromisingItIsRequiredWhenRevealed_IsSeededRequiredWithAConditionalRule()
    {
        // BLUM-7053: F02 declared its intent only in help text — "Required if 'Other' is selected
        // above." — while IsRequired stayed false, so choosing "Other" on F01 revealed a field the
        // form then let you leave blank on Continue, Save as draft and Confirm and save alike.
        //
        // "Conditionally required" is encoded as IsRequired plus a showWhen rule, not as help text:
        // every validation path filters to visible fields before testing IsRequired, so the flag only
        // bites once the controlling answer reveals the field. Asserted as an invariant rather than
        // against F02 alone because nine sibling fields were seeded the same way.
        var f02 = await Context.FormFields
            .SingleAsync(f => f.FieldKey == "F02" && f.FormModule!.Code == DataCollectionFormModuleCodes.Focus);

        f02.IsRequired.Should().BeTrue();

        // Stored as jsonb, so Postgres reorders the keys and respaces the text on round-trip —
        // assert what the rule points at, not the literal string the seeder wrote.
        f02.ConditionalRules.Should().NotBeNull().And.Contain("F01");

        var offenders = await Context.FormFields
            .Where(f => f.IsActive
                && DeliveryPlanModuleCodes.Contains(f.FormModule!.Code)
                && f.HelpText != null
                && f.HelpText.StartsWith("Required if")
                && (!f.IsRequired || f.ConditionalRules == null))
            .Select(f => f.FieldKey)
            .ToListAsync();

        offenders.Should().BeEmpty("a field whose help text promises \"Required if ...\" needs IsRequired and a conditional rule to back the promise");
    }

    [Fact]
    public async Task SeedAsync_WhenAnAlreadyAnsweredFieldIsNotRequiredInTheDatabase_FlipsItToRequired()
    {
        // BLUM-7053 ships as a seed-data change, so the question that decides whether it reaches MVP
        // is whether the reconciler touches modules whose questions already exist and have answers —
        // not just a fresh database. Fields are matched by FieldKey and IsRequired is compared on
        // every start, so it does. Pinned down here rather than trusted.
        var module = await Context.DataCollectionFormModules
            .Include(m => m.Fields)
            .SingleAsync(m => m.Code == DataCollectionFormModuleCodes.Focus);

        var f01 = module.Fields.Single(f => f.FieldKey == "F01");
        var f02 = module.Fields.Single(f => f.FieldKey == "F02");

        // Reproduce the deployed state: F01 answered "other" so F02 is revealed, and F02 still
        // carrying the pre-fix IsRequired=false. Single-record modules like Focus create their
        // submission with no entityType, matching SubmissionsControllerService.SaveFormModule.
        f02.UpdateDetails(f02.Label, f02.DisplayOrder, false, f02.Placeholder, f02.HelpText, f02.DefaultValue);
        var submission = FormSubmission.Create(module.Id);
        submission.SetFieldValue(f01, "other");
        Context.FormSubmissions.Add(submission);
        await SaveChangesAsync();

        (await Context.FormFields.SingleAsync(f => f.Id == f02.Id)).IsRequired
            .Should().BeFalse("the test needs to start from the pre-fix state to mean anything");

        await SeedApplicationDataAsync();

        (await Context.FormFields.SingleAsync(f => f.Id == f02.Id)).IsRequired.Should().BeTrue();
    }
}