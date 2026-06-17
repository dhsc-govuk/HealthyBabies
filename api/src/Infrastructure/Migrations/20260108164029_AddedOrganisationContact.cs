using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddedOrganisationContact : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "created_at",
            table: "organisations",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "timezone('utc', now())");

        migrationBuilder.AddColumn<Guid>(
            name: "created_by_id",
            table: "organisations",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "last_modified_by_id",
            table: "organisations",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ons_code",
            table: "organisations",
            type: "varchar(255)",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<DateTime>(
            name: "updated_at",
            table: "organisations",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "global_data",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                entity = table.Column<string>(type: "varchar(255)", nullable: false),
                value = table.Column<string>(type: "varchar(255)", nullable: false),
                description = table.Column<string>(type: "varchar(500)", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_global_data", x => x.id);
                table.ForeignKey(
                    name: "fk_organisations_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_organisations_users_updated_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "organisation_contacts",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "varchar(255)", nullable: false),
                email = table.Column<string>(type: "varchar(255)", nullable: false),
                role = table.Column<string>(type: "varchar(100)", nullable: false),
                organisation_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_organisation_contacts", x => x.id);
                table.ForeignKey(
                    name: "fk_organisation_contacts_organisations_organisation_id",
                    column: x => x.organisation_id,
                    principalTable: "organisations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_organisation_contacts_users_deleted_by_id",
                    column: x => x.deleted_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_organisations_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_organisations_users_updated_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_organisations_created_by_id",
            table: "organisations",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_organisations_last_modified_by_id",
            table: "organisations",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_global_data_created_by_id",
            table: "global_data",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_global_data_entity",
            table: "global_data",
            column: "entity");

        migrationBuilder.CreateIndex(
            name: "ix_global_data_last_modified_by_id",
            table: "global_data",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_organisation_contacts_created_by_id",
            table: "organisation_contacts",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_organisation_contacts_deleted_by_id",
            table: "organisation_contacts",
            column: "deleted_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_organisation_contacts_last_modified_by_id",
            table: "organisation_contacts",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_organisation_contacts_organisation_id",
            table: "organisation_contacts",
            column: "organisation_id");

        migrationBuilder.AddForeignKey(
            name: "fk_organisations_users_created_by_id",
            table: "organisations",
            column: "created_by_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "fk_organisations_users_updated_by_id",
            table: "organisations",
            column: "last_modified_by_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_organisations_users_created_by_id",
            table: "organisations");

        migrationBuilder.DropForeignKey(
            name: "fk_organisations_users_updated_by_id",
            table: "organisations");

        migrationBuilder.DropTable(
            name: "global_data");

        migrationBuilder.DropTable(
            name: "organisation_contacts");

        migrationBuilder.DropIndex(
            name: "ix_organisations_created_by_id",
            table: "organisations");

        migrationBuilder.DropIndex(
            name: "ix_organisations_last_modified_by_id",
            table: "organisations");

        migrationBuilder.DropColumn(
            name: "created_at",
            table: "organisations");

        migrationBuilder.DropColumn(
            name: "created_by_id",
            table: "organisations");

        migrationBuilder.DropColumn(
            name: "last_modified_by_id",
            table: "organisations");

        migrationBuilder.DropColumn(
            name: "ons_code",
            table: "organisations");

        migrationBuilder.DropColumn(
            name: "updated_at",
            table: "organisations");
    }
}