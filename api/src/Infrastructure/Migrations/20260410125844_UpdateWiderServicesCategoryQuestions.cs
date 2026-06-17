using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWiderServicesCategoryQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE service_category_form_questions 
                SET label = 'Do your delivery locations offer any wider services within this category?',
                    hint = NULL,
                    updated_at = NOW()
                WHERE code = 'WSDM01';
            ");

            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    wsdm01_id UUID;
                BEGIN
                    SELECT id INTO wsdm01_id FROM service_category_form_questions WHERE code = 'WSDM01';
                    
                    IF wsdm01_id IS NOT NULL THEN
                        DELETE FROM service_category_form_question_options WHERE question_id = wsdm01_id;
                        
                        INSERT INTO service_category_form_question_options (id, question_id, value, label, display_order, created_at)
                        VALUES
                            (gen_random_uuid(), wsdm01_id, 'yes', 'Yes', 1, NOW()),
                            (gen_random_uuid(), wsdm01_id, 'no', 'No', 2, NOW());
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                UPDATE service_category_form_questions 
                SET label = 'Where are these wider services delivered?',
                    hint = 'Select all delivery locations that apply.',
                    question_type = 4,
                    conditional_question_code = 'WSDM01',
                    conditional_value = 'yes',
                    updated_at = NOW()
                WHERE code = 'WSDM02';
            ");

            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    wsdm02_id UUID;
                BEGIN
                    SELECT id INTO wsdm02_id FROM service_category_form_questions WHERE code = 'WSDM02';
                    
                    IF wsdm02_id IS NOT NULL THEN
                        DELETE FROM service_category_form_question_options WHERE question_id = wsdm02_id;
                        
                        INSERT INTO service_category_form_question_options (id, question_id, value, label, display_order, created_at)
                        VALUES
                            (gen_random_uuid(), wsdm02_id, 'hub_sites', 'Hub site(s)', 1, NOW()),
                            (gen_random_uuid(), wsdm02_id, 'home_visits', 'Home visits', 2, NOW()),
                            (gen_random_uuid(), wsdm02_id, 'early_years_setting', 'Early years setting', 3, NOW()),
                            (gen_random_uuid(), wsdm02_id, 'hospital', 'Hospital', 4, NOW()),
                            (gen_random_uuid(), wsdm02_id, 'virtual', 'Virtual', 5, NOW()),
                            (gen_random_uuid(), wsdm02_id, 'network_sites', 'Network Site(s)', 6, NOW()),
                            (gen_random_uuid(), wsdm02_id, 'other_locations_la', 'Other locations in LA', 7, NOW());
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    wsdm03_id UUID;
                BEGIN
                    SELECT id INTO wsdm03_id FROM service_category_form_questions WHERE code = 'WSDM03';
                    
                    IF wsdm03_id IS NOT NULL THEN
                        DELETE FROM service_category_form_question_options WHERE question_id = wsdm03_id;
                        DELETE FROM service_category_form_questions WHERE id = wsdm03_id;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE service_category_form_questions 
                SET label = 'Is delivery of the ''{widerServiceCategoryName}'' services part of your Start for Life Offer, delivered through the FH Network to meet the objectives of the FH&SfL programme or delivered through the FH Network to meet the minimum or go-further expectations of the FH&SfL programme guide?',
                    hint = NULL,
                    updated_at = NOW()
                WHERE code = 'WSDM01';
            ");

            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    wsdm01_id UUID;
                BEGIN
                    SELECT id INTO wsdm01_id FROM service_category_form_questions WHERE code = 'WSDM01';
                    
                    IF wsdm01_id IS NOT NULL THEN
                        DELETE FROM service_category_form_question_options WHERE question_id = wsdm01_id;
                        
                        INSERT INTO service_category_form_question_options (id, question_id, value, label, display_order, created_at)
                        VALUES
                            (gen_random_uuid(), wsdm01_id, 'yes_all', 'Yes, for all services delivered under this category', 1, NOW()),
                            (gen_random_uuid(), wsdm01_id, 'yes_some', 'Yes, for some services delivered under this category', 2, NOW()),
                            (gen_random_uuid(), wsdm01_id, 'no', 'No', 3, NOW());
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                UPDATE service_category_form_questions 
                SET label = 'Do you provide the ''{widerServiceCategoryName}'' services within your Family Hub Network?',
                    hint = NULL,
                    question_type = 1,
                    conditional_question_code = NULL,
                    conditional_value = NULL,
                    updated_at = NOW()
                WHERE code = 'WSDM02';
            ");

            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    wsdm02_id UUID;
                BEGIN
                    SELECT id INTO wsdm02_id FROM service_category_form_questions WHERE code = 'WSDM02';
                    
                    IF wsdm02_id IS NOT NULL THEN
                        DELETE FROM service_category_form_question_options WHERE question_id = wsdm02_id;
                        
                        INSERT INTO service_category_form_question_options (id, question_id, value, label, display_order, created_at)
                        VALUES
                            (gen_random_uuid(), wsdm02_id, 'yes', 'Yes', 1, NOW()),
                            (gen_random_uuid(), wsdm02_id, 'no', 'No', 2, NOW());
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO service_category_form_questions (
                    id, code, label, hint, placeholder, question_type, step, display_order,
                    is_required, is_predefined, help_text_summary, help_text,
                    conditional_question_code, conditional_value, is_active,
                    created_at, created_by_id, updated_at, last_modified_by_id
                )
                SELECT
                    gen_random_uuid(),
                    'WSDM03',
                    'Where do you provide the ''{widerServiceCategoryName}'' services?',
                    'Select all delivery locations that apply.',
                    NULL,
                    4,
                    1,
                    3,
                    true,
                    true,
                    NULL,
                    NULL,
                    'WSDM02',
                    'yes',
                    true,
                    NOW(),
                    NULL,
                    NOW(),
                    NULL
                WHERE NOT EXISTS (
                    SELECT 1 FROM service_category_form_questions WHERE code = 'WSDM03'
                );
            ");

            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    wsdm03_id UUID;
                BEGIN
                    SELECT id INTO wsdm03_id FROM service_category_form_questions WHERE code = 'WSDM03';
                    
                    IF wsdm03_id IS NOT NULL THEN
                        INSERT INTO service_category_form_question_options (id, question_id, value, label, display_order, created_at)
                        VALUES
                            (gen_random_uuid(), wsdm03_id, 'all_fh_sites', 'All FH sites', 1, NOW()),
                            (gen_random_uuid(), wsdm03_id, 'home_visits', 'Home visits', 2, NOW()),
                            (gen_random_uuid(), wsdm03_id, 'early_years_setting', 'Early years setting', 3, NOW()),
                            (gen_random_uuid(), wsdm03_id, 'hospital', 'Hospital', 4, NOW()),
                            (gen_random_uuid(), wsdm03_id, 'virtual', 'Virtual', 5, NOW()),
                            (gen_random_uuid(), wsdm03_id, 'family_hub_network_site', 'Family Hub Network Site', 6, NOW()),
                            (gen_random_uuid(), wsdm03_id, 'other_locations', 'Other locations in the local authority', 7, NOW());
                    END IF;
                END $$;
            ");
        }
    }
}