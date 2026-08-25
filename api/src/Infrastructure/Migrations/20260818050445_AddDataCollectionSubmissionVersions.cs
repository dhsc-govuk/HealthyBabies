using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDataCollectionSubmissionVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "data_collection_submission_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_collection_submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    review_notes = table.Column<string>(type: "varchar(2000)", nullable: true),
                    sections_to_review = table.Column<string>(type: "jsonb", nullable: false),
                    resubmit_by = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    submitted_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    content = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_collection_submission_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_data_collection_submission_versions_data_collection_submissions",
                        column: x => x.data_collection_submission_id,
                        principalTable: "data_collection_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_data_collection_submission_versions_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_data_collection_submission_versions_users_last_modified_by_id",
                        column: x => x.last_modified_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_data_collection_submission_versions_users_reviewed_by",
                        column: x => x.reviewed_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_data_collection_submission_versions_users_submitted_by",
                        column: x => x.submitted_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_data_collection_submission_versions_created_by_id",
                table: "data_collection_submission_versions",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_data_collection_submission_versions_last_modified_by_id",
                table: "data_collection_submission_versions",
                column: "last_modified_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_data_collection_submission_versions_reviewed_by_id",
                table: "data_collection_submission_versions",
                column: "reviewed_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_data_collection_submission_versions_submitted_by_id",
                table: "data_collection_submission_versions",
                column: "submitted_by_id");

            migrationBuilder.CreateIndex(
                name: "ux_data_collection_submission_versions_submission_version",
                table: "data_collection_submission_versions",
                columns: new[] { "data_collection_submission_id", "version_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "data_collection_submission_versions");
        }
    }
}