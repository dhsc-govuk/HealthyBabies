using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddedServices : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "services",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organisation_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "varchar(255)", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                current_step = table.Column<int>(type: "integer", nullable: false),
                funding_type = table.Column<int>(type: "integer", nullable: true),
                live_status = table.Column<int>(type: "integer", nullable: true),
                planned_implementation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                frequency = table.Column<int>(type: "integer", nullable: true),
                has_name_changed = table.Column<bool>(type: "boolean", nullable: true),
                previous_name = table.Column<string>(type: "varchar(255)", nullable: true),
                strand = table.Column<int>(type: "integer", nullable: true),
                strand_category = table.Column<string>(type: "varchar(255)", nullable: true),
                lowest_age = table.Column<int>(type: "integer", nullable: true),
                highest_age = table.Column<int>(type: "integer", nullable: true),
                service_types = table.Column<string>(type: "varchar(500)", nullable: true),
                evidence_based_status = table.Column<int>(type: "integer", nullable: true),
                delivery_method = table.Column<string>(type: "varchar(500)", nullable: true),
                delivery_locations = table.Column<string>(type: "varchar(500)", nullable: true),
                service_deliverer = table.Column<int>(type: "integer", nullable: true),
                user_data_status = table.Column<int>(type: "integer", nullable: true),
                outcome_scores_status = table.Column<int>(type: "integer", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_services", x => x.id);
                table.ForeignKey(
                    name: "fk_services_organisations_organisation_id",
                    column: x => x.organisation_id,
                    principalTable: "organisations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_services_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_services_users_deleted_by_id",
                    column: x => x.deleted_by_id,
                    principalTable: "users",
                    principalColumn: "id");
                table.ForeignKey(
                    name: "fk_services_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id");
            });

        migrationBuilder.CreateIndex(
            name: "ix_services_created_by_id",
            table: "services",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_services_deleted_by_id",
            table: "services",
            column: "deleted_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_services_last_modified_by_id",
            table: "services",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_services_organisation_id",
            table: "services",
            column: "organisation_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "services");
    }
}