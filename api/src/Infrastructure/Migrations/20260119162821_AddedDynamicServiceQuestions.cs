using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddedDynamicServiceQuestions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("delete from services");

        migrationBuilder.DropColumn(
            name: "delivery_locations",
            table: "services");

        migrationBuilder.DropColumn(
            name: "delivery_method",
            table: "services");

        migrationBuilder.DropColumn(
            name: "evidence_based_status",
            table: "services");

        migrationBuilder.DropColumn(
            name: "frequency",
            table: "services");

        migrationBuilder.DropColumn(
            name: "funding_type",
            table: "services");

        migrationBuilder.DropColumn(
            name: "has_name_changed",
            table: "services");

        migrationBuilder.DropColumn(
            name: "highest_age",
            table: "services");

        migrationBuilder.DropColumn(
            name: "live_status",
            table: "services");

        migrationBuilder.DropColumn(
            name: "lowest_age",
            table: "services");

        migrationBuilder.DropColumn(
            name: "outcome_scores_status",
            table: "services");

        migrationBuilder.DropColumn(
            name: "planned_implementation_date",
            table: "services");

        migrationBuilder.DropColumn(
            name: "previous_name",
            table: "services");

        migrationBuilder.DropColumn(
            name: "service_deliverer",
            table: "services");

        migrationBuilder.DropColumn(
            name: "service_types",
            table: "services");

        migrationBuilder.DropColumn(
            name: "strand",
            table: "services");

        migrationBuilder.DropColumn(
            name: "strand_category",
            table: "services");

        migrationBuilder.DropColumn(
            name: "user_data_status",
            table: "services");

        migrationBuilder.CreateTable(
            name: "service_answers",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                service_id = table.Column<Guid>(type: "uuid", nullable: false),
                question_code = table.Column<string>(type: "varchar(50)", nullable: false),
                question_label = table.Column<string>(type: "varchar(500)", nullable: false),
                question_hint = table.Column<string>(type: "varchar(1000)", nullable: true),
                question_type = table.Column<int>(type: "integer", nullable: false),
                step = table.Column<int>(type: "integer", nullable: false),
                display_order = table.Column<int>(type: "integer", nullable: false),
                value = table.Column<string>(type: "text", nullable: true),
                display_value = table.Column<string>(type: "text", nullable: true),
                options_snapshot = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_service_answers", x => x.id);
                table.ForeignKey(
                    name: "fk_service_answers_services_service_id",
                    column: x => x.service_id,
                    principalTable: "services",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_service_answers_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_service_answers_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id");
            });

        migrationBuilder.CreateTable(
            name: "service_form_questions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "varchar(50)", nullable: false),
                label = table.Column<string>(type: "varchar(500)", nullable: false),
                hint = table.Column<string>(type: "varchar(1000)", nullable: true),
                placeholder = table.Column<string>(type: "varchar(255)", nullable: true),
                question_type = table.Column<int>(type: "integer", nullable: false),
                step = table.Column<int>(type: "integer", nullable: false),
                display_order = table.Column<int>(type: "integer", nullable: false),
                is_required = table.Column<bool>(type: "boolean", nullable: false),
                is_predefined = table.Column<bool>(type: "boolean", nullable: false),
                help_text_summary = table.Column<string>(type: "varchar(500)", nullable: true),
                help_text = table.Column<string>(type: "text", nullable: true),
                conditional_question_code = table.Column<string>(type: "varchar(50)", nullable: true),
                conditional_value = table.Column<string>(type: "varchar(255)", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_service_form_questions", x => x.id);
                table.ForeignKey(
                    name: "fk_service_form_questions_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_service_form_questions_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id");
            });

        migrationBuilder.CreateTable(
            name: "service_form_question_options",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                question_id = table.Column<Guid>(type: "uuid", nullable: false),
                value = table.Column<string>(type: "varchar(255)", nullable: false),
                label = table.Column<string>(type: "varchar(500)", nullable: false),
                display_order = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_service_form_question_options", x => x.id);
                table.ForeignKey(
                    name: "fk_service_form_question_options_questions_question_id",
                    column: x => x.question_id,
                    principalTable: "service_form_questions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_service_form_question_options_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_service_form_question_options_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id");
            });

        migrationBuilder.CreateIndex(
            name: "ix_service_answers_created_by_id",
            table: "service_answers",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_service_answers_last_modified_by_id",
            table: "service_answers",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_service_answers_service_id_question_code",
            table: "service_answers",
            columns: new[] { "service_id", "question_code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_service_form_question_options_created_by_id",
            table: "service_form_question_options",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_service_form_question_options_last_modified_by_id",
            table: "service_form_question_options",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_service_form_question_options_question_id",
            table: "service_form_question_options",
            column: "question_id");

        migrationBuilder.CreateIndex(
            name: "ix_service_form_questions_code",
            table: "service_form_questions",
            column: "code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_service_form_questions_created_by_id",
            table: "service_form_questions",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_service_form_questions_last_modified_by_id",
            table: "service_form_questions",
            column: "last_modified_by_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "service_answers");

        migrationBuilder.DropTable(
            name: "service_form_question_options");

        migrationBuilder.DropTable(
            name: "service_form_questions");

        migrationBuilder.AddColumn<string>(
            name: "delivery_locations",
            table: "services",
            type: "varchar(500)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "delivery_method",
            table: "services",
            type: "varchar(500)",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "evidence_based_status",
            table: "services",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "frequency",
            table: "services",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "funding_type",
            table: "services",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "has_name_changed",
            table: "services",
            type: "boolean",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "highest_age",
            table: "services",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "live_status",
            table: "services",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "lowest_age",
            table: "services",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "outcome_scores_status",
            table: "services",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "planned_implementation_date",
            table: "services",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "previous_name",
            table: "services",
            type: "varchar(255)",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "service_deliverer",
            table: "services",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "service_types",
            table: "services",
            type: "varchar(500)",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "strand",
            table: "services",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "strand_category",
            table: "services",
            type: "varchar(255)",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "user_data_status",
            table: "services",
            type: "integer",
            nullable: true);
    }
}