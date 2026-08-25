using Application.Submissions.Helpers;
using Domain.ServiceForms;
using Domain.Services;
using FluentAssertions;
using Tests.Data;
using Xunit;

namespace Api.Tests.Integration.Submissions;

// SMD04 is stored day-first, as the client emits it, but this helper used to read it month-first
// via InvariantCulture. The collection start below sits between 10 March and 3 October so that
// "03/10/2026" and "10/03/2026" land on opposite sides of it — the pair isolates the ordering.
// SubmissionsServiceUsersFilterTests covers the same helper end to end, but only with ISO values,
// which parse identically either way.
public class ServiceQuarterlyInclusionTests
{
    private static readonly DateTime CollectionStart = new(2026, 6, 1);

    private static Service ClosedService(string closedDateValue)
    {
        var service = Service.New(ServiceId.New(), OrganisationAdminUsersData.OrganisationId, "Closed Service");
        service.AddAnswer("SMD03", "What is the status of the service?", null, ServiceFormQuestionType.Radio, 1, 3, "2", "No longer offered");
        service.AddAnswer("SMD04", "When did the service close?", null, ServiceFormQuestionType.Date, 1, 4, closedDateValue, null);
        return service;
    }

    [Theory]
    [InlineData("03/10/2026")] // 3 October 2026 — month-first reads this as 10 March and excludes it
    [InlineData("20/07/2026")] // day above 12 — month-first cannot parse it at all and excludes it
    [InlineData("2026-10-03")] // ISO, still accepted
    [InlineData("03-10-2026")] // dash separator, as the bulk-upload grid allows
    public void ShouldInclude_WhenServiceClosedOnOrAfterCollectionStart(string closedDateValue)
    {
        // Act
        var included = ServiceQuarterlyInclusion.IsIncludedInQuarterlyServiceUsers(ClosedService(closedDateValue), CollectionStart);

        // Assert
        included.Should().BeTrue();
    }

    [Theory]
    [InlineData("10/03/2026")] // 10 March 2026 — month-first reads this as 3 October and includes it
    [InlineData("20/02/2026")]
    [InlineData("2026-03-10")]
    public void ShouldExclude_WhenServiceClosedBeforeCollectionStart(string closedDateValue)
    {
        // Act
        var included = ServiceQuarterlyInclusion.IsIncludedInQuarterlyServiceUsers(ClosedService(closedDateValue), CollectionStart);

        // Assert
        included.Should().BeFalse();
    }

    [Fact]
    public void ShouldInclude_WhenServiceClosedExactlyOnCollectionStart()
    {
        // Act
        var included = ServiceQuarterlyInclusion.IsIncludedInQuarterlyServiceUsers(ClosedService("01/06/2026"), CollectionStart);

        // Assert
        included.Should().BeTrue();
    }

    [Fact]
    public void ShouldExclude_WhenClosedDateIsMissingOrUnreadable()
    {
        // Arrange - a service marked "no longer offered" with no usable closing date cannot be
        // placed in a quarter, so it stays out rather than defaulting in
        var service = Service.New(ServiceId.New(), OrganisationAdminUsersData.OrganisationId, "Closed Service");
        service.AddAnswer("SMD03", "What is the status of the service?", null, ServiceFormQuestionType.Radio, 1, 3, "2", "No longer offered");

        // Act
        var included = ServiceQuarterlyInclusion.IsIncludedInQuarterlyServiceUsers(service, CollectionStart);

        // Assert
        included.Should().BeFalse();
    }

    [Fact]
    public void ShouldInclude_WhenServiceIsStillOffered_RegardlessOfAnyClosedDate()
    {
        // Arrange
        var service = Service.New(ServiceId.New(), OrganisationAdminUsersData.OrganisationId, "Live Service");
        service.AddAnswer("SMD03", "What is the status of the service?", null, ServiceFormQuestionType.Radio, 1, 3, "0", "Live");

        // Act
        var included = ServiceQuarterlyInclusion.IsIncludedInQuarterlyServiceUsers(service, CollectionStart);

        // Assert
        included.Should().BeTrue();
    }
}