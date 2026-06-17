using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "organisations",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "varchar(255)", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_organisations", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<Name>(type: "jsonb", nullable: false),
                email = table.Column<string>(type: "varchar(320)", nullable: false),
                sub_id = table.Column<Guid>(type: "uuid", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                role = table.Column<string>(type: "varchar(255)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_users", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "locations",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organisation_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "varchar(255)", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_locations", x => x.id);
                table.ForeignKey(
                    name: "fk_locations_organisations_organisation_id",
                    column: x => x.organisation_id,
                    principalTable: "organisations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "organisation_users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                organisation_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_organisation_users", x => x.id);
                table.ForeignKey(
                    name: "fk_organisation_users_organisations_organisation_id",
                    column: x => x.organisation_id,
                    principalTable: "organisations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_organisation_users_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "location_users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                location_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_location_users", x => x.id);
                table.ForeignKey(
                    name: "fk_location_users_locations_location_id",
                    column: x => x.location_id,
                    principalTable: "locations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_location_users_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_location_users_location_id",
            table: "location_users",
            column: "location_id");

        migrationBuilder.CreateIndex(
            name: "ix_location_users_user_id",
            table: "location_users",
            column: "user_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_locations_organisation_id",
            table: "locations",
            column: "organisation_id");

        migrationBuilder.CreateIndex(
            name: "ix_organisation_users_organisation_id",
            table: "organisation_users",
            column: "organisation_id");

        migrationBuilder.CreateIndex(
            name: "ix_organisation_users_user_id",
            table: "organisation_users",
            column: "user_id",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "location_users");

        migrationBuilder.DropTable(
            name: "organisation_users");

        migrationBuilder.DropTable(
            name: "locations");

        migrationBuilder.DropTable(
            name: "users");

        migrationBuilder.DropTable(
            name: "organisations");
    }
}