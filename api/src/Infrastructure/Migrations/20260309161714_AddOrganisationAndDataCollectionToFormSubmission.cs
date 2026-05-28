using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganisationAndDataCollectionToFormSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "data_collection_id",
                table: "form_submissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "organisation_id",
                table: "form_submissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_form_submissions_organisation_id_data_collection_id_form_mo",
                table: "form_submissions",
                columns: new[] { "organisation_id", "data_collection_id", "form_module_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_form_submissions_organisation_id_data_collection_id_form_mo",
                table: "form_submissions");

            migrationBuilder.DropColumn(
                name: "data_collection_id",
                table: "form_submissions");

            migrationBuilder.DropColumn(
                name: "organisation_id",
                table: "form_submissions");
        }
    }
}