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
    public void EvaluateCondition_AllOf_AcrossDifferentFields_RequiresAllToMatch()
    {
        var fields = BuildTwoRadioFixture();
        var condition = new ShowWhenCondition
        {
            AllOf = new List<ShowWhenCondition>
            {
                new() { FieldKey = "Q_SEX", Equals = "male" },
                new() { FieldKey = "Q_AGE", Equals = "adult" }
            }
        };

        ConditionalRuleEvaluator.EvaluateCondition(condition, Row(("Q_SEX", "Male"), ("Q_AGE", "Adult")), fields).Should().BeTrue();
        ConditionalRuleEvaluator.EvaluateCondition(condition, Row(("Q_SEX", "Male"), ("Q_AGE", "Child")), fields).Should().BeFalse();
        ConditionalRuleEvaluator.EvaluateCondition(condition, Row(("Q_SEX", "Female"), ("Q_AGE", "Adult")), fields).Should().BeFalse();
    }

    [Fact]
    public void IsFieldVisible_AllOf_DeserializesAndEvaluatesCorrectly()
    {
        var fields = BuildTwoRadioFixture();
        const string rule = "{\"showWhen\":{\"allOf\":[{\"fieldKey\":\"Q_SEX\",\"equals\":\"male\"},{\"fieldKey\":\"Q_AGE\",\"equals\":\"adult\"}]}}";

        var target = fields.First(f => f.FieldKey == "Q_SEX");
        target.SetConditionalRules(rule);

        ConditionalRuleEvaluator.IsFieldVisible(target, Row(("Q_SEX", "male"), ("Q_AGE", "adult")), fields).Should().BeTrue();
        ConditionalRuleEvaluator.IsFieldVisible(target, Row(("Q_SEX", "male"), ("Q_AGE", "child")), fields).Should().BeFalse();
    }

    [Theory]
    [InlineData("3", true)]
    [InlineData("2", true)]
    [InlineData("1", false)]
    [InlineData("", false)]
    public void EvaluateCondition_Gte_ComparesNumerically(string value, bool expected)
    {
        var fields = BuildNumericFixture();
        var condition = new ShowWhenCondition { FieldKey = "Q_COUNT", Gte = "2" };

        ConditionalRuleEvaluator.EvaluateCondition(condition, Row(("Q_COUNT", value)), fields).Should().Be(expected);
    }

    [Theory]
    [InlineData("3", true)]
    [InlineData("2", false)]
    [InlineData("1", false)]
    public void EvaluateCondition_Gt_ComparesNumerically(string value, bool expected)
    {
        var fields = BuildNumericFixture();
        var condition = new ShowWhenCondition { FieldKey = "Q_COUNT", Gt = "2" };

        ConditionalRuleEvaluator.EvaluateCondition(condition, Row(("Q_COUNT", value)), fields).Should().Be(expected);
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("2", false)]
    [InlineData("3", false)]
    public void EvaluateCondition_Lt_ComparesNumerically(string value, bool expected)
    {
        var fields = BuildNumericFixture();
        var condition = new ShowWhenCondition { FieldKey = "Q_COUNT", Lt = "2" };

        ConditionalRuleEvaluator.EvaluateCondition(condition, Row(("Q_COUNT", value)), fields).Should().Be(expected);
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("2", true)]
    [InlineData("3", false)]
    public void EvaluateCondition_Lte_ComparesNumerically(string value, bool expected)
    {
        var fields = BuildNumericFixture();
        var condition = new ShowWhenCondition { FieldKey = "Q_COUNT", Lte = "2" };

        ConditionalRuleEvaluator.EvaluateCondition(condition, Row(("Q_COUNT", value)), fields).Should().Be(expected);
    }

    [Theory]
    [InlineData("2", true)]
    [InlineData("2.0", true)]
    [InlineData("3", false)]
    public void EvaluateCondition_Eq_ComparesNumerically(string value, bool expected)
    {
        var fields = BuildNumericFixture();
        var condition = new ShowWhenCondition { FieldKey = "Q_COUNT", Eq = "2" };

        ConditionalRuleEvaluator.EvaluateCondition(condition, Row(("Q_COUNT", value)), fields).Should().Be(expected);
    }

    [Theory]
    [InlineData("3", true)]
    [InlineData("2", false)]
    [InlineData("2.0", false)]
    public void EvaluateCondition_Neq_ComparesNumerically(string value, bool expected)
    {
        var fields = BuildNumericFixture();
        var condition = new ShowWhenCondition { FieldKey = "Q_COUNT", Neq = "2" };

        ConditionalRuleEvaluator.EvaluateCondition(condition, Row(("Q_COUNT", value)), fields).Should().Be(expected);
    }

    [Fact]
    public void IsFieldVisible_NumericOperator_DeserializesAndEvaluatesCorrectly()
    {
        var fields = BuildNumericFixture();
        var target = fields.First(f => f.FieldKey == "Q_DEPENDENT");
        target.SetConditionalRules("{\"showWhen\":{\"fieldKey\":\"Q_COUNT\",\"gt\":\"0\"}}");

        ConditionalRuleEvaluator.IsFieldVisible(target, Row(("Q_COUNT", "5")), fields).Should().BeTrue();
        ConditionalRuleEvaluator.IsFieldVisible(target, Row(("Q_COUNT", "0")), fields).Should().BeFalse();
    }

    [Fact]
    public void EvaluateCondition_AllOf_WithNumericAndServiceFieldKeyIn_GatesLikeQsu07()
    {
        // Mirrors the QSU07-style gate: QSU03 >= 1 (same entity) AND SMD02 funded/partially funded (Service entity).
        var fields = BuildNumericFixture();
        var condition = new ShowWhenCondition
        {
            AllOf = new List<ShowWhenCondition>
            {
                new() { FieldKey = "Q_COUNT", Gte = "1" },
                new() { ServiceFieldKey = "SMD02", In = new List<string> { "0", "1" } }
            }
        };
        var rowData = Row(("Q_COUNT", "5"));
        var funded = new Dictionary<string, string> { ["SMD02"] = "0" };
        var notFunded = new Dictionary<string, string> { ["SMD02"] = "2" };

        ConditionalRuleEvaluator.EvaluateCondition(condition, rowData, fields, funded).Should().BeTrue();
        ConditionalRuleEvaluator.EvaluateCondition(condition, rowData, fields, notFunded).Should().BeFalse();
        ConditionalRuleEvaluator.EvaluateCondition(condition, Row(("Q_COUNT", "0")), fields, funded).Should().BeFalse();
        ConditionalRuleEvaluator.EvaluateCondition(condition, rowData, fields).Should().BeFalse();
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

    private static List<FormField> BuildTwoRadioFixture()
    {
        var module = NewModule();

        var sex = module.AddField("Q_SEX", "Sex", FieldType.Radio, 1, true);
        sex.AddOption("male", "Male", 1);
        sex.AddOption("female", "Female", 2);

        var age = module.AddField("Q_AGE", "Age group", FieldType.Radio, 2, true);
        age.AddOption("adult", "Adult", 1);
        age.AddOption("child", "Child", 2);

        return module.Fields.ToList();
    }

    private static List<FormField> BuildNumericFixture()
    {
        var module = NewModule();

        module.AddField("Q_COUNT", "Overall user count", FieldType.Number, 1, true);
        module.AddField("Q_DEPENDENT", "Ethnicity availability", FieldType.Radio, 2);

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