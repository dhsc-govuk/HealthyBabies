using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class LinkSectionsToFormModules : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_data_collection_sections_data_collections",
            table: "data_collection_sections");

        migrationBuilder.RenameColumn(
            name: "data_collection_id",
            table: "data_collection_sections",
            newName: "form_module_id");

        migrationBuilder.RenameIndex(
            name: "ix_data_collection_sections_data_collection_section_number",
            table: "data_collection_sections",
            newName: "ix_data_collection_sections_form_module_section_number");

        migrationBuilder.AddForeignKey(
            name: "fk_data_collection_sections_form_modules",
            table: "data_collection_sections",
            column: "form_module_id",
            principalTable: "data_collection_form_modules",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_data_collection_sections_form_modules",
            table: "data_collection_sections");

        migrationBuilder.RenameColumn(
            name: "form_module_id",
            table: "data_collection_sections",
            newName: "data_collection_id");

        migrationBuilder.RenameIndex(
            name: "ix_data_collection_sections_form_module_section_number",
            table: "data_collection_sections",
            newName: "ix_data_collection_sections_data_collection_section_number");

        migrationBuilder.AddForeignKey(
            name: "fk_data_collection_sections_data_collections",
            table: "data_collection_sections",
            column: "data_collection_id",
            principalTable: "data_collections",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }
}