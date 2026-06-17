using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class RemoveGlobalDataEntityKeyFromSiteFormQuestions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "global_data_entity_key",
            table: "site_form_questions");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "global_data_entity_key",
            table: "site_form_questions",
            type: "varchar(100)",
            nullable: true);
    }
}