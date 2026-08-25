using Application.Organisations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Xunit;

namespace Api.Tests.Integration.Organisations;

/// <summary>
/// Asserts the SQL-native money_sql function (registered in AddMoneySqlFunction, used by
/// DeliveryOverviewQueries to sum currency answers at the database level) agrees with
/// DeliveryPlanSpendReconciler.Money (the C# parser the existing per-LA dashboard uses) on the same
/// inputs. A mismatch here means the new cross-LA Delivery overview totals would silently disagree
/// with the existing per-LA totals for the same underlying data.
///
/// NOTE: runs against a real Postgres via Testcontainers.
/// </summary>
public class DeliveryOverviewMoneySqlParityTests(IntegrationTestWebFactory factory) : BaseIntegrationTest(factory)
{
    public static IEnumerable<object?[]> MessyValues =>
        new List<object?[]>
        {
            new object?[] { "£1,000.00" },
            new object?[] { " 500 " },
            new object?[] { string.Empty },
            new object?[] { null },
            new object?[] { "0" },
            new object?[] { "-100" },
            new object?[] { "100-" }, // accounting-style trailing negative
            new object?[] { "£8,000.00" },
            new object?[] { " 9000 " },
            new object?[] { "1-2" }, // malformed either way — both must fall back to 0
            new object?[] { "-100-" }, // both a leading and trailing sign — invalid either way
            new object?[] { "not a number" },
        };

    [Theory]
    [MemberData(nameof(MessyValues))]
    public async Task MoneySqlShouldAgreeWithMoney(string? raw)
    {
        var expected = DeliveryPlanSpendReconciler.Money(raw);

        // SqlQueryRaw<T> for a scalar (non-entity) type requires the result set to have a single
        // column named exactly "Value" — EF wraps this query and selects s."Value" from it, so an
        // unaliased column here fails with "column s.Value does not exist" at query time.
        var actual = await Context.Database
            .SqlQueryRaw<decimal>("SELECT money_sql({0}) AS \"Value\"", (object?)raw ?? DBNull.Value)
            .SingleAsync();

        actual.Should().Be(expected);
    }
}