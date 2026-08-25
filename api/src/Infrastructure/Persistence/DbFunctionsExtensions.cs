namespace Infrastructure.Persistence;

/// <summary>
/// C# stand-ins for database functions, registered via <see cref="ApplicationDbContext.OnModelCreating"/>.
/// Each method here only exists so EF Core has something to translate a LINQ call into — calling one
/// outside a query that gets translated to SQL always throws.
/// </summary>
public static class DbFunctionsExtensions
{
    /// <summary>
    /// SQL-native equivalent of <see cref="Application.Organisations.DeliveryPlanSpendReconciler.Money"/>,
    /// mapped to the Postgres function <c>money_sql</c> (see migration AddMoneySqlFunction). Needed
    /// because Money's char-by-char stripping is arbitrary imperative code that EF Core cannot
    /// translate — this lets a cross-organisation aggregation query SUM currency-formatted answers
    /// in the database instead of loading every raw value into memory first.
    ///
    /// Keep this parsing logic identical to Money's (digits, '.', '-' only; empty/unparseable → 0) —
    /// DeliveryOverviewMoneySqlParityTests asserts the two agree on the same inputs.
    /// </summary>
    public static decimal MoneySql(string? value) =>
        throw new NotSupportedException($"{nameof(MoneySql)} can only be used within a LINQ expression translated to SQL.");
}