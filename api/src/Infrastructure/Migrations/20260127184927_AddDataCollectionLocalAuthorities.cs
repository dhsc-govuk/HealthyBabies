using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddDataCollectionLocalAuthorities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "data_collection_local_authorities",
            columns: table => new
            {
                data_collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                local_authority_id = table.Column<Guid>(type: "uuid", nullable: false),
                assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                assigned_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_data_collection_local_authorities", x => new { x.data_collection_id, x.local_authority_id });
                table.ForeignKey(
                    name: "fk_data_collection_local_authorities_data_collections",
                    column: x => x.data_collection_id,
                    principalTable: "data_collections",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_data_collection_local_authorities_organisations",
                    column: x => x.local_authority_id,
                    principalTable: "organisations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_data_collection_local_authorities_users_assigned_by",
                    column: x => x.assigned_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_local_authorities_assigned_by_id",
            table: "data_collection_local_authorities",
            column: "assigned_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_local_authorities_local_authority_id",
            table: "data_collection_local_authorities",
            column: "local_authority_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "data_collection_local_authorities");
    }
}