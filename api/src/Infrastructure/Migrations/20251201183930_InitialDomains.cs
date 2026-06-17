using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialDomains : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "data_sources",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "varchar(100)", nullable: false),
                name = table.Column<string>(type: "varchar(255)", nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                is_hierarchical = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_data_sources", x => x.id);
                table.ForeignKey(
                    name: "fk_data_sources_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_data_sources_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "form_definitions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "varchar(100)", nullable: false),
                name = table.Column<string>(type: "varchar(255)", nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_form_definitions", x => x.id);
                table.ForeignKey(
                    name: "fk_form_definitions_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_form_definitions_users_deleted_by_id",
                    column: x => x.deleted_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_form_definitions_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "data_source_items",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                data_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                parent_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                value = table.Column<string>(type: "varchar(255)", nullable: false),
                label = table.Column<string>(type: "varchar(500)", nullable: false),
                display_order = table.Column<int>(type: "integer", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                metadata = table.Column<string>(type: "jsonb", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_data_source_items", x => x.id);
                table.ForeignKey(
                    name: "fk_data_source_items_data_source_items_parent_item_id",
                    column: x => x.parent_item_id,
                    principalTable: "data_source_items",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_data_source_items_data_sources_data_source_temp_id",
                    column: x => x.data_source_id,
                    principalTable: "data_sources",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_data_source_items_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_data_source_items_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "form_versions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                form_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                version_number = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "draft"),
                change_notes = table.Column<string>(type: "text", nullable: true),
                published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                published_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                deprecated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                validation_schema = table.Column<string>(type: "jsonb", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_form_versions", x => x.id);
                table.ForeignKey(
                    name: "fk_form_versions_form_definitions_form_definition_temp_id",
                    column: x => x.form_definition_id,
                    principalTable: "form_definitions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_form_versions_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_form_versions_users_deleted_by_id",
                    column: x => x.deleted_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_form_versions_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_form_versions_users_published_by_id",
                    column: x => x.published_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "form_sections",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                form_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                section_number = table.Column<int>(type: "integer", nullable: false),
                title = table.Column<string>(type: "varchar(255)", nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                help_text = table.Column<string>(type: "varchar(500)", nullable: true),
                help_url = table.Column<string>(type: "varchar(500)", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_form_sections", x => x.id);
                table.ForeignKey(
                    name: "fk_form_sections_form_versions_form_version_temp_id1",
                    column: x => x.form_version_id,
                    principalTable: "form_versions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_form_sections_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_form_sections_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "form_submissions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                form_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "draft"),
                entity_type = table.Column<string>(type: "varchar(100)", nullable: true),
                entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                draft_saved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                submitted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                reviewed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                review_notes = table.Column<string>(type: "text", nullable: true),
                submitter_ip_address = table.Column<string>(type: "varchar(45)", nullable: true),
                submitter_user_agent = table.Column<string>(type: "varchar(500)", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_form_submissions", x => x.id);
                table.ForeignKey(
                    name: "fk_form_submissions_form_versions_form_version_id",
                    column: x => x.form_version_id,
                    principalTable: "form_versions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_form_submissions_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_form_submissions_users_deleted_by_id",
                    column: x => x.deleted_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_form_submissions_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_form_submissions_users_reviewed_by_id",
                    column: x => x.reviewed_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_form_submissions_users_submitted_by_id",
                    column: x => x.submitted_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "form_fields",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                form_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                form_section_id = table.Column<Guid>(type: "uuid", nullable: true),
                field_key = table.Column<string>(type: "varchar(100)", nullable: false),
                label = table.Column<string>(type: "varchar(500)", nullable: false),
                field_type = table.Column<string>(type: "varchar(50)", nullable: false),
                display_order = table.Column<int>(type: "integer", nullable: false),
                is_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                placeholder = table.Column<string>(type: "varchar(500)", nullable: true),
                help_text = table.Column<string>(type: "text", nullable: true),
                default_value = table.Column<string>(type: "text", nullable: true),
                validation_rules = table.Column<string>(type: "jsonb", nullable: true),
                conditional_rules = table.Column<string>(type: "jsonb", nullable: true),
                configuration = table.Column<string>(type: "jsonb", nullable: true),
                parent_field_id = table.Column<Guid>(type: "uuid", nullable: true),
                data_source_id = table.Column<Guid>(type: "uuid", nullable: true),
                data_source_parent_value = table.Column<string>(type: "varchar(255)", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_form_fields", x => x.id);
                table.ForeignKey(
                    name: "fk_form_fields_data_sources_data_source_id",
                    column: x => x.data_source_id,
                    principalTable: "data_sources",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_form_fields_form_fields_parent_field_id",
                    column: x => x.parent_field_id,
                    principalTable: "form_fields",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_form_fields_form_sections_form_section_id",
                    column: x => x.form_section_id,
                    principalTable: "form_sections",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_form_fields_form_versions_form_version_temp_id",
                    column: x => x.form_version_id,
                    principalTable: "form_versions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_form_fields_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_form_fields_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "form_submission_histories",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                form_submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                previous_status = table.Column<string>(type: "varchar(50)", nullable: false),
                new_status = table.Column<string>(type: "varchar(50)", nullable: false),
                notes = table.Column<string>(type: "text", nullable: true),
                changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_form_submission_histories", x => x.id);
                table.ForeignKey(
                    name: "fk_form_submission_histories_form_submissions_form_submission_",
                    column: x => x.form_submission_id,
                    principalTable: "form_submissions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_form_submission_histories_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_form_submission_histories_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "form_field_options",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                form_field_id = table.Column<Guid>(type: "uuid", nullable: false),
                value = table.Column<string>(type: "varchar(255)", nullable: false),
                label = table.Column<string>(type: "varchar(500)", nullable: false),
                display_order = table.Column<int>(type: "integer", nullable: false),
                is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                triggered_field_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_form_field_options", x => x.id);
                table.ForeignKey(
                    name: "fk_form_field_options_form_fields_form_field_id1",
                    column: x => x.form_field_id,
                    principalTable: "form_fields",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_form_field_options_form_fields_triggered_field_id",
                    column: x => x.triggered_field_id,
                    principalTable: "form_fields",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_form_field_options_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_form_field_options_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "form_field_values",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                form_submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                form_field_id = table.Column<Guid>(type: "uuid", nullable: false),
                value = table.Column<string>(type: "text", nullable: true),
                display_value = table.Column<string>(type: "text", nullable: true),
                file_reference = table.Column<string>(type: "varchar(1000)", nullable: true),
                file_name = table.Column<string>(type: "varchar(255)", nullable: true),
                file_size = table.Column<long>(type: "bigint", nullable: true),
                file_mime_type = table.Column<string>(type: "varchar(100)", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_form_field_values", x => x.id);
                table.ForeignKey(
                    name: "fk_form_field_values_form_fields_form_field_id",
                    column: x => x.form_field_id,
                    principalTable: "form_fields",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_form_field_values_form_submissions_form_submission_temp_id",
                    column: x => x.form_submission_id,
                    principalTable: "form_submissions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_form_field_values_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_form_field_values_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_data_source_items_created_by_id",
            table: "data_source_items",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_source_items_data_source_id_parent_item_id_display_ord",
            table: "data_source_items",
            columns: new[] { "data_source_id", "parent_item_id", "display_order" });

        migrationBuilder.CreateIndex(
            name: "ix_data_source_items_data_source_id_value",
            table: "data_source_items",
            columns: new[] { "data_source_id", "value" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_data_source_items_last_modified_by_id",
            table: "data_source_items",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_source_items_parent_item_id",
            table: "data_source_items",
            column: "parent_item_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_sources_code",
            table: "data_sources",
            column: "code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_data_sources_created_by_id",
            table: "data_sources",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_sources_last_modified_by_id",
            table: "data_sources",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_definitions_code",
            table: "form_definitions",
            column: "code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_form_definitions_created_by_id",
            table: "form_definitions",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_definitions_deleted_by_id",
            table: "form_definitions",
            column: "deleted_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_definitions_last_modified_by_id",
            table: "form_definitions",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_field_options_created_by_id",
            table: "form_field_options",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_field_options_form_field_id_display_order",
            table: "form_field_options",
            columns: new[] { "form_field_id", "display_order" });

        migrationBuilder.CreateIndex(
            name: "ix_form_field_options_last_modified_by_id",
            table: "form_field_options",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_field_options_triggered_field_id",
            table: "form_field_options",
            column: "triggered_field_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_field_values_created_by_id",
            table: "form_field_values",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_field_values_form_field_id",
            table: "form_field_values",
            column: "form_field_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_field_values_form_submission_id_form_field_id",
            table: "form_field_values",
            columns: new[] { "form_submission_id", "form_field_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_form_field_values_last_modified_by_id",
            table: "form_field_values",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_fields_created_by_id",
            table: "form_fields",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_fields_data_source_id",
            table: "form_fields",
            column: "data_source_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_fields_form_section_id",
            table: "form_fields",
            column: "form_section_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_fields_form_version_id_field_key",
            table: "form_fields",
            columns: new[] { "form_version_id", "field_key" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_form_fields_last_modified_by_id",
            table: "form_fields",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_fields_parent_field_id",
            table: "form_fields",
            column: "parent_field_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_sections_created_by_id",
            table: "form_sections",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_sections_form_version_id_section_number",
            table: "form_sections",
            columns: new[] { "form_version_id", "section_number" });

        migrationBuilder.CreateIndex(
            name: "ix_form_sections_last_modified_by_id",
            table: "form_sections",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_submission_histories_created_by_id",
            table: "form_submission_histories",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_submission_histories_form_submission_id",
            table: "form_submission_histories",
            column: "form_submission_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_submission_histories_last_modified_by_id",
            table: "form_submission_histories",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_submissions_created_by_id",
            table: "form_submissions",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_submissions_deleted_by_id",
            table: "form_submissions",
            column: "deleted_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_submissions_entity_type_entity_id",
            table: "form_submissions",
            columns: new[] { "entity_type", "entity_id" });

        migrationBuilder.CreateIndex(
            name: "ix_form_submissions_form_version_id",
            table: "form_submissions",
            column: "form_version_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_submissions_last_modified_by_id",
            table: "form_submissions",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_submissions_reviewed_by_id",
            table: "form_submissions",
            column: "reviewed_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_submissions_submitted_by_id",
            table: "form_submissions",
            column: "submitted_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_versions_created_by_id",
            table: "form_versions",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_versions_deleted_by_id",
            table: "form_versions",
            column: "deleted_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_versions_form_definition_id_version_number",
            table: "form_versions",
            columns: new[] { "form_definition_id", "version_number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_form_versions_last_modified_by_id",
            table: "form_versions",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_form_versions_published_by_id",
            table: "form_versions",
            column: "published_by_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "data_source_items");

        migrationBuilder.DropTable(
            name: "form_field_options");

        migrationBuilder.DropTable(
            name: "form_field_values");

        migrationBuilder.DropTable(
            name: "form_submission_histories");

        migrationBuilder.DropTable(
            name: "form_fields");

        migrationBuilder.DropTable(
            name: "form_submissions");

        migrationBuilder.DropTable(
            name: "data_sources");

        migrationBuilder.DropTable(
            name: "form_sections");

        migrationBuilder.DropTable(
            name: "form_versions");

        migrationBuilder.DropTable(
            name: "form_definitions");
    }
}