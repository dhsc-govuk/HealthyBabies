using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMoneySqlFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL-native twin of DeliveryPlanSpendReconciler.Money — strips everything except digits,
            // '.' and '-', then parses. Uses plpgsql with an exception handler (not a plain SQL cast)
            // because a malformed value (e.g. "1-2" after stripping) must fall back to 0 the same way
            // Money's decimal.TryParse does, rather than aborting the whole aggregate query.
            //
            // Money uses NumberStyles.Number, which accepts a *trailing* sign as well as a leading one
            // (accounting-style negatives, e.g. "100-" -> -100). Postgres's numeric literal syntax has
            // no such allowance, so a trailing-only sign is moved to the front before casting — a value
            // with both a leading and trailing sign (e.g. "-100-") is left alone and still fails to
            // parse, matching TryParse's own rejection of that shape.
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION money_sql(value text)
                RETURNS numeric
                LANGUAGE plpgsql
                IMMUTABLE
                AS $$
                DECLARE
                    cleaned text;
                BEGIN
                    cleaned := regexp_replace(coalesce(value, ''), '[^0-9.-]', '', 'g');
                    IF cleaned IS NULL OR cleaned = '' THEN
                        RETURN 0;
                    END IF;
                    IF right(cleaned, 1) = '-' AND left(cleaned, 1) != '-' THEN
                        cleaned := '-' || left(cleaned, length(cleaned) - 1);
                    END IF;
                    BEGIN
                        RETURN cleaned::numeric;
                    EXCEPTION WHEN OTHERS THEN
                        RETURN 0;
                    END;
                END;
                $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS money_sql(text);");
        }
    }
}