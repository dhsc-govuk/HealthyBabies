using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFormFieldValueSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "display_order",
                table: "form_field_values",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "field_type",
                table: "form_field_values",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "options_snapshot",
                table: "form_field_values",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "question_code",
                table: "form_field_values",
                type: "varchar(100)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "question_hint",
                table: "form_field_values",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "question_label",
                table: "form_field_values",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "display_order",
                table: "form_field_values");

            migrationBuilder.DropColumn(
                name: "field_type",
                table: "form_field_values");

            migrationBuilder.DropColumn(
                name: "options_snapshot",
                table: "form_field_values");

            migrationBuilder.DropColumn(
                name: "question_code",
                table: "form_field_values");

            migrationBuilder.DropColumn(
                name: "question_hint",
                table: "form_field_values");

            migrationBuilder.DropColumn(
                name: "question_label",
                table: "form_field_values");
        }
    }
}