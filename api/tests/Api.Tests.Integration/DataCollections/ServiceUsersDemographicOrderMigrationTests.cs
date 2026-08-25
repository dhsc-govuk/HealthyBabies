using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using FluentAssertions;
using Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Tests.Common;
using Xunit;

namespace Api.Tests.Integration.DataCollections;

/// <summary>
/// BLUM-7222. The Service Users summary screens flatten the module into one list, so the
/// demographic breakdowns only read correctly while display_order happens to keep each one
/// contiguous and behind its gate question. Production drifted out of that state: a sex "Unknown"
/// renders between IMD deciles 9 and 10, an age band renders inside the sex breakdown, and the age
/// boxes 20260708112413 inserted at 31-37 are interleaved with the sex boxes already at 28-32.
///
/// These tests run the real SQL from 20260814092303_NormaliseServiceUsersDemographicGrouping
/// against a live database - the operations are pulled off the migration itself rather than copied
/// - and assert both halves of the repair: the legacy duplicates are retired, and what is left is
/// renumbered gate by gate and group by group, with every saved answer untouched.
/// </summary>
public class ServiceUsersDemographicOrderMigrationTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    private static readonly string[] RetiredFieldKeys =
    [
        "QSU12_UNKNOWN", "QSU13_UNKNOWN", "QSU13_11",
        "QSU13_02", "QSU13_unknown",
    ];

    private DataCollectionFormModuleId _moduleId = null!;
    private FormSubmissionId _submissionId = null!;

    [Fact]
    public async Task GroupsEachBreakdownUnderItsGate_AndUninterleavesTheSexAndAgeBoxes()
    {
        await SeedServiceUsersModule();

        // Guard against a vacuous pass: the seeded module really does reproduce BLUM-7222, with the
        // sex "Unknown" wedged between two IMD deciles and the age boxes cutting through the sex block.
        (await FieldKeysInReviewScreenOrder())
            .Should().ContainInOrder("QSU10_decile9", "QSU12_UNKNOWN", "QSU10_decile10")
            .And.ContainInOrder("QSU12_prefer_not_to_say", "QSU13B_0_2", "QSU12_unknown");

        await RunMigrationAsync();

        string[] expected =
        [
            "QSU01", "QSU02",                                                       // section 1
            "QSU03",                                                                // section 2
            "QSU07", "QSU08_white", "QSU08_unknown",                                // section 3, ethnicity
            "QSU09", "QSU10_decile1", "QSU10_decile9", "QSU10_decile10", "QSU10_unknown",
            "QSU11", "QSU12_male", "QSU12_female", "QSU12_other",                   // sex, one block
            "QSU12_prefer_not_to_say", "QSU12_unknown",
            "QSU13A", "QSU13B_0_2", "QSU13B_3_4", "QSU13B_unknown",                 // age, one block
            "QSU13", "QSU14", "QSU15", "QSU16",
            "QSU18", "QSU19", "QSU20_low", "QSU20_mid", "QSU20_high", "QSU17",      // section 4
        ];

        (await FieldKeysInReviewScreenOrder()).Should().Equal(expected);
    }

    [Fact]
    public async Task RetiresTheLegacyDuplicates_ButKeepsTheAgeGateAndTheOutcomeScoresQuestion()
    {
        await SeedServiceUsersModule();

        await RunMigrationAsync();

        var inactive = await ModuleFields().Where(f => !f.IsActive).Select(f => f.FieldKey).ToListAsync();
        inactive.Should().BeEquivalentTo(RetiredFieldKeys);

        // QSU13 (the outcome-scores radio) and QSU13A (the age gate) share the stem with the retired
        // keys. A prefix match would take the age question away entirely.
        var active = await ModuleFields().Where(f => f.IsActive).Select(f => f.FieldKey).ToListAsync();
        active.Should().Contain(["QSU13", "QSU13A", "QSU12_unknown", "QSU13B_unknown"]);
    }

    [Fact]
    public async Task KeepsTheLegacyAgeSet_WhenItsReplacementIsMissing()
    {
        // A database that never received 20260716134254 has no QSU13B_* to fall back on. Retiring
        // QSU13_* there would leave the age gate with nothing behind it.
        await SeedServiceUsersModule();
        await DeleteTheQsu13bReplacementBoxes();

        await RunMigrationAsync();

        var active = await ModuleFields().Where(f => f.IsActive).Select(f => f.FieldKey).ToListAsync();
        active.Should().Contain(["QSU13_02", "QSU13_unknown"]);

        // The uppercase twin and the retired banding still go - they were never part of a coherent
        // set, and QSU13_unknown remains as the "Unknown" box.
        active.Should().NotContain(["QSU13_UNKNOWN", "QSU13_11"]);

        (await FieldKeysInReviewScreenOrder()).Should().ContainInOrder("QSU13A", "QSU13_02", "QSU13_unknown", "QSU13");
    }

    [Fact]
    public async Task KeepsTheEbiBlockBeforeClarificationComments()
    {
        // BLUM-7219 regression. Renumbering the whole module must not undo the within-section rank
        // that put the EBI questions together at the end, ahead of the clarification comments.
        await SeedServiceUsersModule();

        await RunMigrationAsync();

        (await FieldKeysInReviewScreenOrder())
            .Should().EndWith(["QSU18", "QSU19", "QSU20_low", "QSU20_mid", "QSU20_high", "QSU17"]);
    }

    [Fact]
    public async Task LeavesEverySavedAnswerIntact_AndIsANoOpOnASecondRun()
    {
        await SeedServiceUsersModule();
        var answersBefore = await SubmittedAnswers().CountAsync();

        await RunMigrationAsync();
        var orderAfterFirstRun = await FieldKeysInReviewScreenOrder();
        var retiredAfterFirstRun = await ModuleFields().Where(f => !f.IsActive).Select(f => f.FieldKey).ToListAsync();

        await RunMigrationAsync();

        (await FieldKeysInReviewScreenOrder()).Should().Equal(orderAfterFirstRun);
        (await ModuleFields().Where(f => !f.IsActive).Select(f => f.FieldKey).ToListAsync())
            .Should().BeEquivalentTo(retiredAfterFirstRun);
        (await SubmittedAnswers().CountAsync()).Should().Be(answersBefore);

        // Retiring a question is is_active = false, never a DELETE, so the number the LA submitted
        // against QSU13_02 is still in the database and still points at the question it was given.
        var legacyAgeFieldId = await ModuleFields().Where(f => f.FieldKey == "QSU13_02").Select(f => f.Id).SingleAsync();
        var answeredFieldIds = await SubmittedAnswers().Select(v => v.FormFieldId).ToListAsync();
        answeredFieldIds.Should().Contain(legacyAgeFieldId);
    }

    [Fact]
    public async Task ProducesADenseSequenceWithNoDuplicates()
    {
        await SeedServiceUsersModule();

        await RunMigrationAsync();

        // Inactive fields are renumbered along with the rest: leaving them out would punch holes in
        // the sequence and hand the next inserted question a number that collides with a live one.
        var orders = await ModuleFields().OrderBy(f => f.DisplayOrder).Select(f => f.DisplayOrder).ToListAsync();

        orders.Should().Equal(Enumerable.Range(1, orders.Count));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await ClearAllTablesAsync();

    /// <summary>
    /// Executes the migration's own SQL, so the tests cannot drift away from what ships.
    /// </summary>
    private async Task RunMigrationAsync()
    {
        var migration = (Migration)new NormaliseServiceUsersDemographicGrouping();

        foreach (var operation in migration.UpOperations.OfType<SqlOperation>())
        {
            await Context.Database.ExecuteSqlRawAsync(operation.Sql);
        }

        Context.ChangeTracker.Clear();
    }

    private async Task DeleteTheQsu13bReplacementBoxes()
    {
        await Context.Database.ExecuteSqlRawAsync("DELETE FROM form_fields WHERE field_key LIKE 'QSU13B%';");
        Context.ChangeTracker.Clear();
    }

    // The app seeds other modules at start-up, so every assertion is scoped to the module under test.
    private IQueryable<FormField> ModuleFields() =>
        Context.Set<FormField>().AsNoTracking().Where(f => f.FormModuleId == _moduleId);

    private IQueryable<FormFieldValue> SubmittedAnswers() =>
        Context.Set<FormFieldValue>().AsNoTracking().Where(v => v.FormSubmissionId == _submissionId);

    /// <summary>
    /// What the summary screens render: the active fields, flattened into one list. Ordering by
    /// display_order alone matches the API's section-then-order sequence once the numbers are
    /// normalised, and reproduces the jumbled order before that.
    /// </summary>
    private async Task<List<string>> FieldKeysInReviewScreenOrder() =>
        await ModuleFields()
            .Where(f => f.IsActive)
            .OrderBy(f => f.DisplayOrder)
            .ThenBy(f => f.FieldKey)
            .Select(f => f.FieldKey)
            .ToListAsync();

    /// <summary>
    /// Reproduces the live section-3 shape: the sex boxes at 28-32, the age boxes 20260708112413
    /// inserted at 31-37 cutting through them, the superseded QSU13_* age set at 34-39, and the
    /// three ungrouped legacy fields tied to whichever question happens to share their number.
    /// </summary>
    private async Task SeedServiceUsersModule()
    {
        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            code: "service-users",
            sectionNumber: 2,
            name: "Service users");

        var delivery = module.AddSection(1, "Service delivery");
        var numbers = module.AddSection(2, "User numbers");
        var demographics = module.AddSection(3, "User demographics and data collection");
        var clarification = module.AddSection(4, "Clarification comments");

        const string GatedOnUserNumbers = """{"showWhen":{"allOf":[{"fieldKey":"QSU01","equals":"yes"},{"fieldKey":"QSU02","equals":"yes"}]}}""";

        // The demographic gates trail a serviceFieldKey condition, which describes the Service record
        // rather than a question in this module - the migration has to skip it and fall back to QSU03.
        const string GatedOnUserCountAndFunding =
            """{"showWhen":{"allOf":[{"fieldKey":"QSU03","gte":"1"},{"serviceFieldKey":"SMD02","in":["0","1"]}]}}""";

        static string InlineChildOf(string gate) =>
            $$"""{"showWhen":{"fieldKey":"{{gate}}","equals":"yes"},"displayInline":true,"parentOption":"yes"}""";

        static string ChildOf(string gate) =>
            $$"""{"showWhen":{"fieldKey":"{{gate}}","equals":"yes"},"parentOption":"yes"}""";

        static string InGroup(string group) => $$"""{"group":"{{group}}","min":0}""";

        AddField(module, delivery.Id, "QSU01", "Was the service delivered for users this quarter?", FieldType.Radio, 1);
        AddField(module, delivery.Id, "QSU02", "Do you have data on how many people use this service?", FieldType.Radio, 2, InlineChildOf("QSU01"));
        AddField(module, numbers.Id, "QSU03", "Overall, how many service users have used this service?", FieldType.Number, 3, GatedOnUserNumbers);

        // Ethnicity is already correct and has to stay correct.
        AddField(module, demographics.Id, "QSU07", "Do you have data broken down by ethnicity?", FieldType.Radio, 7, GatedOnUserCountAndFunding);
        AddField(module, demographics.Id, "QSU08_white", "White British", FieldType.Number, 8, InlineChildOf("QSU07"), InGroup("QSU08"));
        AddField(module, demographics.Id, "QSU08_unknown", "Unknown", FieldType.Number, 14, InlineChildOf("QSU07"), InGroup("QSU08"));

        // The sex "Unknown" twin ties with decile 9, so it renders between deciles 9 and 10.
        AddField(module, demographics.Id, "QSU09", "Do you have data broken down by IMD?", FieldType.Radio, 15, GatedOnUserCountAndFunding);
        AddField(module, demographics.Id, "QSU10_decile1", "IDACI Decile 1", FieldType.Number, 16, InlineChildOf("QSU09"), InGroup("QSU10"));
        AddField(module, demographics.Id, "QSU10_decile9", "IDACI Decile 9", FieldType.Number, 24, InlineChildOf("QSU09"), InGroup("QSU10"));
        AddField(module, demographics.Id, "QSU12_UNKNOWN", "Unknown", FieldType.Number, 24, InlineChildOf("QSU11"));
        AddField(module, demographics.Id, "QSU10_decile10", "IDACI Decile 10", FieldType.Number, 25, InlineChildOf("QSU09"), InGroup("QSU10"));
        AddField(module, demographics.Id, "QSU10_unknown", "Unknown", FieldType.Number, 26, InlineChildOf("QSU09"), InGroup("QSU10"));

        AddField(module, demographics.Id, "QSU11", "Do you have data broken down by sex?", FieldType.Radio, 27, GatedOnUserCountAndFunding);
        AddField(module, demographics.Id, "QSU12_male", "Male", FieldType.Number, 28, InlineChildOf("QSU11"), InGroup("QSU12"));
        AddField(module, demographics.Id, "QSU12_female", "Female", FieldType.Number, 29, InlineChildOf("QSU11"), InGroup("QSU12"));
        AddField(module, demographics.Id, "QSU13_11", "12 - 17 years", FieldType.Number, 29, ChildOf("QSU13A"));
        AddField(module, demographics.Id, "QSU12_other", "Other", FieldType.Number, 30, InlineChildOf("QSU11"), InGroup("QSU12"));
        AddField(module, demographics.Id, "QSU12_prefer_not_to_say", "Prefer not to say", FieldType.Number, 31, InlineChildOf("QSU11"), InGroup("QSU12"));
        AddField(module, demographics.Id, "QSU13B_0_2", "0-2", FieldType.Number, 31, InlineChildOf("QSU13A"), InGroup("QSU13B"));
        AddField(module, demographics.Id, "QSU12_unknown", "Unknown", FieldType.Number, 32, InlineChildOf("QSU11"), InGroup("QSU12"));
        AddField(module, demographics.Id, "QSU13_UNKNOWN", "Unknown", FieldType.Number, 32, InlineChildOf("QSU13A"));
        AddField(module, demographics.Id, "QSU13B_3_4", "3-4", FieldType.Number, 32, InlineChildOf("QSU13A"), InGroup("QSU13B"));
        AddField(module, demographics.Id, "QSU13A", "Do you have data broken down by age group?", FieldType.Radio, 33, GatedOnUserCountAndFunding);
        var legacyAge = AddField(module, demographics.Id, "QSU13_02", "0-2 years", FieldType.Number, 34, ChildOf("QSU13A"), InGroup("QSU13"));
        AddField(module, demographics.Id, "QSU13B_unknown", "Unknown", FieldType.Number, 37, InlineChildOf("QSU13A"), InGroup("QSU13B"));
        AddField(module, demographics.Id, "QSU13_unknown", "Unknown", FieldType.Number, 39, ChildOf("QSU13A"), InGroup("QSU13"));

        AddField(module, demographics.Id, "QSU13", "Did the service provider collect pre and post outcome scores?", FieldType.Radio, 40, GatedOnUserNumbers);
        AddField(module, demographics.Id, "QSU14", "How many users completed a pre- and post- outcome score?", FieldType.Number, 41, InlineChildOf("QSU13"));
        AddField(module, demographics.Id, "QSU15", "Do you have data on waiting times for this service?", FieldType.Radio, 42, GatedOnUserNumbers);
        AddField(module, demographics.Id, "QSU16", "What was the average waiting time from referral to first appointment?", FieldType.Number, 43, InlineChildOf("QSU15"));

        // The EBI block sits in the clarification section, as the admin API left it (BLUM-7219).
        var qsu17 = AddField(module, clarification.Id, "QSU17", "Provide more information (optional)", FieldType.Textarea, 44);
        var qsu18 = AddField(module, clarification.Id, "QSU18", "Is the service categorised as an Evidence Based Intervention?", FieldType.Radio, 45);
        var qsu19 = AddField(module, clarification.Id, "QSU19", "Do you have data on the total number of sessions attended?", FieldType.Radio, 46, InlineChildOf("QSU18"));
        var qsu20High = AddField(module, clarification.Id, "QSU20_high", "Attended more than 80% of sessions", FieldType.Number, 47, InlineChildOf("QSU19"), InGroup("QSU20"));
        var qsu20Low = AddField(module, clarification.Id, "QSU20_low", "Attended less than 50% of sessions", FieldType.Number, 47, InlineChildOf("QSU19"), InGroup("QSU20"));
        var qsu20Mid = AddField(module, clarification.Id, "QSU20_mid", "Attended 50% to 80% of sessions", FieldType.Number, 47, InlineChildOf("QSU19"), InGroup("QSU20"));

        Context.DataCollectionFormModules.Add(module);
        await SaveChangesAsync();
        _moduleId = module.Id;

        await GiveTheEbiAttendanceBoxesTheirCreationOrder();

        var submission = FormSubmission.Create(module.Id);
        _submissionId = submission.Id;
        submission.SetFieldValue(legacyAge, "4");
        submission.SetFieldValue(qsu17, "Some clarifying notes");
        submission.SetFieldValue(qsu18, "yes");
        submission.SetFieldValue(qsu19, "yes");
        submission.SetFieldValue(qsu20Low, "10");
        submission.SetFieldValue(qsu20Mid, "20");
        submission.SetFieldValue(qsu20High, "30");

        Context.Set<FormSubmission>().Add(submission);
        await SaveChangesAsync();
    }

    /// <summary>
    /// The three QSU20 boxes share a display_order, so created_at decides their order. Alphabetically
    /// they sort high, low, mid; they were created - and read - low, mid, high.
    /// </summary>
    private async Task GiveTheEbiAttendanceBoxesTheirCreationOrder()
    {
        await Context.Database.ExecuteSqlRawAsync(
            """
            UPDATE form_fields
            SET created_at = TIMESTAMPTZ '2026-01-01 00:00:00Z' + (
                CASE field_key WHEN 'QSU20_low' THEN 0 WHEN 'QSU20_mid' THEN 1 ELSE 2 END * INTERVAL '1 hour')
            WHERE field_key IN ('QSU20_low', 'QSU20_mid', 'QSU20_high');
            """);

        Context.ChangeTracker.Clear();
    }

    private static FormField AddField(
        DataCollectionFormModule module,
        FormSectionId? sectionId,
        string fieldKey,
        string label,
        FieldType fieldType,
        int displayOrder,
        string? conditionalRules = null,
        string? configuration = null)
    {
        var field = module.AddField(fieldKey, label, fieldType, displayOrder);
        field.SetSection(sectionId);
        field.SetConditionalRules(conditionalRules);
        field.SetConfiguration(configuration);
        return field;
    }
}