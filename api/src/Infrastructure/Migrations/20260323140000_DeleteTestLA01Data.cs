using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class DeleteTestLA01Data : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            DELETE FROM form_field_values 
            WHERE form_submission_id IN (
                SELECT id FROM form_submissions 
                WHERE organisation_id = (SELECT id FROM organisations WHERE name = 'TestLA01')
            );
        ");

        migrationBuilder.Sql(@"
            DELETE FROM form_submission_histories 
            WHERE form_submission_id IN (
                SELECT id FROM form_submissions 
                WHERE organisation_id = (SELECT id FROM organisations WHERE name = 'TestLA01')
            );
        ");

        migrationBuilder.Sql(@"
            DELETE FROM form_submissions 
            WHERE organisation_id = (SELECT id FROM organisations WHERE name = 'TestLA01');
        ");

        migrationBuilder.Sql(@"
            DELETE FROM data_collection_submissions 
            WHERE organisation_id = (SELECT id FROM organisations WHERE name = 'TestLA01');
        ");

        migrationBuilder.Sql(@"
            DELETE FROM service_category_answers 
            WHERE service_id IN (
                SELECT id FROM services 
                WHERE organisation_id = (SELECT id FROM organisations WHERE name = 'TestLA01')
            );
        ");

        migrationBuilder.Sql(@"
            DELETE FROM service_answers 
            WHERE service_id IN (
                SELECT id FROM services 
                WHERE organisation_id = (SELECT id FROM organisations WHERE name = 'TestLA01')
            );
        ");

        migrationBuilder.Sql(@"
            DELETE FROM site_answers 
            WHERE location_id IN (
                SELECT id FROM locations 
                WHERE organisation_id = (SELECT id FROM organisations WHERE name = 'TestLA01')
            );
        ");

        migrationBuilder.Sql(@"
            DELETE FROM services 
            WHERE organisation_id = (SELECT id FROM organisations WHERE name = 'TestLA01');
        ");

        migrationBuilder.Sql(@"
            DELETE FROM locations 
            WHERE organisation_id = (SELECT id FROM organisations WHERE name = 'TestLA01');
        ");

        migrationBuilder.Sql(@"
            DELETE FROM data_collection_local_authorities 
            WHERE organisation_id = (SELECT id FROM organisations WHERE name = 'TestLA01');
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}