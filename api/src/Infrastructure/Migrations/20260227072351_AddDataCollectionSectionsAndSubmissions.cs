using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddDataCollectionSectionsAndSubmissions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "data_collection_sections",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                data_collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                form_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                section_number = table.Column<int>(type: "integer", nullable: false),
                title = table.Column<string>(type: "varchar(255)", nullable: false),
                description = table.Column<string>(type: "varchar(1000)", nullable: true),
                display_order = table.Column<int>(type: "integer", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_data_collection_sections", x => x.id);
                table.ForeignKey(
                    name: "fk_data_collection_sections_data_collections",
                    column: x => x.data_collection_id,
                    principalTable: "data_collections",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_data_collection_sections_form_definitions",
                    column: x => x.form_definition_id,
                    principalTable: "form_definitions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_data_collection_sections_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_data_collection_sections_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "data_collection_submissions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                data_collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                organisation_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                submitted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                reviewed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                review_notes = table.Column<string>(type: "varchar(2000)", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_data_collection_submissions", x => x.id);
                table.ForeignKey(
                    name: "fk_data_collection_submissions_data_collections",
                    column: x => x.data_collection_id,
                    principalTable: "data_collections",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_data_collection_submissions_organisations",
                    column: x => x.organisation_id,
                    principalTable: "organisations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_data_collection_submissions_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_data_collection_submissions_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_data_collection_submissions_users_reviewed_by",
                    column: x => x.reviewed_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_data_collection_submissions_users_submitted_by",
                    column: x => x.submitted_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "data_collection_section_submissions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                data_collection_submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                data_collection_section_id = table.Column<Guid>(type: "uuid", nullable: false),
                form_submission_id = table.Column<Guid>(type: "uuid", nullable: true),
                status = table.Column<int>(type: "integer", nullable: false),
                last_saved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_data_collection_section_submissions", x => x.id);
                table.ForeignKey(
                    name: "fk_data_collection_section_submissions_form_submissions",
                    column: x => x.form_submission_id,
                    principalTable: "form_submissions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_data_collection_section_submissions_sections",
                    column: x => x.data_collection_section_id,
                    principalTable: "data_collection_sections",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_data_collection_section_submissions_submissions",
                    column: x => x.data_collection_submission_id,
                    principalTable: "data_collection_submissions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_data_collection_section_submissions_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_data_collection_section_submissions_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_section_submissions_created_by_id",
            table: "data_collection_section_submissions",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_section_submissions_data_collection_section",
            table: "data_collection_section_submissions",
            column: "data_collection_section_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_section_submissions_form_submission_id",
            table: "data_collection_section_submissions",
            column: "form_submission_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_section_submissions_last_modified_by_id",
            table: "data_collection_section_submissions",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_section_submissions_submission_section",
            table: "data_collection_section_submissions",
            columns: new[] { "data_collection_submission_id", "data_collection_section_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_sections_created_by_id",
            table: "data_collection_sections",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_sections_data_collection_section_number",
            table: "data_collection_sections",
            columns: new[] { "data_collection_id", "section_number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_sections_form_definition_id",
            table: "data_collection_sections",
            column: "form_definition_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_sections_last_modified_by_id",
            table: "data_collection_sections",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_submissions_created_by_id",
            table: "data_collection_submissions",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_submissions_data_collection_organisation",
            table: "data_collection_submissions",
            columns: new[] { "data_collection_id", "organisation_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_submissions_last_modified_by_id",
            table: "data_collection_submissions",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_submissions_organisation_id",
            table: "data_collection_submissions",
            column: "organisation_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_submissions_reviewed_by_id",
            table: "data_collection_submissions",
            column: "reviewed_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_submissions_submitted_by_id",
            table: "data_collection_submissions",
            column: "submitted_by_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "data_collection_section_submissions");

        migrationBuilder.DropTable(
            name: "data_collection_sections");

        migrationBuilder.DropTable(
            name: "data_collection_submissions");
    }
}