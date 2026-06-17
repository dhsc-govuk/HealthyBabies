using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddFormSubmissionIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ix_form_submissions_status",
            table: "form_submissions",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "ix_form_submissions_submitted_at",
            table: "form_submissions",
            column: "submitted_at");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_form_submissions_status",
            table: "form_submissions");

        migrationBuilder.DropIndex(
            name: "ix_form_submissions_submitted_at",
            table: "form_submissions");
    }
}