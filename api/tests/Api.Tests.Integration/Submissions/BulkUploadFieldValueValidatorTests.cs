using Application.Submissions.Commands;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Api.Tests.Integration.Submissions;

public class BulkUploadFieldValueValidatorTests
{
    private static DataCollectionFormModule NewModule() =>
        DataCollectionFormModule.Create(DataCollectionFormModuleId.New(), "test-module", 1, "Test module");

    private static FormField Qsu06Field()
    {
        var module = NewModule();
        var field = module.AddField("QSU06", "Which types of user are included in your data?", FieldType.Checkbox, 1);
        field.AddOption("primary_carers", "Primary carers", 1);
        field.AddOption("co_parents", "Co-parents/partners/spouses/other adult", 2);
        field.AddOption("children", "Children", 3);
        field.AddOption("siblings", "Siblings", 4);
        field.AddOption("other", "Other", 5);
        field.AddOption("unknown", "Unknown", 6);
        return field;
    }

    private static FormField DemographicField(string fieldKey, string maxSumField, string? sumGroup = null)
    {
        var module = NewModule();
        var field = module.AddField(fieldKey, fieldKey, FieldType.Number, 1);
        field.SetConfiguration(sumGroup != null
            ? $"{{\"min\": 0, \"sumGroup\": \"{sumGroup}\", \"maxSumField\": \"{maxSumField}\"}}"
            : $"{{\"min\": 0, \"maxSumField\": \"{maxSumField}\"}}");
        return field;
    }

    [Fact]
    public void ValidateFieldValue_RejectsInvalidCheckboxOption()
    {
        var field = Qsu06Field();

        var errors = BulkUploadFieldValueValidator.ValidateFieldValue("child", field);

        errors.Should().ContainSingle(e => e.Contains("Invalid options") && e.Contains("Children"));
    }

    [Fact]
    public void ValidateFieldValue_AcceptsValidCheckboxOption()
    {
        var field = Qsu06Field();

        var errors = BulkUploadFieldValueValidator.ValidateFieldValue("children", field);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateFieldValue_RejectsNonNumericValue()
    {
        var field = DemographicField("QSU08_white", "QSU03");

        var errors = BulkUploadFieldValueValidator.ValidateFieldValue("test", field);

        errors.Should().ContainSingle(e => e == "Must be a valid number");
    }

    [Fact]
    public void ValidateFieldValue_RejectsNegativeNumber()
    {
        var field = DemographicField("QSU08_white", "QSU03");

        var errors = BulkUploadFieldValueValidator.ValidateFieldValue("-5", field);

        errors.Should().ContainSingle(e => e == "Must be 0 or more");
    }

    [Fact]
    public void ValidateFieldValue_AcceptsValidNonNegativeNumber()
    {
        var field = DemographicField("QSU08_white", "QSU03");

        var errors = BulkUploadFieldValueValidator.ValidateFieldValue("5", field);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSumGroups_RejectsWhenGroupSumExceedsMax()
    {
        var white = DemographicField("QSU08_white", "QSU03", "ethnicity");
        var mixed = DemographicField("QSU08_mixed", "QSU03", "ethnicity");
        var fields = new List<FormField> { white, mixed };

        var rowData = new Dictionary<string, string>
        {
            ["QSU03"] = "10",
            ["QSU08_white"] = "8",
            ["QSU08_mixed"] = "5",
        };

        var errors = BulkUploadFieldValueValidator.ValidateSumGroups(rowData, fields);

        errors.Should().Contain(e => e.FieldKey == "QSU08_white" && e.Message.Contains("cannot exceed the total number of service users (10)"));
        errors.Should().Contain(e => e.FieldKey == "QSU08_mixed");
    }

    [Fact]
    public void ValidateSumGroups_AcceptsWhenGroupSumWithinMax()
    {
        var white = DemographicField("QSU08_white", "QSU03", "ethnicity");
        var mixed = DemographicField("QSU08_mixed", "QSU03", "ethnicity");
        var fields = new List<FormField> { white, mixed };

        var rowData = new Dictionary<string, string>
        {
            ["QSU03"] = "10",
            ["QSU08_white"] = "4",
            ["QSU08_mixed"] = "5",
        };

        var errors = BulkUploadFieldValueValidator.ValidateSumGroups(rowData, fields);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateSumGroups_RejectsSingleFieldExceedingMax_WhenNoSumGroup()
    {
        var qsu14 = DemographicField("QSU14", "QSU03");
        var fields = new List<FormField> { qsu14 };

        var rowData = new Dictionary<string, string>
        {
            ["QSU03"] = "10",
            ["QSU14"] = "15",
        };

        var errors = BulkUploadFieldValueValidator.ValidateSumGroups(rowData, fields);

        errors.Should().ContainSingle(e => e.FieldKey == "QSU14" && e.Message.Contains("cannot exceed the total number of service users (10)"));
    }
}