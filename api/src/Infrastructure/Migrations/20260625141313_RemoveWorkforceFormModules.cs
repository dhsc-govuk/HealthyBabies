using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class RemoveWorkforceFormModules : Migration
{
    private static readonly string[] WorkforceCodes = ["workforce-training", "funded-total-workforce"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var code in WorkforceCodes)
        {
            migrationBuilder.Sql($@"
                    DELETE FROM form_submissions
                    WHERE form_module_id IN (
                        SELECT id FROM data_collection_form_modules WHERE code = '{code}'
                    );
                ");

            migrationBuilder.Sql($@"
                    DELETE FROM form_field_options
                    WHERE form_field_id IN (
                        SELECT id FROM form_fields
                        WHERE form_module_id IN (
                            SELECT id FROM data_collection_form_modules WHERE code = '{code}'
                        )
                    );
                ");

            migrationBuilder.Sql($@"
                    DELETE FROM form_fields
                    WHERE form_module_id IN (
                        SELECT id FROM data_collection_form_modules WHERE code = '{code}'
                    );
                ");

            migrationBuilder.Sql($@"
                    DELETE FROM form_sections
                    WHERE form_module_id IN (
                        SELECT id FROM data_collection_form_modules WHERE code = '{code}'
                    );
                ");

            migrationBuilder.Sql($@"
                    DELETE FROM data_collection_form_module_assignments
                    WHERE form_module_id IN (
                        SELECT id FROM data_collection_form_modules WHERE code = '{code}'
                    );
                ");

            migrationBuilder.Sql($@"
                    DELETE FROM data_collection_form_modules WHERE code = '{code}';
                ");
        }
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Modules will be re-seeded on application startup
    }
}