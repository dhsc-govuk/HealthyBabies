using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InsertQSU13AAgeQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adds the "Service users by age" gate (QSU13A) and its per-age-band breakdown fields to the
            // already-live service-users module. The seeder only creates this module from scratch if it's
            // entirely missing, so a live module needs an explicit data migration to gain new fields.
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    module_id UUID;
                BEGIN
                    SELECT id INTO module_id FROM data_collection_form_modules WHERE code = 'service-users';

                    IF module_id IS NOT NULL THEN
                        INSERT INTO form_fields (
                            id, form_module_id, field_key, label, field_type, display_order,
                            is_required, conditional_rules, is_active, created_at
                        )
                        SELECT
                            gen_random_uuid(), module_id, 'QSU13A',
                            'Do you have data on how many people used this service in the relevant quarter, by age group?',
                            'radio', 30, true,
                            '{""showWhen"":{""allOf"":[{""fieldKey"":""QSU01"",""equals"":""yes""},{""fieldKey"":""QSU02"",""equals"":""yes""}]}}',
                            true, NOW()
                        WHERE NOT EXISTS (
                            SELECT 1 FROM form_fields WHERE form_module_id = module_id AND field_key = 'QSU13A'
                        );

                        INSERT INTO form_field_options (id, form_field_id, value, label, display_order, created_at)
                        SELECT gen_random_uuid(), ff.id, o.value, o.label, o.display_order, NOW()
                        FROM form_fields ff
                        CROSS JOIN (VALUES ('yes', 'Yes', 1), ('no', 'No', 2)) AS o(value, label, display_order)
                        WHERE ff.form_module_id = module_id AND ff.field_key = 'QSU13A'
                          AND NOT EXISTS (
                              SELECT 1 FROM form_field_options fo WHERE fo.form_field_id = ff.id AND fo.value = o.value
                          );

                        INSERT INTO form_fields (
                            id, form_module_id, field_key, label, field_type, display_order,
                            is_required, placeholder, conditional_rules, configuration, is_active, created_at
                        )
                        SELECT
                            gen_random_uuid(), module_id, v.field_key, v.label, 'number', v.display_order,
                            false, 'parents/carers',
                            '{""showWhen"":{""fieldKey"":""QSU13A"",""equals"":""yes""},""displayInline"":true,""parentOption"":""yes""}',
                            v.configuration::jsonb, true, NOW()
                        FROM (VALUES
                            ('QSU13A_0_2', '0-2', 31, '{""group"":""QSU13A"",""groupLabel"":""How many parents or carers have used this service, broken down by age?"",""groupHint"":""For each age group, give a figure for the last 3 months, from 1 October to 31 December 2026."",""suffix"":""parents/carers"",""width"":""5"",""sumGroup"":""age"",""maxSumField"":""QSU03"",""min"":0}'),
                            ('QSU13A_3_4', '3-4', 32, '{""group"":""QSU13A"",""suffix"":""parents/carers"",""width"":""5"",""sumGroup"":""age"",""maxSumField"":""QSU03"",""min"":0}'),
                            ('QSU13A_5_11', '5-11', 33, '{""group"":""QSU13A"",""suffix"":""parents/carers"",""width"":""5"",""sumGroup"":""age"",""maxSumField"":""QSU03"",""min"":0}'),
                            ('QSU13A_12_17', '12-17', 34, '{""group"":""QSU13A"",""suffix"":""parents/carers"",""width"":""5"",""sumGroup"":""age"",""maxSumField"":""QSU03"",""min"":0}'),
                            ('QSU13A_18_25', '18-25', 35, '{""group"":""QSU13A"",""suffix"":""parents/carers"",""width"":""5"",""sumGroup"":""age"",""maxSumField"":""QSU03"",""min"":0}'),
                            ('QSU13A_26_plus', '26 and older', 36, '{""group"":""QSU13A"",""suffix"":""parents/carers"",""width"":""5"",""sumGroup"":""age"",""maxSumField"":""QSU03"",""min"":0}'),
                            ('QSU13A_unknown', 'Unknown', 37, '{""group"":""QSU13A"",""suffix"":""parents/carers"",""width"":""5"",""sumGroup"":""age"",""maxSumField"":""QSU03"",""min"":0}')
                        ) AS v(field_key, label, display_order, configuration)
                        WHERE NOT EXISTS (
                            SELECT 1 FROM form_fields WHERE form_module_id = module_id AND field_key = v.field_key
                        );

                        -- Shift QSU13 onward to make room for the new age section (30-37).
                        UPDATE form_fields SET display_order = 38 WHERE form_module_id = module_id AND field_key = 'QSU13';
                        UPDATE form_fields SET display_order = 39 WHERE form_module_id = module_id AND field_key = 'QSU14';
                        UPDATE form_fields SET display_order = 40 WHERE form_module_id = module_id AND field_key = 'QSU15';
                        UPDATE form_fields SET display_order = 41 WHERE form_module_id = module_id AND field_key = 'QSU16';
                        UPDATE form_fields SET display_order = 42 WHERE form_module_id = module_id AND field_key = 'QSU17';
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
                        UPDATE form_fields SET display_order = 30 WHERE form_module_id = module_id AND field_key = 'QSU13';
                        UPDATE form_fields SET display_order = 31 WHERE form_module_id = module_id AND field_key = 'QSU14';
                        UPDATE form_fields SET display_order = 32 WHERE form_module_id = module_id AND field_key = 'QSU15';
                        UPDATE form_fields SET display_order = 33 WHERE form_module_id = module_id AND field_key = 'QSU16';
                        UPDATE form_fields SET display_order = 34 WHERE form_module_id = module_id AND field_key = 'QSU17';

                        -- form_field_values -> form_fields FK is RESTRICT, so delete values before fields.
                        DELETE FROM form_field_values
                        WHERE form_field_id IN (
                            SELECT id FROM form_fields
                            WHERE form_module_id = module_id
                              AND field_key IN ('QSU13A', 'QSU13A_0_2', 'QSU13A_3_4', 'QSU13A_5_11', 'QSU13A_12_17', 'QSU13A_18_25', 'QSU13A_26_plus', 'QSU13A_unknown')
                        );

                        DELETE FROM form_fields
                        WHERE form_module_id = module_id
                          AND field_key IN ('QSU13A', 'QSU13A_0_2', 'QSU13A_3_4', 'QSU13A_5_11', 'QSU13A_12_17', 'QSU13A_18_25', 'QSU13A_26_plus', 'QSU13A_unknown');
                    END IF;
                END $$;
            ");
        }
    }
}