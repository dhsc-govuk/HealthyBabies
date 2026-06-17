using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class InsertSMD19Question : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Insert SMD19 question into service_form_questions table
        // SMD19 is conditional on SMD17 containing 'some_hub_sites' or 'some_network_sites'
        migrationBuilder.Sql(@"
            INSERT INTO service_form_questions (
                id, code, label, hint, placeholder, question_type, step, display_order,
                is_required, is_predefined, help_text_summary, help_text,
                conditional_question_code, conditional_value, is_active,
                created_at, created_by_id, updated_at, last_modified_by_id
            )
            SELECT
                gen_random_uuid(),
                'SMD19',
                'Which sites offer the service?',
                'Select all sites that apply',
                NULL,
                4, -- Checkbox type
                2, -- Step 2
                13, -- Display order (after SMD18 at 12)
                false, -- Not required
                false, -- Not predefined
                NULL,
                'Select the Family Hub or Network sites where this service is offered. Options are populated from your organisation''s registered locations.',
                'SMD17',
                'some_hub_sites,some_network_sites',
                true,
                NOW(),
                NULL,
                NOW(),
                NULL
            WHERE NOT EXISTS (
                SELECT 1 FROM service_form_questions WHERE code = 'SMD19'
            );
        ");

        // Update SMD17 options - delete old options and insert new ones
        migrationBuilder.Sql(@"
            -- First, get the SMD17 question ID
            DO $$
            DECLARE
                smd17_id UUID;
            BEGIN
                SELECT id INTO smd17_id FROM service_form_questions WHERE code = 'SMD17';
                
                IF smd17_id IS NOT NULL THEN
                    -- Delete existing options for SMD17
                    DELETE FROM service_form_question_options WHERE question_id = smd17_id;
                    
                    -- Insert new options for SMD17
                    INSERT INTO service_form_question_options (id, question_id, value, label, display_order, created_at)
                    VALUES
                        (gen_random_uuid(), smd17_id, 'all_hub_sites', 'All hub sites', 1, NOW()),
                        (gen_random_uuid(), smd17_id, 'some_hub_sites', 'Some hub sites', 2, NOW()),
                        (gen_random_uuid(), smd17_id, 'all_network_sites', 'All network sites', 3, NOW()),
                        (gen_random_uuid(), smd17_id, 'some_network_sites', 'Some network sites', 4, NOW()),
                        (gen_random_uuid(), smd17_id, 'home_visits', 'Home visits', 5, NOW()),
                        (gen_random_uuid(), smd17_id, 'early_years_setting', 'Early years setting', 6, NOW()),
                        (gen_random_uuid(), smd17_id, 'primary_school', 'Primary school', 7, NOW()),
                        (gen_random_uuid(), smd17_id, 'hospital', 'Hospital', 8, NOW()),
                        (gen_random_uuid(), smd17_id, 'virtual', 'Virtual', 9, NOW()),
                        (gen_random_uuid(), smd17_id, 'other', 'Other', 10, NOW());
                END IF;
            END $$;
        ");

        // Update display orders for SMD20, SMD21 to accommodate SMD19
        // SMD18 stays at 12, SMD19 is at 13, so SMD20 and SMD21 need to shift
        migrationBuilder.Sql(@"
            UPDATE service_form_questions SET display_order = 14 WHERE code = 'SMD20';
            UPDATE service_form_questions SET display_order = 15 WHERE code = 'SMD21';
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Delete SMD19 question
        migrationBuilder.Sql(@"
            DELETE FROM service_form_questions WHERE code = 'SMD19';
        ");

        // Revert SMD17 options to original values
        migrationBuilder.Sql(@"
            DO $$
            DECLARE
                smd17_id UUID;
            BEGIN
                SELECT id INTO smd17_id FROM service_form_questions WHERE code = 'SMD17';
                
                IF smd17_id IS NOT NULL THEN
                    -- Delete new options
                    DELETE FROM service_form_question_options WHERE question_id = smd17_id;
                    
                    -- Insert original options
                    INSERT INTO service_form_question_options (id, question_id, value, label, display_order, created_at)
                    VALUES
                        (gen_random_uuid(), smd17_id, '0', 'All Family Hubs sites', 1, NOW()),
                        (gen_random_uuid(), smd17_id, '1', 'Family Hub Network site', 2, NOW()),
                        (gen_random_uuid(), smd17_id, '2', 'Hospital', 3, NOW()),
                        (gen_random_uuid(), smd17_id, '3', 'Early years setting', 4, NOW()),
                        (gen_random_uuid(), smd17_id, '4', 'Home visits', 5, NOW()),
                        (gen_random_uuid(), smd17_id, '5', 'Virtual', 6, NOW()),
                        (gen_random_uuid(), smd17_id, '6', 'Other locations in LA', 7, NOW()),
                        (gen_random_uuid(), smd17_id, '7', 'Other', 8, NOW());
                END IF;
            END $$;
        ");

        // Revert display orders
        migrationBuilder.Sql(@"
            UPDATE service_form_questions SET display_order = 13 WHERE code = 'SMD20';
            UPDATE service_form_questions SET display_order = 14 WHERE code = 'SMD21';
        ");
    }
}