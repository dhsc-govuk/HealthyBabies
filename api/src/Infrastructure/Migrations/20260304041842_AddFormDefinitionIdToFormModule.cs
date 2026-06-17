using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddFormDefinitionIdToFormModule : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "form_definition_id",
            table: "data_collection_form_modules",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_form_modules_form_definition_id",
            table: "data_collection_form_modules",
            column: "form_definition_id");

        migrationBuilder.AddForeignKey(
            name: "fk_data_collection_form_modules_form_definitions_form_definiti",
            table: "data_collection_form_modules",
            column: "form_definition_id",
            principalTable: "form_definitions",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_data_collection_form_modules_form_definitions_form_definiti",
            table: "data_collection_form_modules");

        migrationBuilder.DropIndex(
            name: "ix_data_collection_form_modules_form_definition_id",
            table: "data_collection_form_modules");

        migrationBuilder.DropColumn(
            name: "form_definition_id",
            table: "data_collection_form_modules");
    }
}