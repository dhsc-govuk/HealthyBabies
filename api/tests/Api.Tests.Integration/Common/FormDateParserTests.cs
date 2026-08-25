using System.Globalization;
using Application.Common;
using FluentAssertions;
using Xunit;

namespace Api.Tests.Integration.Common;

// Date answers reach the API as the client emits them, DD/MM/YYYY. These tests assert the exact
// parsed value rather than "did it parse", because the defect behind BLUM-7521 was an ordering
// bug: "03/10/2026" parsed as 10 March instead of 3 October. Asserting exact values pins day-first
// ordering regardless of the culture the test host happens to run under.
public class FormDateParserTests
{
    [Theory]
    [InlineData("03/10/2026", 2026, 10, 3)] // the value reported in BLUM-7521 — month-first gives 10 March
    [InlineData("13/10/2026", 2026, 10, 13)] // day > 12 — month-first fails to parse this at all
    [InlineData("3/10/2026", 2026, 10, 3)] // unpadded day, as GovUKDateField emits before blur
    [InlineData("2026-10-03", 2026, 10, 3)] // ISO, the format bulk-upload templates advertise
    [InlineData("2026-10-03T00:00:00", 2026, 10, 3)]
    [InlineData("2026-10-03T12:30:00.123", 2026, 10, 3)]
    [InlineData("03-10-2026", 2026, 10, 3)] // dash separator — the bulk-upload grid accepts this
    [InlineData("2026/10/03", 2026, 10, 3)] // year-first with slashes — likewise
    [InlineData("03/10/2026 14:30:00", 2026, 10, 3)]
    [InlineData("9/9/2025 12:00:00 AM", 2025, 9, 9)] // legacy .NET round-trip shape held by older rows
    [InlineData("9 September 2025", 2025, 9, 9)] // an Excel column formatted as a long date
    [InlineData("9 Sep 2025", 2025, 9, 9)]
    [InlineData("  03/10/2026  ", 2026, 10, 3)]
    public void ShouldParseDayFirst(string value, int expectedYear, int expectedMonth, int expectedDay)
    {
        // Act
        var parsed = FormDateParser.TryParse(value, out var date);

        // Assert - a value carrying a time keeps it; every rule check compares on .Date
        parsed.Should().BeTrue();
        date.Date.Should().Be(new DateTime(expectedYear, expectedMonth, expectedDay));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a date")]
    [InlineData("2026-13-01")]
    [InlineData("32/01/2026")]
    [InlineData("30/02/2026")]
    public void ShouldNotParse_WhenValueIsNotARealDayFirstDate(string? value)
    {
        // Act
        var parsed = FormDateParser.TryParse(value, out var date);

        // Assert
        parsed.Should().BeFalse();
        date.Should().Be(default);
    }

    [Fact]
    public void ShouldParseDayFirst_EvenUnderAMonthFirstCulture()
    {
        // Arrange — reproduces the deployed host, which configures no culture and so resolves
        // month-first. This is the condition that made BLUM-7521 invisible on en-GB dev machines.
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        try
        {
            // Act
            var parsed = FormDateParser.TryParse("03/10/2026", out var date);

            // Assert
            parsed.Should().BeTrue();
            date.Should().Be(new DateTime(2026, 10, 3));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}