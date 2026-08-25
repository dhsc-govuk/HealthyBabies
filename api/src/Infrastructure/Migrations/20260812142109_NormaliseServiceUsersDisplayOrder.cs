using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormaliseServiceUsersDisplayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BLUM-7219. On the Service Users "check your answers" screen the EBI questions
            // (QSU18-20) are scattered through the list instead of sitting together at the end,
            // before the clarification comments.
            //
            // form_fields.display_order is a single sequence per module, but nothing keeps it
            // monotonic: CreateFormFieldCommand hands a new question the module-wide max + 1
            // whatever section it is filed under, the admin edit screen lets the number be typed
            // in freehand, and admin drag-and-drop used to renumber one section 1..n, colliding
            // with every other section. The wizard survives all of that because the API sorts by
            // section first, but the review screens flatten the module into one list, so the
            // moment display_order stops agreeing with section order the questions interleave.
            // The client fix teaches those screens to sort by section then display order; this
            // migration repairs the numbers themselves so both orderings agree again.
            //
            // Data-only - no schema change. Answers live in form_field_values keyed by
            // form_fields.id, so renumbering is invisible to them: nothing is inserted, deleted or
            // re-keyed, and form_section_id is deliberately never written, so no question changes
            // step and the data-entry flow LAs are part-way through is untouched. The
            // display_order copied onto form_field_values by the answer snapshot is never read for
            // sorting (exports order by the live field), so stale snapshot values stay inert.
            //
            // The renumbering orders by:
            //   1. section number - COALESCE(..., 0) mirrors the API's `?? 0`, so sections keep
            //      their existing grouping and relative order;
            //   2. a within-section rank: ordinary questions (0), then EBI questions (1), then
            //      clarification comments (2). Ranking clarification last means the EBI block
            //      lands before it whichever section the EBI questions turn out to occupy;
            //   3. the existing display_order, then created_at, then field_key, so questions that
            //      share a rank keep the order admins already see and the result is deterministic.
            //
            // created_at rather than field_key decides ties because alphabetical order is actively
            // misleading for the grouped questions: the QSU20 attendance boxes sort high, low, mid
            // by key but were created - and are meant to read - low, mid, high. Insertion order is
            // the better proxy for intent, and field_key only settles rows created in the same
            // transaction.
            //
            // What the tie-break cannot do is un-interleave questions that already carry a
            // duplicate display_order. Live databases hold several inside section 3 - the exported
            // form has QSU10_decile9 and QSU12_UNKNOWN both on 24, QSU12_female and QSU13_11 both
            // on 29 - and because display_order is the primary sort key, both members of a tied
            // pair sit between the same two neighbours no matter how the tie is broken. QSU12_UNKNOWN
            // renders between "IDACI Decile 9" and "IDACI Decile 10" before this migration and still
            // does afterwards; that is a data-quality problem in those legacy rows, not something
            // renumbering introduces or can repair. Fixing it means grouping on
            // configuration->>'group', which is a separate change.
            //
            // QSU18-20 exist only in the deployed databases - they were added through the admin
            // API and are in no seeder or migration here - so they are matched by key pattern with
            // label fallbacks rather than by an enumerated list. The fallbacks are paired with a
            // field_type check because the label patterns are substrings: on their own, an ordinary
            // question worded "...average session attendance rate..." would be promoted, and any
            // question mentioning "clarification" would be pushed to the end of its section. If
            // none of the matchers hit, the migration still collapses display_order into a dense
            // module-wide sequence, which on its own repairs cross-section interleaving.
            //
            // Idempotent: the UPDATE only touches rows whose number actually changes, so a second
            // run is a no-op.
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    module_id UUID;
                BEGIN
                    SELECT id INTO module_id FROM data_collection_form_modules WHERE code = 'service-users';

                    IF module_id IS NULL THEN
                        RETURN;
                    END IF;

                    WITH ordered AS (
                        SELECT ff.id,
                               ROW_NUMBER() OVER (
                                   ORDER BY COALESCE(fs.section_number, 0),
                                            CASE
                                                WHEN ff.field_key = 'QSU17'
                                                  OR (ff.field_type ILIKE 'textarea' AND ff.label ILIKE '%clarification%') THEN 2
                                                WHEN ff.field_key ~* '^QSU(18|19|20)([^0-9].*)?$'
                                                  OR (ff.field_type ILIKE 'radio' AND ff.label ILIKE '%evidence based intervention%')
                                                  OR (ff.field_type ILIKE 'number' AND ff.label ILIKE '%attendance rate%') THEN 1
                                                ELSE 0
                                            END,
                                            ff.display_order,
                                            ff.created_at,
                                            ff.field_key) AS new_order
                        FROM form_fields ff
                        LEFT JOIN form_sections fs ON fs.id = ff.form_section_id
                        WHERE ff.form_module_id = module_id
                    )
                    UPDATE form_fields ff
                    SET display_order = ordered.new_order,
                        updated_at = NOW()
                    FROM ordered
                    WHERE ff.id = ordered.id
                      AND ff.display_order IS DISTINCT FROM ordered.new_order;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty. Up() collapses whatever arbitrary display_order integers a
            // module had accumulated into a dense sequence; the originals are not recorded
            // anywhere and restoring them would only reinstate BLUM-7219. Rolling this migration
            // back leaves the normalised ordering in place, which is valid for every earlier
            // migration too.
        }
    }
}