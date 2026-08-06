using Api.Services.Implementation;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Api.Tests.Integration.Submissions;

/// <summary>
/// Unit tests for the self-describing answer snapshot (BLUM-7178): submitting an answer
/// captures the field's label/type/options at that moment, and viewing/exporting resolves
/// the answer from that snapshot so it survives later form/option changes.
/// </summary>
public class FormFieldValueSnapshotTests
{
    private static DataCollectionFormModule NewModule() =>
        DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            code: "TEST",
            sectionNumber: 1,
            name: "Test module");

    [Fact]
    public void SetFieldValue_CapturesFieldSnapshot()
    {
        var module = NewModule();
        var field = module.AddField("SMD03", "What is the status of the service?", FieldType.Radio, displayOrder: 3);
        field.AddOption("open", "The service is open", 1);
        field.AddOption("closed", "The service is closed", 2);

        var submission = FormSubmission.Create(module.Id);
        var fieldValue = submission.SetFieldValue(field, "open");

        fieldValue.Value.Should().Be("open");
        fieldValue.QuestionCode.Should().Be("SMD03");
        fieldValue.QuestionLabel.Should().Be("What is the status of the service?");
        fieldValue.FieldType.Should().Be(FieldType.Radio.Value);
        fieldValue.DisplayOrder.Should().Be(3);
        fieldValue.OptionsSnapshot.Should().NotBeNull();
        fieldValue.OptionsSnapshot.Should().Contain("The service is open").And.Contain("The service is closed");
    }

    [Fact]
    public void ResolveDisplayValue_PrefersSnapshot_WhenLiveOptionRelabelled()
    {
        var module = NewModule();
        var field = module.AddField("SMD03", "What is the status of the service?", FieldType.Radio, displayOrder: 3);
        field.AddOption("open", "The service is open", 1);

        var submission = FormSubmission.Create(module.Id);
        var fieldValue = submission.SetFieldValue(field, "open");

        // The live form later relabels (and drops) the option the answer used.
        var liveModule = NewModule();
        var liveField = liveModule.AddField("SMD03", "What is the status of the service?", FieldType.Radio, displayOrder: 3);
        liveField.AddOption("open", "Now open", 1);

        AnswerSnapshotResolver.ResolveDisplayValue(fieldValue, liveField).Should().Be("The service is open");
    }

    [Fact]
    public void ResolveDisplayValue_ResolvesFromSnapshot_WhenLiveOptionRemoved()
    {
        var module = NewModule();
        var field = module.AddField("SMD03", "Status", FieldType.Radio, displayOrder: 1);
        field.AddOption("open", "The service is open", 1);

        var submission = FormSubmission.Create(module.Id);
        var fieldValue = submission.SetFieldValue(field, "open");

        // Live field no longer has the option at all.
        var liveModule = NewModule();
        var liveField = liveModule.AddField("SMD03", "Status", FieldType.Radio, displayOrder: 1);
        liveField.AddOption("closed", "Closed", 1);

        AnswerSnapshotResolver.ResolveDisplayValue(fieldValue, liveField).Should().Be("The service is open");
    }

    [Fact]
    public void ResolveDisplayValue_HandlesMultiSelectFromSnapshot()
    {
        var module = NewModule();
        var field = module.AddField("SMD16", "How is this service delivered?", FieldType.Checkbox, displayOrder: 1);
        field.AddOption("a", "Home visits", 1);
        field.AddOption("b", "Peer support", 2);
        field.AddOption("c", "Other", 3);

        var submission = FormSubmission.Create(module.Id);
        var fieldValue = submission.SetFieldValue(field, "a,b");

        AnswerSnapshotResolver.ResolveDisplayValue(fieldValue, liveField: null)
            .Should().Be("Home visits, Peer support");
    }

    [Fact]
    public void ResolveDisplayValue_FallsBackToLiveOptions_WhenNoSnapshot()
    {
        var module = NewModule();
        var field = module.AddField("SMD03", "Status", FieldType.Radio, displayOrder: 1);
        field.AddOption("open", "The service is open", 1);

        // Legacy row: saved via the id-only overload, so no snapshot is captured.
        var submission = FormSubmission.Create(module.Id);
        var fieldValue = submission.SetFieldValue(field.Id, "open");

        AnswerSnapshotResolver.ResolveDisplayValue(fieldValue, field).Should().Be("The service is open");
    }

    [Fact]
    public void ResolveDisplayValue_ReturnsRawValue_WhenNoOptionsAnywhere()
    {
        var module = NewModule();
        var field = module.AddField("SMD01", "Service name", FieldType.Text, displayOrder: 1);

        var submission = FormSubmission.Create(module.Id);
        var fieldValue = submission.SetFieldValue(field, "Play group");

        AnswerSnapshotResolver.ResolveDisplayValue(fieldValue, field).Should().Be("Play group");
    }
}