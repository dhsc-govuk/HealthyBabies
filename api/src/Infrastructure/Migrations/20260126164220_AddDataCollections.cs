using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddDataCollections : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "data_collections",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "varchar(255)", nullable: false),
                description = table.Column<string>(type: "varchar(1000)", nullable: true),
                start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                deleted_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_data_collections", x => x.id);
                table.ForeignKey(
                    name: "fk_data_collections_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_data_collections_users_deleted_by_id",
                    column: x => x.deleted_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "fk_data_collections_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "ix_data_collections_created_by_id",
            table: "data_collections",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_collections_deleted_by_id",
            table: "data_collections",
            column: "deleted_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_collections_last_modified_by_id",
            table: "data_collections",
            column: "last_modified_by_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "data_collections");
    }
}