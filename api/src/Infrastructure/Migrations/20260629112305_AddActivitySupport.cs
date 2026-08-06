using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActivitySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "form_submissions",
                type: "varchar(500)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "allows_multiple_activities",
                table: "data_collection_form_modules",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "name",
                table: "form_submissions");

            migrationBuilder.DropColumn(
                name: "allows_multiple_activities",
                table: "data_collection_form_modules");
        }
    }
}