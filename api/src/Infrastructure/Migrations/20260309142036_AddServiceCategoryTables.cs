using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddServiceCategoryTables : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "service_categories",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organisation_id = table.Column<Guid>(type: "uuid", nullable: false),
                category_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                category_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                current_step = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                last_modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_service_categories", x => x.id);
                table.ForeignKey(
                    name: "fk_service_categories_organisations_organisation_id",
                    column: x => x.organisation_id,
                    principalTable: "organisations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_service_categories_users_created_by",
                    column: x => x.created_by,
                    principalTable: "users",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_service_categories_users_last_modified_by",
                    column: x => x.last_modified_by,
                    principalTable: "users",
                    principalColumn: "id");
            });

        migrationBuilder.CreateTable(
            name: "service_category_form_questions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                label = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                hint = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                placeholder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                question_type = table.Column<int>(type: "integer", nullable: false),
                step = table.Column<int>(type: "integer", nullable: false),
                display_order = table.Column<int>(type: "integer", nullable: false),
                is_required = table.Column<bool>(type: "boolean", nullable: false),
                is_predefined = table.Column<bool>(type: "boolean", nullable: false),
                help_text_summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                help_text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                conditional_question_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                conditional_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_service_category_form_questions", x => x.id);
                table.ForeignKey(
                    name: "fk_service_category_form_questions_users_created_by",
                    column: x => x.created_by,
                    principalTable: "users",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_service_category_form_questions_users_last_modified_by",
                    column: x => x.last_modified_by,
                    principalTable: "users",
                    principalColumn: "id");
            });

        migrationBuilder.CreateTable(
            name: "service_category_answers",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                service_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                question_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                question_label = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                question_hint = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                question_type = table.Column<int>(type: "integer", nullable: false),
                step = table.Column<int>(type: "integer", nullable: false),
                display_order = table.Column<int>(type: "integer", nullable: false),
                value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                display_value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                options_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_service_category_answers", x => x.id);
                table.ForeignKey(
                    name: "fk_service_category_answers_service_categories_service_categor",
                    column: x => x.service_category_id,
                    principalTable: "service_categories",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_service_category_answers_users_created_by",
                    column: x => x.created_by,
                    principalTable: "users",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_service_category_answers_users_last_modified_by",
                    column: x => x.last_modified_by,
                    principalTable: "users",
                    principalColumn: "id");
            });

        migrationBuilder.CreateTable(
            name: "service_category_form_question_options",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                question_id = table.Column<Guid>(type: "uuid", nullable: false),
                value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                label = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                display_order = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_service_category_form_question_options", x => x.id);
                table.ForeignKey(
                    name: "fk_service_category_form_question_options_service_category_for",
                    column: x => x.question_id,
                    principalTable: "service_category_form_questions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_service_category_form_question_options_users_created_by",
                    column: x => x.created_by,
                    principalTable: "users",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_service_category_form_question_options_users_last_modified_",
                    column: x => x.last_modified_by,
                    principalTable: "users",
                    principalColumn: "id");
            });

        migrationBuilder.CreateIndex(
            name: "ix_service_categories_created_by",
            table: "service_categories",
            column: "created_by");

        migrationBuilder.CreateIndex(
            name: "ix_service_categories_last_modified_by",
            table: "service_categories",
            column: "last_modified_by");

        migrationBuilder.CreateIndex(
            name: "ix_service_categories_organisation_id_category_code",
            table: "service_categories",
            columns: new[] { "organisation_id", "category_code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_service_category_answers_created_by",
            table: "service_category_answers",
            column: "created_by");

        migrationBuilder.CreateIndex(
            name: "ix_service_category_answers_last_modified_by",
            table: "service_category_answers",
            column: "last_modified_by");

        migrationBuilder.CreateIndex(
            name: "ix_service_category_answers_service_category_id",
            table: "service_category_answers",
            column: "service_category_id");

        migrationBuilder.CreateIndex(
            name: "ix_service_category_form_question_options_created_by",
            table: "service_category_form_question_options",
            column: "created_by");

        migrationBuilder.CreateIndex(
            name: "ix_service_category_form_question_options_last_modified_by",
            table: "service_category_form_question_options",
            column: "last_modified_by");

        migrationBuilder.CreateIndex(
            name: "ix_service_category_form_question_options_question_id",
            table: "service_category_form_question_options",
            column: "question_id");

        migrationBuilder.CreateIndex(
            name: "ix_service_category_form_questions_code",
            table: "service_category_form_questions",
            column: "code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_service_category_form_questions_created_by",
            table: "service_category_form_questions",
            column: "created_by");

        migrationBuilder.CreateIndex(
            name: "ix_service_category_form_questions_last_modified_by",
            table: "service_category_form_questions",
            column: "last_modified_by");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "service_category_answers");

        migrationBuilder.DropTable(
            name: "service_category_form_question_options");

        migrationBuilder.DropTable(
            name: "service_categories");

        migrationBuilder.DropTable(
            name: "service_category_form_questions");
    }
}