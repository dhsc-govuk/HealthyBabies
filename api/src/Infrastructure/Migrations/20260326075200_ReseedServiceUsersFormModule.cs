using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class ReseedServiceUsersFormModule : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Delete existing service-users form module data to allow clean reseed
        // This will clear all service user form submissions

        // First, delete form submissions for service-users module
        migrationBuilder.Sql(@"
            DELETE FROM form_submissions
            WHERE form_module_id IN (
                SELECT id FROM data_collection_form_modules WHERE code = 'service-users'
            );
        ");

        // Delete form field options for service-users module (fields have direct form_module_id)
        migrationBuilder.Sql(@"
            DELETE FROM form_field_options
            WHERE form_field_id IN (
                SELECT id FROM form_fields
                WHERE form_module_id IN (
                    SELECT id FROM data_collection_form_modules WHERE code = 'service-users'
                )
            );
        ");

        // Delete form fields for service-users module
        migrationBuilder.Sql(@"
            DELETE FROM form_fields
            WHERE form_module_id IN (
                SELECT id FROM data_collection_form_modules WHERE code = 'service-users'
            );
        ");

        // Delete form sections for service-users module
        migrationBuilder.Sql(@"
            DELETE FROM form_sections
            WHERE form_module_id IN (
                SELECT id FROM data_collection_form_modules WHERE code = 'service-users'
            );
        ");

        // Delete form module assignments for service-users
        migrationBuilder.Sql(@"
            DELETE FROM data_collection_form_module_assignments
            WHERE form_module_id IN (
                SELECT id FROM data_collection_form_modules WHERE code = 'service-users'
            );
        ");

        // Delete the service-users form module itself
        migrationBuilder.Sql(@"
            DELETE FROM data_collection_form_modules WHERE code = 'service-users';
        ");

        // The seeder will recreate the module with correct structure on next application startup
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Data will be re-seeded on application startup
    }
}