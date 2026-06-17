using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class RemoveFormVersionAndFormDefinition : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_data_collection_form_modules_form_definitions_form_definiti",
            table: "data_collection_form_modules");

        migrationBuilder.DropForeignKey(
            name: "fk_data_collection_sections_form_definitions",
            table: "data_collection_sections");

        migrationBuilder.DropForeignKey(
            name: "fk_form_fields_form_versions_form_version_id",
            table: "form_fields");

        migrationBuilder.DropForeignKey(
            name: "fk_form_sections_form_versions_form_version_id",
            table: "form_sections");

        migrationBuilder.DropForeignKey(
            name: "fk_form_submissions_form_versions_form_version_id",
            table: "form_submissions");

        migrationBuilder.DropTable(name: "form_versions");
        migrationBuilder.DropTable(name: "form_definitions");

        migrationBuilder.DropIndex(
            name: "ix_data_collection_sections_form_definition_id",
            table: "data_collection_sections");

        migrationBuilder.DropColumn(
            name: "form_definition_id",
            table: "data_collection_sections");

        migrationBuilder.DropIndex(
            name: "ix_data_collection_form_modules_form_definition_id",
            table: "data_collection_form_modules");

        migrationBuilder.DropColumn(
            name: "form_definition_id",
            table: "data_collection_form_modules");

        // Clear orphaned data before renaming columns
        migrationBuilder.Sql("DELETE FROM form_field_values;");
        migrationBuilder.Sql("DELETE FROM form_submission_histories;");
        migrationBuilder.Sql("DELETE FROM form_submissions;");
        migrationBuilder.Sql("DELETE FROM form_field_options;");
        migrationBuilder.Sql("DELETE FROM form_fields;");
        migrationBuilder.Sql("DELETE FROM form_sections;");

        migrationBuilder.RenameColumn(
            name: "form_version_id",
            table: "form_submissions",
            newName: "form_module_id");

        migrationBuilder.RenameIndex(
            name: "ix_form_submissions_form_version_id",
            table: "form_submissions",
            newName: "ix_form_submissions_form_module_id");

        migrationBuilder.RenameColumn(
            name: "form_version_id",
            table: "form_sections",
            newName: "form_module_id");

        migrationBuilder.RenameIndex(
            name: "ix_form_sections_form_version_id_section_number",
            table: "form_sections",
            newName: "ix_form_sections_form_module_id_section_number");

        migrationBuilder.RenameColumn(
            name: "form_version_id",
            table: "form_fields",
            newName: "form_module_id");

        migrationBuilder.RenameIndex(
            name: "ix_form_fields_form_version_id_field_key",
            table: "form_fields",
            newName: "ix_form_fields_form_module_id_field_key");

        migrationBuilder.AddColumn<Guid>(
            name: "published_by_id",
            table: "data_collection_form_modules",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "published_at",
            table: "data_collection_form_modules",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "status",
            table: "data_collection_form_modules",
            type: "character varying(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "draft");

        migrationBuilder.AddColumn<string>(
            name: "validation_schema",
            table: "data_collection_form_modules",
            type: "text",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_form_modules_published_by_id",
            table: "data_collection_form_modules",
            column: "published_by_id");

        migrationBuilder.AddForeignKey(
            name: "fk_data_collection_form_modules_users_published_by_id",
            table: "data_collection_form_modules",
            column: "published_by_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "fk_form_fields_data_collection_form_modules_form_module_id",
            table: "form_fields",
            column: "form_module_id",
            principalTable: "data_collection_form_modules",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "fk_form_sections_data_collection_form_modules_form_module_id",
            table: "form_sections",
            column: "form_module_id",
            principalTable: "data_collection_form_modules",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "fk_form_submissions_form_modules_form_module_id",
            table: "form_submissions",
            column: "form_module_id",
            principalTable: "data_collection_form_modules",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("This migration cannot be reversed.");
    }
}