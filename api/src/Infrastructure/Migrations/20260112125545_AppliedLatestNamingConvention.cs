using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AppliedLatestNamingConvention : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_data_source_items_data_sources_data_source_temp_id",
            table: "data_source_items");

        migrationBuilder.DropForeignKey(
            name: "fk_form_field_options_form_fields_form_field_id1",
            table: "form_field_options");

        migrationBuilder.DropForeignKey(
            name: "fk_form_field_values_form_submissions_form_submission_temp_id",
            table: "form_field_values");

        migrationBuilder.DropForeignKey(
            name: "fk_form_fields_form_versions_form_version_temp_id",
            table: "form_fields");

        migrationBuilder.DropForeignKey(
            name: "fk_form_sections_form_versions_form_version_temp_id1",
            table: "form_sections");

        migrationBuilder.DropForeignKey(
            name: "fk_form_versions_form_definitions_form_definition_temp_id",
            table: "form_versions");

        migrationBuilder.AddForeignKey(
            name: "fk_data_source_items_data_sources_data_source_id",
            table: "data_source_items",
            column: "data_source_id",
            principalTable: "data_sources",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "fk_form_field_options_form_fields_form_field_id",
            table: "form_field_options",
            column: "form_field_id",
            principalTable: "form_fields",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "fk_form_field_values_form_submissions_form_submission_id",
            table: "form_field_values",
            column: "form_submission_id",
            principalTable: "form_submissions",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "fk_form_fields_form_versions_form_version_id",
            table: "form_fields",
            column: "form_version_id",
            principalTable: "form_versions",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "fk_form_sections_form_versions_form_version_id",
            table: "form_sections",
            column: "form_version_id",
            principalTable: "form_versions",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "fk_form_versions_form_definitions_form_definition_id",
            table: "form_versions",
            column: "form_definition_id",
            principalTable: "form_definitions",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_data_source_items_data_sources_data_source_id",
            table: "data_source_items");

        migrationBuilder.DropForeignKey(
            name: "fk_form_field_options_form_fields_form_field_id",
            table: "form_field_options");

        migrationBuilder.DropForeignKey(
            name: "fk_form_field_values_form_submissions_form_submission_id",
            table: "form_field_values");

        migrationBuilder.DropForeignKey(
            name: "fk_form_fields_form_versions_form_version_id",
            table: "form_fields");

        migrationBuilder.DropForeignKey(
            name: "fk_form_sections_form_versions_form_version_id",
            table: "form_sections");

        migrationBuilder.DropForeignKey(
            name: "fk_form_versions_form_definitions_form_definition_id",
            table: "form_versions");

        migrationBuilder.AddForeignKey(
            name: "fk_data_source_items_data_sources_data_source_temp_id",
            table: "data_source_items",
            column: "data_source_id",
            principalTable: "data_sources",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "fk_form_field_options_form_fields_form_field_id1",
            table: "form_field_options",
            column: "form_field_id",
            principalTable: "form_fields",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "fk_form_field_values_form_submissions_form_submission_temp_id",
            table: "form_field_values",
            column: "form_submission_id",
            principalTable: "form_submissions",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "fk_form_fields_form_versions_form_version_temp_id",
            table: "form_fields",
            column: "form_version_id",
            principalTable: "form_versions",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "fk_form_sections_form_versions_form_version_temp_id1",
            table: "form_sections",
            column: "form_version_id",
            principalTable: "form_versions",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "fk_form_versions_form_definitions_form_definition_temp_id",
            table: "form_versions",
            column: "form_definition_id",
            principalTable: "form_definitions",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }
}