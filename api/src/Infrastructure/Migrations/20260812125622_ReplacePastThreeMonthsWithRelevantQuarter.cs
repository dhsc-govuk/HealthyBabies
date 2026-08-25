using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePastThreeMonthsWithRelevantQuarter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BLUM-7218. The MI form copy pins local authorities to a fixed reporting window - "the
            // past/last 3 months", "the last quarter (1 January to 31 March 2026)", "as of 31 March
            // 2026". That is wrong: an LA is not always filling in the quarter that just ended
            // (reopened and back-dated collections both happen), and each data collection carries
            // its own start/end reporting window. The agreed wording is "the relevant quarter",
            // which is already the house style across most of the seeded question copy.
            //
            // This is data-only - no schema change. It rewrites rows in place rather than
            // reseeding, because production holds real LA submissions and form_field_values rows
            // reference form_fields.id, so field identity must be preserved.
            //
            // Six rewrites, applied in order. A "date token" below means a day-and-month date
            // ("1 October", "31 December 2026") or an unsubstituted {startDate}/{endDate}
            // placeholder. Bounding the ranges to that token - rather than "anything up to the next
            // comma" - is deliberate: a looser matcher would swallow trailing sentence content, so
            // "..., from 1 October to 31 December 2026 excluding any users referred to other
            // services" would silently lose its exclusion clause.
            //
            //   1. "<prep> the past|last 3 months, from <date> to <date>"  -> "<prep> the relevant quarter"
            //   2. "<prep> the past|last 3 months (<date> to <date>)"      -> "<prep> the relevant quarter"
            //   3. bare "<prep> the past|last 3 months"                    -> "<prep> the relevant quarter"
            //   4. "<prep> the past|last quarter (<date> to <date>)"       -> "<prep> the relevant quarter"
            //   5. bare "<prep> the past|last quarter"                     -> "<prep> the relevant quarter"
            //   6. "as of <date>"                                          -> "as of the end of the relevant quarter"
            //
            // Rules 1/2 and 4 must precede 3 and 5 respectively, so the date range is removed with
            // the phrase instead of being stranded next to "the relevant quarter". Trailing clauses
            // outside the range survive: "Give the average for the last 3 months, from 1 October to
            // 31 December 2026, in number of days." becomes "Give the average for the relevant
            // quarter, in number of days."
            //
            // Matching is deliberately pattern-based rather than an enumerated list of strings:
            // content has drifted between environments, and migration 20260716134254 renamed
            // QSU13A_* -> QSU13B_*, so keying off field_key would silently miss rows. The
            // preposition is required so unrelated prose ("compared to the last quarter") is left
            // alone, and "this quarter" is never touched - it correctly means the open submission.
            //
            // Idempotent: each UPDATE only touches rows the rewrite actually changes, so a second
            // run is a no-op.
            migrationBuilder.Sql(@"
                CREATE FUNCTION pg_temp.to_relevant_quarter(txt text) RETURNS text AS $f$
                    SELECT regexp_replace(
                           regexp_replace(
                           regexp_replace(
                           regexp_replace(
                           regexp_replace(
                           regexp_replace(
                               txt,
                               '\m(for|over|in)\s+the\s+(?:past|last)\s+(?:3|three)\s+months,\s+from\s+(?:\{[A-Za-z]+\}|[0-9]{1,2}\s+[A-Za-z]+(?:\s+[0-9]{4})?)\s+to\s+(?:\{[A-Za-z]+\}|[0-9]{1,2}\s+[A-Za-z]+(?:\s+[0-9]{4})?)',
                               '\1 the relevant quarter', 'gi'),
                               '\m(for|over|in)\s+the\s+(?:past|last)\s+(?:3|three)\s+months\s*\(\s*(?:\{[A-Za-z]+\}|[0-9]{1,2}\s+[A-Za-z]+(?:\s+[0-9]{4})?)\s+to\s+(?:\{[A-Za-z]+\}|[0-9]{1,2}\s+[A-Za-z]+(?:\s+[0-9]{4})?)\s*\)',
                               '\1 the relevant quarter', 'gi'),
                               '\m(for|over|in)\s+the\s+(?:past|last)\s+(?:3|three)\s+months',
                               '\1 the relevant quarter', 'gi'),
                               '\m(for|over|in)\s+the\s+(?:past|last)\s+quarter\s*\(\s*(?:\{[A-Za-z]+\}|[0-9]{1,2}\s+[A-Za-z]+(?:\s+[0-9]{4})?)\s+to\s+(?:\{[A-Za-z]+\}|[0-9]{1,2}\s+[A-Za-z]+(?:\s+[0-9]{4})?)\s*\)',
                               '\1 the relevant quarter', 'gi'),
                               '\m(for|over|in)\s+the\s+(?:past|last)\s+quarter',
                               '\1 the relevant quarter', 'gi'),
                               '\mas\s+of\s+(?:\{[A-Za-z]+\}|[0-9]{1,2}\s+[A-Za-z]+(?:\s+[0-9]{4})?)',
                               'as of the end of the relevant quarter', 'gi');
                $f$ LANGUAGE sql IMMUTABLE;

                -- Rewrite the module's own description and bump last_changed_on, which is the value
                -- the admin data-collection wizard renders as the module's ""Last changed on"" date;
                -- leaving it stale would hide this content change from the admin assembling the next
                -- collection. The bump also covers modules whose own description is fine but whose
                -- sections or fields are about to be rewritten below, so this statement must run
                -- BEFORE those - once the children are rewritten the EXISTS predicates no longer
                -- match. Re-running is still a no-op: nothing is left for the function to change.
                UPDATE data_collection_form_modules m
                SET description = pg_temp.to_relevant_quarter(m.description),
                    last_changed_on = NOW(),
                    updated_at = NOW()
                WHERE pg_temp.to_relevant_quarter(m.description) IS DISTINCT FROM m.description
                   OR EXISTS (
                        SELECT 1 FROM form_sections s
                        WHERE s.form_module_id = m.id
                          AND (pg_temp.to_relevant_quarter(s.description) IS DISTINCT FROM s.description
                            OR pg_temp.to_relevant_quarter(s.help_text) IS DISTINCT FROM s.help_text))
                   OR EXISTS (
                        SELECT 1 FROM form_fields f
                        WHERE f.form_module_id = m.id
                          AND (pg_temp.to_relevant_quarter(f.label) IS DISTINCT FROM f.label
                            OR pg_temp.to_relevant_quarter(f.help_text) IS DISTINCT FROM f.help_text
                            OR pg_temp.to_relevant_quarter(f.placeholder) IS DISTINCT FROM f.placeholder
                            OR (jsonb_typeof(f.configuration -> 'groupHint') = 'string'
                                AND pg_temp.to_relevant_quarter(f.configuration ->> 'groupHint')
                                    IS DISTINCT FROM f.configuration ->> 'groupHint')
                            OR (jsonb_typeof(f.configuration -> 'groupLabel') = 'string'
                                AND pg_temp.to_relevant_quarter(f.configuration ->> 'groupLabel')
                                    IS DISTINCT FROM f.configuration ->> 'groupLabel')));

                UPDATE form_sections
                SET description = pg_temp.to_relevant_quarter(description),
                    updated_at = NOW()
                WHERE pg_temp.to_relevant_quarter(description) IS DISTINCT FROM description;

                UPDATE form_sections
                SET help_text = pg_temp.to_relevant_quarter(help_text),
                    updated_at = NOW()
                WHERE pg_temp.to_relevant_quarter(help_text) IS DISTINCT FROM help_text;

                UPDATE form_fields
                SET label = pg_temp.to_relevant_quarter(label),
                    updated_at = NOW()
                WHERE pg_temp.to_relevant_quarter(label) IS DISTINCT FROM label;

                UPDATE form_fields
                SET help_text = pg_temp.to_relevant_quarter(help_text),
                    updated_at = NOW()
                WHERE pg_temp.to_relevant_quarter(help_text) IS DISTINCT FROM help_text;

                UPDATE form_fields
                SET placeholder = pg_temp.to_relevant_quarter(placeholder),
                    updated_at = NOW()
                WHERE pg_temp.to_relevant_quarter(placeholder) IS DISTINCT FROM placeholder;

                -- configuration is jsonb; rewrite the two copy-bearing keys individually with
                -- jsonb_set so the document structure cannot be disturbed.
                UPDATE form_fields
                SET configuration = jsonb_set(
                        configuration,
                        '{groupHint}',
                        to_jsonb(pg_temp.to_relevant_quarter(configuration ->> 'groupHint'))),
                    updated_at = NOW()
                WHERE jsonb_typeof(configuration -> 'groupHint') = 'string'
                  AND pg_temp.to_relevant_quarter(configuration ->> 'groupHint')
                      IS DISTINCT FROM configuration ->> 'groupHint';

                UPDATE form_fields
                SET configuration = jsonb_set(
                        configuration,
                        '{groupLabel}',
                        to_jsonb(pg_temp.to_relevant_quarter(configuration ->> 'groupLabel'))),
                    updated_at = NOW()
                WHERE jsonb_typeof(configuration -> 'groupLabel') = 'string'
                  AND pg_temp.to_relevant_quarter(configuration ->> 'groupLabel')
                      IS DISTINCT FROM configuration ->> 'groupLabel';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally a no-op.
            //
            // This migration is not reversible, and attempting to reverse it would be destructive:
            //
            //   1. The dropped date ranges ("..., from 1 October to 31 December 2026") cannot be
            //      reconstructed - the quarter they referred to is not recorded in the text.
            //   2. More importantly, a blind reverse replacement would corrupt correct copy. Around
            //      twenty seeded strings already read "the relevant quarter" and never said
            //      "3 months"; rewriting every "relevant quarter" back would silently damage them.
            //
            // Rolling this migration back therefore leaves the corrected copy in place, which is
            // the safe outcome - the wording is valid regardless of schema version.
        }
    }
}