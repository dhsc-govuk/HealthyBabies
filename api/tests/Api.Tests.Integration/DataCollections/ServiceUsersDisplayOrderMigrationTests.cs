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
/// BLUM-7219. The Service Users review screen flattens the module into one list, so it only reads
/// correctly while form_fields.display_order agrees with section order. Production drifted out of
/// that state and the EBI questions (QSU18-20) ended up scattered through the list instead of
/// sitting together before the clarification comments.
///
/// These tests run the real SQL from 20260812142109_NormaliseServiceUsersDisplayOrder against a
/// live database - the operations are pulled off the migration itself rather than copied - and
/// assert the repair on both shapes the corruption takes, with answers left untouched.
/// </summary>
public class ServiceUsersDisplayOrderMigrationTests(IntegrationTestWebFactory factory)
    : BaseIntegrationTest(factory), IAsyncLifetime
{
    private static readonly string[] EbiFieldKeys = ["QSU18", "QSU19", "QSU20_low", "QSU20_mid", "QSU20_high"];

    private DataCollectionFormModuleId _moduleId = null!;
    private FormSubmissionId _submissionId = null!;

    [Fact]
    public async Task NormalisesOrderAcrossSections_AndMovesEbiToTheEndOfItsSection()
    {
        // The shape the old per-section drag-drop left behind: every section renumbered 1..n on its
        // own, so a flat sort interleaves sections and clarification comments surfaces fourth.
        await SeedServiceUsersModule(ebiInClarificationSection: false);

        // Guard against a vacuous pass: the seeded module really does reproduce BLUM-7219.
        (await FieldKeysInReviewScreenOrder()).Should().StartWith(["QSU01", "QSU03", "QSU07", "QSU17"]);

        await RunMigrationAsync();

        var order = await FieldKeysInReviewScreenOrder();

        string[] expected =
        [
            "QSU_LEGACY",                                                        // no section, sorts as section 0
            "QSU01", "QSU02",                                                    // section 1
            "QSU03", "QSU04",                                                    // section 2
            "QSU07", "QSU08_white", "QSU16",                                     // section 3, ordinary questions
            "QSU18", "QSU19", "QSU20_low", "QSU20_mid", "QSU20_high",            // section 3, EBI block last
            "QSU17",                                                             // section 4, clarification comments
        ];

        order.Should().Equal(expected);
    }

    [Fact]
    public async Task PlacesEbiBeforeClarificationComments_EvenWhenTheyShareASection()
    {
        // The other shape: the questions were created through the admin API, which hands out a
        // module-wide max + 1, so they landed after the clarification comments they should precede.
        await SeedServiceUsersModule(ebiInClarificationSection: true);

        await RunMigrationAsync();

        var order = await FieldKeysInReviewScreenOrder();

        order.Should().EndWith(["QSU18", "QSU19", "QSU20_low", "QSU20_mid", "QSU20_high", "QSU17"]);
    }

    [Fact]
    public async Task LeavesEverySavedAnswerIntact_AndIsANoOpOnASecondRun()
    {
        await SeedServiceUsersModule(ebiInClarificationSection: false);
        var answersBefore = await SubmittedAnswers().CountAsync();

        await RunMigrationAsync();
        var orderAfterFirstRun = await FieldKeysInReviewScreenOrder();

        await RunMigrationAsync();

        (await FieldKeysInReviewScreenOrder()).Should().Equal(orderAfterFirstRun);
        (await SubmittedAnswers().CountAsync()).Should().Be(answersBefore);

        // Answers are keyed by form_fields.id, so every one still resolves to the question it was
        // given - renumbering cannot silently re-point an answer at a different question.
        var answeredFieldIds = await SubmittedAnswers().Select(v => v.FormFieldId).ToListAsync();
        var ebiFieldIds = await ModuleFields()
            .Where(f => EbiFieldKeys.Contains(f.FieldKey))
            .Select(f => f.Id)
            .ToListAsync();
        answeredFieldIds.Should().Contain(ebiFieldIds);
    }

    [Fact]
    public async Task ProducesADenseSequenceWithNoDuplicates()
    {
        await SeedServiceUsersModule(ebiInClarificationSection: false);

        await RunMigrationAsync();

        var orders = await ModuleFields()
            .OrderBy(f => f.DisplayOrder)
            .Select(f => f.DisplayOrder)
            .ToListAsync();

        orders.Should().Equal(Enumerable.Range(1, orders.Count));
    }

    [Fact]
    public async Task BreaksTiedDisplayOrdersByCreationOrder_NotAlphabetically()
    {
        // Live databases hold duplicate display_orders, so the tie-break decides real ordering.
        // Alphabetically the attendance boxes sort high, low, mid; they were created - and read -
        // low, mid, high.
        await SeedServiceUsersModule(ebiInClarificationSection: false);
        await GiveTheEbiAttendanceBoxesOneSharedDisplayOrder();

        await RunMigrationAsync();

        var order = await FieldKeysInReviewScreenOrder();

        order.Should().EndWith(["QSU20_low", "QSU20_mid", "QSU20_high", "QSU17"]);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await ClearAllTablesAsync();

    /// <summary>
    /// Collapses the three QSU20 boxes onto a single display_order with staggered created_at, the
    /// shape an admin produces by typing the same number into the edit screen three times.
    /// </summary>
    private async Task GiveTheEbiAttendanceBoxesOneSharedDisplayOrder()
    {
        await Context.Database.ExecuteSqlRawAsync(
            """
            UPDATE form_fields
            SET display_order = 5,
                created_at = TIMESTAMPTZ '2026-01-01 00:00:00Z' + (
                    CASE field_key WHEN 'QSU20_low' THEN 0 WHEN 'QSU20_mid' THEN 1 ELSE 2 END * INTERVAL '1 hour')
            WHERE field_key IN ('QSU20_low', 'QSU20_mid', 'QSU20_high');
            """);

        Context.ChangeTracker.Clear();
    }

    /// <summary>
    /// Executes the migration's own SQL, so the tests cannot drift away from what ships.
    /// </summary>
    private async Task RunMigrationAsync()
    {
        var migration = (Migration)new NormaliseServiceUsersDisplayOrder();

        foreach (var operation in migration.UpOperations.OfType<SqlOperation>())
        {
            await Context.Database.ExecuteSqlRawAsync(operation.Sql);
        }

        Context.ChangeTracker.Clear();
    }

    // The app seeds other modules at start-up, so every assertion is scoped to the module under test.
    private IQueryable<FormField> ModuleFields() =>
        Context.Set<FormField>().AsNoTracking().Where(f => f.FormModuleId == _moduleId);

    private IQueryable<FormFieldValue> SubmittedAnswers() =>
        Context.Set<FormFieldValue>().AsNoTracking().Where(v => v.FormSubmissionId == _submissionId);

    /// <summary>
    /// The order the review screen produced before this fix: display_order alone, section ignored.
    /// Once the numbers are normalised it has to match the API's section-then-order sequence.
    /// </summary>
    private async Task<List<string>> FieldKeysInReviewScreenOrder() =>
        await ModuleFields()
            .OrderBy(f => f.DisplayOrder)
            .Select(f => f.FieldKey)
            .ToListAsync();

    private async Task SeedServiceUsersModule(bool ebiInClarificationSection)
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

        var ebiSection = ebiInClarificationSection ? clarification : demographics;

        // display_order restarts at 1 in every section, which is exactly what the review screen
        // could not cope with.
        AddField(module, delivery.Id, "QSU01", "Was the service delivered for users this quarter?", FieldType.Radio, 1);
        AddField(module, delivery.Id, "QSU02", "Do you have data on how many people use this service?", FieldType.Radio, 2);
        AddField(module, numbers.Id, "QSU03", "How many people used this service over the relevant quarter?", FieldType.Number, 1);
        AddField(module, numbers.Id, "QSU04", "How many people used this service online over the relevant quarter?", FieldType.Number, 2);
        AddField(module, demographics.Id, "QSU07", "Do you have data on the ethnicity of users?", FieldType.Radio, 1);
        AddField(module, demographics.Id, "QSU08_white", "White British", FieldType.Number, 2);

        var qsu18 = AddField(module, ebiSection.Id, "QSU18", "Is the service categorised as an Evidence Based Intervention?", FieldType.Radio, 3);
        var qsu19 = AddField(module, ebiSection.Id, "QSU19", "Do you have data on the total number of sessions service users attended?", FieldType.Radio, 4);
        var qsu20Low = AddField(module, ebiSection.Id, "QSU20_low", "Attended less than 50% of sessions", FieldType.Number, 5);
        var qsu20Mid = AddField(module, ebiSection.Id, "QSU20_mid", "Attended 50% to 80% of sessions", FieldType.Number, 6);
        var qsu20High = AddField(module, ebiSection.Id, "QSU20_high", "Attended more than 80% of sessions", FieldType.Number, 7);

        AddField(module, demographics.Id, "QSU16", "What was the average waiting time from referral to first appointment?", FieldType.Number, 8);
        var qsu17 = AddField(module, clarification.Id, "QSU17", "Clarification comments", FieldType.Textarea, ebiInClarificationSection ? 20 : 1);

        // A question left without a section, as earlier migrations are known to have done. The API
        // sorts these as section 0, so the normalised sequence has to start with it.
        AddField(module, sectionId: null, "QSU_LEGACY", "A question left with no section", FieldType.Number, 99);

        Context.DataCollectionFormModules.Add(module);
        await SaveChangesAsync();
        _moduleId = module.Id;

        var submission = FormSubmission.Create(module.Id);
        _submissionId = submission.Id;
        submission.SetFieldValue(qsu18, "yes");
        submission.SetFieldValue(qsu19, "yes");
        submission.SetFieldValue(qsu20Low, "10");
        submission.SetFieldValue(qsu20Mid, "20");
        submission.SetFieldValue(qsu20High, "30");
        submission.SetFieldValue(qsu17, "Some clarifying notes");

        Context.Set<FormSubmission>().Add(submission);
        await SaveChangesAsync();
    }

    private static FormField AddField(
        DataCollectionFormModule module,
        FormSectionId? sectionId,
        string fieldKey,
        string label,
        FieldType fieldType,
        int displayOrder)
    {
        var field = module.AddField(fieldKey, label, fieldType, displayOrder);
        field.SetSection(sectionId);
        return field;
    }
}