using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormaliseServiceUsersDemographicGrouping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BLUM-7222. On the Service Users summary screens the demographic numbers appear
            // between unrelated questions: a sex "Unknown" sits inside the IMD deciles, an age band
            // sits inside the sex breakdown, and the age boxes are interleaved with the sex boxes.
            //
            // Two separate causes, both in the data.
            //
            // 1. Legacy duplicates. Section 3 carries QSU12_UNKNOWN and QSU13_UNKNOWN - uppercase
            //    twins of QSU12_unknown and QSU13B_unknown - plus QSU13_11 ("12 - 17 years"), an age
            //    band from a retired banding. None of them carry configuration->>'group', so nothing
            //    ties them to a breakdown, and their display_order ties them to whatever question
            //    happens to share the number. The QSU13_* age set is a whole superseded breakdown:
            //    20260716134254 replaced it with QSU13B_*, but left it active, so the age question is
            //    asked twice under one gate.
            //
            // 2. Interleaved numbering. 20260708112413 inserted the age boxes at display_order 31-37,
            //    straight through the sex boxes already sitting at 28-32 and the legacy age boxes at
            //    34-39. BLUM-7219's 20260812142109 collapsed the module into a dense sequence but
            //    deliberately preserved relative order, so the interleave survived it. That migration
            //    scoped this repair out in as many words: "Fixing it means grouping on
            //    configuration->>'group', which is a separate change."
            //
            // Data-only - no schema change. Nothing is inserted, deleted or re-keyed: answers live in
            // form_field_values keyed by form_fields.id, so both halves below are invisible to them.
            // Retiring a field is is_active = false, never a DELETE, so any numbers an LA submitted
            // against it stay in the database and the change is reversible from the admin UI.
            // form_section_id is never written, so no question changes step and a data-entry flow an
            // LA is part-way through is untouched.
            //
            // Idempotent throughout: every UPDATE is predicated on the value actually changing, so a
            // second run is a no-op.
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    module_id UUID;
                    retired_count INT := 0;
                    rows_affected INT;
                BEGIN
                    SELECT id INTO module_id FROM data_collection_form_modules WHERE code = 'service-users';

                    IF module_id IS NULL THEN
                        RETURN;
                    END IF;

                    -- Matched by an exact key list rather than a prefix, because QSU13 (the
                    -- outcome-scores radio) and QSU13A (the age gate) share the stem and must stay
                    -- active. A prefix match would silently take the whole age question away.
                    UPDATE form_fields
                    SET is_active = false,
                        updated_at = NOW()
                    WHERE form_module_id = module_id
                      AND field_key IN ('QSU12_UNKNOWN', 'QSU13_UNKNOWN', 'QSU13_11')
                      AND is_active;

                    GET DIAGNOSTICS rows_affected = ROW_COUNT;
                    retired_count := retired_count + rows_affected;

                    -- The superseded age set only goes when its replacement is actually present. A
                    -- database that never received 20260716134254 has no QSU13B_* to fall back on,
                    -- and retiring QSU13_* there would leave the age gate with nothing behind it.
                    IF EXISTS (
                        SELECT 1 FROM form_fields
                        WHERE form_module_id = module_id AND field_key ~ '^QSU13B_'
                    ) THEN
                        UPDATE form_fields
                        SET is_active = false,
                            updated_at = NOW()
                        WHERE form_module_id = module_id
                          AND field_key IN ('QSU13_02', 'QSU13_35', 'QSU13_618', 'QSU13_1925', 'QSU13_26', 'QSU13_unknown')
                          AND is_active;

                        GET DIAGNOSTICS rows_affected = ROW_COUNT;
                        retired_count := retired_count + rows_affected;
                    END IF;

                    -- last_changed_on is what the admin data-collection wizard renders as the
                    -- module's ""Last changed on"" date. Retiring questions changes what the form
                    -- asks, so leaving it stale would hide that from the admin assembling the next
                    -- collection. Guarded on the row count, so a re-run does not keep bumping it.
                    IF retired_count > 0 THEN
                        UPDATE data_collection_form_modules
                        SET last_changed_on = NOW(),
                            updated_at = NOW()
                        WHERE id = module_id;
                    END IF;

                    -- Renumber display_order so a flat sort reproduces the shape the data-entry
                    -- screens already render: each gate question, then its breakdown, one block at a
                    -- time. FormFieldRenderer nests a gate's inline children underneath it and buckets
                    -- them by configuration->>'group'; the summary screens flatten the module into one
                    -- list, so they only agree with that while the numbers happen to line up.
                    --
                    -- Inactive fields are renumbered along with the rest. They are excluded from every
                    -- read path (SubmissionsControllerService, the exports, the bulk-upload template),
                    -- but leaving them out would punch holes in the sequence and hand the next
                    -- inserted question a number that collides with a live one.
                    WITH base AS (
                        SELECT ff.id,
                               ff.field_key,
                               ff.display_order,
                               ff.created_at,
                               ff.field_type,
                               ff.label,
                               COALESCE(fs.section_number, 0) AS section_number,
                               ff.configuration ->> 'group' AS group_key,
                               -- The gate this field hangs off. showWhen.fieldKey covers the simple
                               -- form; for allOf, the last condition naming a field is the immediate
                               -- gate, which is the same rule FormFieldRenderer applies when it
                               -- decides which radio to nest a field under. Conditions keyed on
                               -- serviceFieldKey describe the Service record, not a question here, so
                               -- they are skipped rather than treated as a missing gate.
                               CASE
                                   WHEN ff.conditional_rules -> 'showWhen' ->> 'fieldKey' IS NOT NULL
                                       THEN ff.conditional_rules -> 'showWhen' ->> 'fieldKey'
                                   WHEN jsonb_typeof(ff.conditional_rules -> 'showWhen' -> 'allOf') = 'array'
                                       THEN (
                                           SELECT c.condition ->> 'fieldKey'
                                           FROM jsonb_array_elements(ff.conditional_rules -> 'showWhen' -> 'allOf')
                                                WITH ORDINALITY AS c(condition, position)
                                           WHERE c.condition ->> 'fieldKey' IS NOT NULL
                                           ORDER BY c.position DESC
                                           LIMIT 1)
                               END AS gate_key
                        FROM form_fields ff
                        LEFT JOIN form_sections fs ON fs.id = ff.form_section_id
                        WHERE ff.form_module_id = module_id
                    ),
                    ranked AS (
                        -- BLUM-7219's within-section rank, carried forward unchanged so this
                        -- migration cannot undo it: ordinary questions, then the EBI block, then
                        -- clarification comments. QSU18-20 exist only in the deployed databases, so
                        -- they are matched by key pattern with label fallbacks; each fallback is
                        -- paired with a field_type check because the label patterns are substrings.
                        SELECT b.id,
                               CASE
                                   WHEN b.field_key = 'QSU17'
                                     OR (b.field_type ILIKE 'textarea' AND b.label ILIKE '%clarification%') THEN 2
                                   WHEN b.field_key ~* '^QSU(18|19|20)([^0-9].*)?$'
                                     OR (b.field_type ILIKE 'radio' AND b.label ILIKE '%evidence based intervention%')
                                     OR (b.field_type ILIKE 'number' AND b.label ILIKE '%attendance rate%') THEN 1
                                   ELSE 0
                               END AS section_rank
                        FROM base b
                    ),
                    anchored AS (
                        -- The gate is only honoured inside the same section. The demographic gates
                        -- are themselves conditional on QSU01-QSU03 over in sections 1 and 2; without
                        -- this the whole of section 3 would collapse onto one anchor and the blocks
                        -- would sort by nothing but their old numbers again.
                        SELECT b.id,
                               b.field_key,
                               b.display_order,
                               b.created_at,
                               b.section_number,
                               b.group_key,
                               COALESCE(gate.id, b.id) AS anchor_id,
                               COALESCE(gate.display_order, b.display_order) AS anchor_order,
                               COALESCE(gate.created_at, b.created_at) AS anchor_created_at,
                               COALESCE(gate.field_key, b.field_key) AS anchor_field_key,
                               CASE WHEN gate.id IS NULL THEN 0 ELSE 1 END AS is_child
                        FROM base b
                        LEFT JOIN base gate
                               ON gate.field_key = b.gate_key
                              AND gate.section_number = b.section_number
                              AND gate.id <> b.id
                    ),
                    grouped AS (
                        -- Anchoring a group on its first member is what pulls a breakdown back into
                        -- one block; an ungrouped child keeps its own position among them. This needs
                        -- its own step because Postgres will not nest a window function inside the
                        -- ORDER BY of another one.
                        SELECT a.*,
                               CASE
                                   WHEN a.group_key IS NULL THEN a.display_order
                                   ELSE MIN(a.display_order) OVER (
                                       PARTITION BY a.section_number, a.anchor_id, a.group_key)
                               END AS group_anchor
                        FROM anchored a
                    ),
                    ordered AS (
                        SELECT g.id,
                               ROW_NUMBER() OVER (
                                   ORDER BY g.section_number,
                                            -- Ranked on the anchor, not on the field, so a gate and
                                            -- its children can never be split across two ranks.
                                            anchor_rank.section_rank,
                                            -- These three together identify the anchor (field_key is
                                            -- unique per module), so everything hanging off one gate
                                            -- stays contiguous even where two gates tie on a number.
                                            g.anchor_order,
                                            g.anchor_created_at,
                                            g.anchor_field_key,
                                            g.is_child,
                                            g.group_anchor,
                                            g.display_order,
                                            -- created_at before field_key for the reason BLUM-7219
                                            -- gave: the QSU20 attendance boxes sort high, low, mid
                                            -- alphabetically but were created - and read - low, mid,
                                            -- high. Insertion order is the better proxy for intent.
                                            g.created_at,
                                            g.field_key) AS new_order
                        FROM grouped g
                        JOIN ranked anchor_rank ON anchor_rank.id = g.anchor_id
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
            // Reactivates the questions Up() retired. It cannot tell them apart from questions an
            // admin later disabled by hand through the configuration screen, so rolling back brings
            // those back too - the keys are a fixed list, and nothing records who turned each one off.
            //
            // The renumbering is deliberately not reversed. Up() collapses whatever arbitrary
            // display_order integers the module had accumulated into a dense sequence; the originals
            // are not recorded anywhere and restoring them would only reinstate BLUM-7222. Leaving the
            // normalised ordering in place is valid for every earlier migration too.
            migrationBuilder.Sql(@"
                UPDATE form_fields ff
                SET is_active = true,
                    updated_at = NOW()
                FROM data_collection_form_modules m
                WHERE m.id = ff.form_module_id
                  AND m.code = 'service-users'
                  AND ff.field_key IN (
                      'QSU12_UNKNOWN', 'QSU13_UNKNOWN', 'QSU13_11',
                      'QSU13_02', 'QSU13_35', 'QSU13_618', 'QSU13_1925', 'QSU13_26', 'QSU13_unknown'
                  )
                  AND NOT ff.is_active;
            ");
        }
    }
}