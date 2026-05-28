using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class ClearFormModulesAndAssignments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Delete form submissions that reference form modules
        migrationBuilder.Sql("DELETE FROM form_submissions;");

        // Delete data collection section submissions
        migrationBuilder.Sql("DELETE FROM data_collection_section_submissions;");

        // Delete data collection submissions
        migrationBuilder.Sql("DELETE FROM data_collection_submissions;");

        // Delete form module assignments
        migrationBuilder.Sql("DELETE FROM data_collection_form_module_assignments;");

        // Delete form field options
        migrationBuilder.Sql("DELETE FROM form_field_options;");

        // Delete form fields
        migrationBuilder.Sql("DELETE FROM form_fields;");

        // Delete form sections
        migrationBuilder.Sql("DELETE FROM form_sections;");

        // Delete data collection sections
        migrationBuilder.Sql("DELETE FROM data_collection_sections;");

        // Delete form modules
        migrationBuilder.Sql("DELETE FROM data_collection_form_modules;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Data will be re-seeded on application startup
    }
}