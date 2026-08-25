using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceOverTheRelevantQuarterWithIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BLUM-7544. Migration 20260812125622 replaced the fixed reporting window ("the past
            // 3 months", "the last quarter (1 January to 31 March 2026)") with "the relevant
            // quarter", but deliberately kept whatever preposition preceded it - the rewrite was
            // '\m(for|over|in)\s+the\s+(?:past|last)...' -> '\1 the relevant quarter'. So "over
            // the past 3 months" became "over the relevant quarter". BLUM-7218 and test case
            // TC-1923 specified the literal phrase "in the relevant quarter", and QA raised the
            // Section 1 home page reading "over" instead.
            //
            // This is data-only - no schema change - and it rewrites rows in place rather than
            // reseeding, because production holds real LA submissions and form_field_values rows
            // reference form_fields.id, so field identity must be preserved.
            //
            // Scoped to description columns only. The MI section home pages render
            // data_collection_form_modules.description (Submissions/Module/index.tsx), and the
            // per-section pages render form_sections.description; those are the strings the ticket
            // is about. form_fields.label / help_text / configuration are deliberately left alone:
            // "over"/"for" is the correct preposition in question copy ("How many people used this
            // service over the relevant quarter?", "Give a figure for the relevant quarter."), and
            // rewriting those would damage wording nobody asked to change.
            //
            // The \m word-boundary anchor keeps this off words ending in "over" ("the handover the
            // relevant quarter summary" is left alone); matching is pattern-based rather than an
            // enumerated list of strings because content has drifted between environments.
            //
            // Sentence-initial "Over" is rewritten to "In", not "in", by a separate case-sensitive
            // pass. A single case-insensitive rewrite with a lowercase replacement would turn
            // "Over the relevant quarter, tell us..." into a sentence starting lower-case - and
            // drifted content is exactly what the pattern matching above exists to catch.
            //
            // Idempotent: the WHERE predicates match [Oo] only, so they match exactly what the two
            // rewrites can change and a second run is a no-op. (A case-insensitive ~* would keep
            // re-selecting an all-caps "OVER" row the rewrites never alter, re-bumping
            // last_changed_on on every deploy.)
            migrationBuilder.Sql(@"
                -- Rewrite the module's own description and bump last_changed_on, which is the value
                -- the admin data-collection wizard renders as the module's ""Last changed on"" date;
                -- leaving it stale would hide this content change from the admin assembling the next
                -- collection. The bump also covers modules whose own description is fine but whose
                -- sections are about to be rewritten below, so this statement must run BEFORE that
                -- one - once the children are rewritten the EXISTS predicate no longer matches.
                UPDATE data_collection_form_modules m
                SET description = regexp_replace(
                                      regexp_replace(m.description, '\mOver\s+the\s+relevant\s+quarter', 'In the relevant quarter', 'g'),
                                      '\mover\s+the\s+relevant\s+quarter', 'in the relevant quarter', 'g'),
                    last_changed_on = NOW(),
                    updated_at = NOW()
                WHERE m.description ~ '\m[Oo]ver\s+the\s+relevant\s+quarter'
                   OR EXISTS (
                        SELECT 1 FROM form_sections s
                        WHERE s.form_module_id = m.id
                          AND s.description ~ '\m[Oo]ver\s+the\s+relevant\s+quarter');

                UPDATE form_sections
                SET description = regexp_replace(
                                      regexp_replace(description, '\mOver\s+the\s+relevant\s+quarter', 'In the relevant quarter', 'g'),
                                      '\mover\s+the\s+relevant\s+quarter', 'in the relevant quarter', 'g'),
                    updated_at = NOW()
                WHERE description ~ '\m[Oo]ver\s+the\s+relevant\s+quarter';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally a no-op, for the same reason as migration 20260812125622.
            //
            // "in the relevant quarter" is not exclusive to this migration - Section 4's copy and
            // a number of seeded question strings already read that way beforehand. A blind reverse
            // replacement would rewrite those to "over the relevant quarter" and silently damage
            // copy this migration never touched.
            //
            // Rolling back therefore leaves the corrected wording in place, which is the safe
            // outcome - it is valid regardless of schema version.
        }
    }
}