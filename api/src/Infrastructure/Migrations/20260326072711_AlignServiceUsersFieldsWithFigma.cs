using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignServiceUsersFieldsWithFigma : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure QSU03-QSU06 are in Section 2 (Step 2) and have NO displayInline styling
            // These fields should display as normal questions without grey borders
            migrationBuilder.Sql(@"
                UPDATE form_fields ff
                SET form_section_id = (
                    SELECT fs.id FROM form_sections fs
                    JOIN data_collection_form_modules dcfm ON fs.form_module_id = dcfm.id
                    WHERE dcfm.code = 'service-users' AND fs.section_number = 2
                ),
                conditional_rules = '{""showWhen"":{""allOf"":[{""fieldKey"":""QSU01"",""equals"":""yes""},{""fieldKey"":""QSU02"",""equals"":""yes""}]}}'
                WHERE ff.field_key IN ('QSU03', 'QSU04', 'QSU05', 'QSU06')
                AND EXISTS (
                    SELECT 1 FROM form_sections fs
                    JOIN data_collection_form_modules dcfm ON fs.form_module_id = dcfm.id
                    WHERE dcfm.code = 'service-users' AND ff.form_section_id = fs.id
                );
            ");

            // Ensure QSU07, QSU09, QSU11, QSU13, QSU15 are in Section 3 (Step 3)
            migrationBuilder.Sql(@"
                UPDATE form_fields ff
                SET form_section_id = (
                    SELECT fs.id FROM form_sections fs
                    JOIN data_collection_form_modules dcfm ON fs.form_module_id = dcfm.id
                    WHERE dcfm.code = 'service-users' AND fs.section_number = 3
                )
                WHERE ff.field_key IN ('QSU07', 'QSU09', 'QSU11', 'QSU13', 'QSU15')
                AND EXISTS (
                    SELECT 1 FROM form_sections fs
                    JOIN data_collection_form_modules dcfm ON fs.form_module_id = dcfm.id
                    WHERE dcfm.code = 'service-users' AND ff.form_section_id = fs.id
                );
            ");

            // Ensure QSU17 is in Section 4 (Step 4)
            migrationBuilder.Sql(@"
                UPDATE form_fields ff
                SET form_section_id = (
                    SELECT fs.id FROM form_sections fs
                    JOIN data_collection_form_modules dcfm ON fs.form_module_id = dcfm.id
                    WHERE dcfm.code = 'service-users' AND fs.section_number = 4
                )
                WHERE ff.field_key = 'QSU17'
                AND EXISTS (
                    SELECT 1 FROM form_sections fs
                    JOIN data_collection_form_modules dcfm ON fs.form_module_id = dcfm.id
                    WHERE dcfm.code = 'service-users' AND ff.form_section_id = fs.id
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}