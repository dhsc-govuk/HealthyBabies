using Application.Submissions.Commands;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Api.Tests.Integration.Submissions;

public class ConditionalRuleEvaluatorTests
{
    private const string Pps08Rule = "{\"showWhen\":{\"fieldKey\":\"PPS02\",\"in\":[\"gad7\"]}}";

    [Fact]
    public void IsFieldVisible_ReturnsTrue_WhenCheckboxParentHoldsOptionLabel()
    {
        var fields = BuildPps02AndPps08Fields();
        var pps08 = fields.First(f => f.FieldKey == "PPS08_pre");
        var rowData = Row(("PPS02", "GAD-7"));

        var visible = ConditionalRuleEvaluator.IsFieldVisible(pps08, rowData, fields);

        visible.Should().BeTrue();
    }

    [Fact]
    public void IsFieldVisible_ReturnsTrue_WhenCheckboxParentHoldsOptionValue()
    {
        var fields = BuildPps02AndPps08Fields();
        var pps08 = fields.First(f => f.FieldKey == "PPS08_pre");
        var rowData = Row(("PPS02", "gad7"));

        ConditionalRuleEvaluator.IsFieldVisible(pps08, rowData, fields).Should().BeTrue();
    }

    [Fact]
    public void IsFieldVisible_ReturnsTrue_WhenCheckboxParentHoldsMultipleValuesIncludingMatch()
    {
        var fields = BuildPps02AndPps08Fields();
        var pps08 = fields.First(f => f.FieldKey == "PPS08_pre");
        var rowData = Row(("PPS02", "asq3,gad7"));

        ConditionalRuleEvaluator.IsFieldVisible(pps08, rowData, fields).Should().BeTrue();
    }

    [Fact]
    public void IsFieldVisible_ReturnsTrue_WhenCheckboxParentHoldsMixedLabelsAndValues()
    {
        var fields = BuildPps02AndPps08Fields();
        var pps08 = fields.First(f => f.FieldKey == "PPS08_pre");
        var rowData = Row(("PPS02", "ASQ-3, gad7"));

        ConditionalRuleEvaluator.IsFieldVisible(pps08, rowData, fields).Should().BeTrue();
    }

    [Fact]
    public void IsFieldVisible_ReturnsTrue_WhenCheckboxParentHasWhitespaceAroundTokens()
    {
        var fields = BuildPps02AndPps08Fields();
        var pps08 = fields.First(f => f.FieldKey == "PPS08_pre");
        var rowData = Row(("PPS02", "  GAD-7  ,  ASQ-3  "));

        ConditionalRuleEvaluator.IsFieldVisible(pps08, rowData, fields).Should().BeTrue();
    }

    [Fact]
    public void IsFieldVisible_ReturnsFalse_WhenCheckboxParentDoesNotIncludeRequiredOption()
    {
        var fields = BuildPps02AndPps08Fields();
        var pps08 = fields.First(f => f.FieldKey == "PPS08_pre");
        var rowData = Row(("PPS02", "ASQ-3,CPRS-SF"));

        ConditionalRuleEvaluator.IsFieldVisible(pps08, rowData, fields).Should().BeFalse();
    }

    [Fact]
    public void IsFieldVisible_ReturnsFalse_WhenCheckboxParentIsEmpty()
    {
        var fields = BuildPps02AndPps08Fields();
        var pps08 = fields.First(f => f.FieldKey == "PPS08_pre");
        var rowData = Row(("PPS02", string.Empty));

        ConditionalRuleEvaluator.IsFieldVisible(pps08, rowData, fields).Should().BeFalse();
    }

    [Fact]
    public void IsFieldVisible_ReturnsTrue_WhenFieldHasNoConditionalRules()
    {
        var fields = BuildPps02AndPps08Fields();
        var pps02 = fields.First(f => f.FieldKey == "PPS02");

        ConditionalRuleEvaluator.IsFieldVisible(pps02, Row(), fields).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_Equals_NormalizesLabelOnRadioParent()
    {
        var fields = BuildRadioFixture();
        var condition = new ShowWhenCondition { FieldKey = "Q_SEX", Equals = "male" };
        var rowData = Row(("Q_SEX", "Male"));

        ConditionalRuleEvaluator.EvaluateCondition(condition, rowData, fields).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_Equals_ReturnsFalseWhenValueDoesNotMatch()
    {
        var fields = BuildRadioFixture();
        var condition = new ShowWhenCondition { FieldKey = "Q_SEX", Equals = "male" };
        var rowData = Row(("Q_SEX", "Female"));

        ConditionalRuleEvaluator.EvaluateCondition(condition, rowData, fields).Should().BeFalse();
    }

    [Fact]
    public void EvaluateCondition_AllOf_CombinesBothConditions()
    {
        var fields = BuildPps02AndPps08Fields();
        var condition = new ShowWhenCondition
        {
            AllOf = new List<ShowWhenCondition>
            {
                new() { FieldKey = "PPS02", In = new List<string> { "gad7" } },
                new() { FieldKey = "PPS02", In = new List<string> { "asq3" } }
            }
        };
        var rowData = Row(("PPS02", "gad7,asq3"));

        ConditionalRuleEvaluator.EvaluateCondition(condition, rowData, fields).Should().BeTrue();
    }

    [Fact]
    public void EvaluateCondition_AnyOf_MatchesAtLeastOne()
    {
        var fields = BuildPps02AndPps08Fields();
        var condition = new ShowWhenCondition
        {
            AnyOf = new List<ShowWhenCondition>
            {
                new() { FieldKey = "PPS02", In = new List<string> { "gad7" } },
                new() { FieldKey = "PPS02", In = new List<string> { "hle" } }
            }
        };
        var rowData = Row(("PPS02", "GAD-7"));

        ConditionalRuleEvaluator.EvaluateCondition(condition, rowData, fields).Should().BeTrue();
    }

    [Fact]
    public void NormalizeFieldValueToOptionValues_MapsCheckboxLabelsToValues()
    {
        var fields = BuildPps02AndPps08Fields();
        var pps02 = fields.First(f => f.FieldKey == "PPS02");

        var result = ConditionalRuleEvaluator.NormalizeFieldValueToOptionValues("GAD-7, ASQ-3", pps02);

        result.Should().BeEquivalentTo(new[] { "gad7", "asq3" });
    }

    [Fact]
    public void NormalizeFieldValueToOptionValues_KeepsUnrecognizedTokensUnchanged()
    {
        var fields = BuildPps02AndPps08Fields();
        var pps02 = fields.First(f => f.FieldKey == "PPS02");

        var result = ConditionalRuleEvaluator.NormalizeFieldValueToOptionValues("gad7, not-a-real-option", pps02);

        result.Should().BeEquivalentTo(new[] { "gad7", "not-a-real-option" });
    }

    [Fact]
    public void NormalizeFieldValueToOptionValues_ReturnsEmptyListForBlankInput()
    {
        var fields = BuildPps02AndPps08Fields();
        var pps02 = fields.First(f => f.FieldKey == "PPS02");

        ConditionalRuleEvaluator.NormalizeFieldValueToOptionValues("   ", pps02).Should().BeEmpty();
        ConditionalRuleEvaluator.NormalizeFieldValueToOptionValues(null, pps02).Should().BeEmpty();
    }

    [Fact]
    public void NormalizeFieldValueToOptionValues_DoesNotSplitNonCheckboxFields()
    {
        var fields = BuildRadioFixture();
        var radio = fields.First();

        var result = ConditionalRuleEvaluator.NormalizeFieldValueToOptionValues("Male", radio);

        result.Should().ContainSingle().Which.Should().Be("male");
    }

    private static Dictionary<string, string> Row(params (string Key, string Value)[] entries) =>
        entries.ToDictionary(e => e.Key, e => e.Value, StringComparer.OrdinalIgnoreCase);

    private static List<FormField> BuildPps02AndPps08Fields()
    {
        var module = NewModule();

        var pps02 = module.AddField("PPS02", "What outcome measure(s) did this parent or carer complete?", FieldType.Checkbox, 1, true);
        pps02.AddOption("asq3", "ASQ-3", 1);
        pps02.AddOption("cprs_sf", "CPRS-SF", 2);
        pps02.AddOption("gad7", "GAD-7", 3);
        pps02.AddOption("hle", "HLE", 4);

        var pps08 = module.AddField("PPS08_pre", "Pre-intervention GAD-7 score", FieldType.Number, 2);
        pps08.SetConditionalRules(Pps08Rule);

        return module.Fields.ToList();
    }

    private static List<FormField> BuildRadioFixture()
    {
        var module = NewModule();

        var field = module.AddField("Q_SEX", "Sex", FieldType.Radio, 1, true);
        field.AddOption("male", "Male", 1);
        field.AddOption("female", "Female", 2);

        return module.Fields.ToList();
    }

    private static DataCollectionFormModule NewModule() =>
        DataCollectionFormModule.Create(
            id: DataCollectionFormModuleId.New(),
            code: "test-module",
            sectionNumber: 1,
            name: "Test",
            description: "Test module");
}