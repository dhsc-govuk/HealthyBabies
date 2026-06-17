using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddDataCollectionFormModulesAndIsDraft : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "is_closed",
            table: "data_collections",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "is_draft",
            table: "data_collections",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "is_submitted_by_all_local_authorities",
            table: "data_collections",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.CreateTable(
            name: "data_collection_form_modules",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                section_number = table.Column<int>(type: "integer", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                last_changed_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_data_collection_form_modules", x => x.id);
                table.ForeignKey(
                    name: "fk_data_collection_form_modules_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_data_collection_form_modules_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_form_modules_code",
            table: "data_collection_form_modules",
            column: "code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_form_modules_created_by_id",
            table: "data_collection_form_modules",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_form_modules_last_modified_by_id",
            table: "data_collection_form_modules",
            column: "last_modified_by_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "data_collection_form_modules");

        migrationBuilder.DropColumn(
            name: "is_closed",
            table: "data_collections");

        migrationBuilder.DropColumn(
            name: "is_draft",
            table: "data_collections");

        migrationBuilder.DropColumn(
            name: "is_submitted_by_all_local_authorities",
            table: "data_collections");
    }
}