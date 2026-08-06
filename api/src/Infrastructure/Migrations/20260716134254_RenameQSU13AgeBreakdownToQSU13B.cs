using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameQSU13AgeBreakdownToQSU13B : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Re-home the QSU13 age-breakdown boxes into their own QSU13B_ group. They were originally
            // coded off the gate (QSU13A_*); this keeps them distinct from both the QSU13A "do you have age
            // data?" gate and the bare QSU13 outcome-scores question. This is a rename only: the fields keep
            // their ids, so submitted answers (form_field_values) are preserved. The QSU13A gate they display
            // under (showWhen QSU13A = yes) is intentionally left unchanged.
            //
            // Also places the gate and the age boxes in section 3: 20260708112413_InsertQSU13AAgeQuestions
            // inserted them with no section, so on live DBs they surface under "No Section". Their display
            // orders (QSU13A = 30, QSU13B_* = 31-37) already sit below QSU12 and above QSU13 (38), so once
            // sectioned they read as QSU12 -> QSU13A -> QSU13B -> QSU13.
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    module_id UUID;
                    section3_id UUID;
                BEGIN
                    SELECT id INTO module_id FROM data_collection_form_modules WHERE code = 'service-users';

                    IF module_id IS NOT NULL THEN
                        UPDATE form_fields
                        SET field_key = 'QSU13B_' || substring(field_key FROM 8),
                            configuration = jsonb_set(configuration, '{group}', '""QSU13B""'::jsonb),
                            updated_at = NOW()
                        WHERE form_module_id = module_id
                          AND field_key IN (
                              'QSU13A_0_2', 'QSU13A_3_4', 'QSU13A_5_11', 'QSU13A_12_17',
                              'QSU13A_18_25', 'QSU13A_26_plus', 'QSU13A_unknown'
                          );

                        SELECT id INTO section3_id FROM form_sections
                        WHERE form_module_id = module_id AND section_number = 3;

                        IF section3_id IS NOT NULL THEN
                            UPDATE form_fields
                            SET form_section_id = section3_id, updated_at = NOW()
                            WHERE form_module_id = module_id
                              AND field_key IN (
                                  'QSU13A', 'QSU13B_0_2', 'QSU13B_3_4', 'QSU13B_5_11', 'QSU13B_12_17',
                                  'QSU13B_18_25', 'QSU13B_26_plus', 'QSU13B_unknown'
                              );
                        END IF;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    module_id UUID;
                BEGIN
                    SELECT id INTO module_id FROM data_collection_form_modules WHERE code = 'service-users';

                    IF module_id IS NOT NULL THEN
                        UPDATE form_fields
                        SET field_key = 'QSU13A_' || substring(field_key FROM 8),
                            configuration = jsonb_set(configuration, '{group}', '""QSU13A""'::jsonb),
                            updated_at = NOW()
                        WHERE form_module_id = module_id
                          AND field_key IN (
                              'QSU13B_0_2', 'QSU13B_3_4', 'QSU13B_5_11', 'QSU13B_12_17',
                              'QSU13B_18_25', 'QSU13B_26_plus', 'QSU13B_unknown'
                          );

                        -- Restore the pre-migration state: these fields had no section.
                        UPDATE form_fields
                        SET form_section_id = NULL, updated_at = NOW()
                        WHERE form_module_id = module_id
                          AND field_key IN (
                              'QSU13A', 'QSU13A_0_2', 'QSU13A_3_4', 'QSU13A_5_11', 'QSU13A_12_17',
                              'QSU13A_18_25', 'QSU13A_26_plus', 'QSU13A_unknown'
                          );
                    END IF;
                END $$;
            ");
        }
    }
}