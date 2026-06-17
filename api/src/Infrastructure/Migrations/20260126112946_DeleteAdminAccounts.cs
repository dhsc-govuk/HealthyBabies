using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class DeleteAdminAccounts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
                DELETE FROM users 
                WHERE email IN (
                    'jane.hodges@newham.gov.uk',
                    'naomi.townson@rotherham.gov.uk',
                    'zuzana.roskova@camden.gov.uk',
                    'rachel.sykes@northlincs.gov.uk',
                    'nicola.willsher@kent.gov.uk'
                )
            ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}