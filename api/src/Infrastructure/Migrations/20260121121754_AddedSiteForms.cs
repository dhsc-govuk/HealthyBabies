using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddedSiteForms : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "bsfh_branding",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "clarification_comments",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "date_opened",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "delivery_site_name",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "location_types",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "name_change",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "status_of_site",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "type_of_site",
            table: "locations");

        migrationBuilder.AlterColumn<string>(
            name: "reference_number",
            table: "locations",
            type: "varchar(255)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AlterColumn<string>(
            name: "post_code",
            table: "locations",
            type: "varchar(20)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.CreateTable(
            name: "site_answers",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                location_id = table.Column<Guid>(type: "uuid", nullable: false),
                question_code = table.Column<string>(type: "varchar(50)", nullable: false),
                question_label = table.Column<string>(type: "varchar(500)", nullable: false),
                question_hint = table.Column<string>(type: "varchar(1000)", nullable: true),
                question_type = table.Column<int>(type: "integer", nullable: false),
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
                table.PrimaryKey("pk_site_answers", x => x.id);
                table.ForeignKey(
                    name: "fk_site_answers_locations_location_id",
                    column: x => x.location_id,
                    principalTable: "locations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_site_answers_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_site_answers_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id");
            });

        migrationBuilder.CreateTable(
            name: "site_form_questions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "varchar(50)", nullable: false),
                label = table.Column<string>(type: "varchar(500)", nullable: false),
                hint = table.Column<string>(type: "varchar(1000)", nullable: true),
                placeholder = table.Column<string>(type: "varchar(255)", nullable: true),
                question_type = table.Column<int>(type: "integer", nullable: false),
                display_order = table.Column<int>(type: "integer", nullable: false),
                is_required = table.Column<bool>(type: "boolean", nullable: false),
                is_predefined = table.Column<bool>(type: "boolean", nullable: false),
                help_text_summary = table.Column<string>(type: "varchar(500)", nullable: true),
                help_text = table.Column<string>(type: "text", nullable: true),
                conditional_question_code = table.Column<string>(type: "varchar(50)", nullable: true),
                conditional_value = table.Column<string>(type: "varchar(255)", nullable: true),
                global_data_entity_key = table.Column<string>(type: "varchar(100)", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_site_form_questions", x => x.id);
                table.ForeignKey(
                    name: "fk_site_form_questions_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_site_form_questions_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id");
            });

        migrationBuilder.CreateTable(
            name: "site_form_question_options",
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
                table.PrimaryKey("pk_site_form_question_options", x => x.id);
                table.ForeignKey(
                    name: "fk_site_form_question_options_questions_question_id",
                    column: x => x.question_id,
                    principalTable: "site_form_questions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_site_form_question_options_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_site_form_question_options_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id");
            });

        migrationBuilder.CreateIndex(
            name: "ix_site_answers_created_by_id",
            table: "site_answers",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_site_answers_last_modified_by_id",
            table: "site_answers",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_site_answers_location_id_question_code",
            table: "site_answers",
            columns: new[] { "location_id", "question_code" });

        migrationBuilder.CreateIndex(
            name: "ix_site_form_question_options_created_by_id",
            table: "site_form_question_options",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_site_form_question_options_last_modified_by_id",
            table: "site_form_question_options",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_site_form_question_options_question_id",
            table: "site_form_question_options",
            column: "question_id");

        migrationBuilder.CreateIndex(
            name: "ix_site_form_questions_code",
            table: "site_form_questions",
            column: "code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_site_form_questions_created_by_id",
            table: "site_form_questions",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_site_form_questions_last_modified_by_id",
            table: "site_form_questions",
            column: "last_modified_by_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "site_answers");

        migrationBuilder.DropTable(
            name: "site_form_question_options");

        migrationBuilder.DropTable(
            name: "site_form_questions");

        migrationBuilder.AlterColumn<string>(
            name: "reference_number",
            table: "locations",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "varchar(255)");

        migrationBuilder.AlterColumn<string>(
            name: "post_code",
            table: "locations",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "varchar(20)");

        migrationBuilder.AddColumn<string>(
            name: "bsfh_branding",
            table: "locations",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "clarification_comments",
            table: "locations",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<DateTime>(
            name: "date_opened",
            table: "locations",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<string>(
            name: "delivery_site_name",
            table: "locations",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "location_types",
            table: "locations",
            type: "jsonb",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<bool>(
            name: "name_change",
            table: "locations",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "status_of_site",
            table: "locations",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "type_of_site",
            table: "locations",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);
    }
}