using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddDataCollectionFormModuleAssignments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "data_collection_form_module_assignments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                data_collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                form_module_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_data_collection_form_module_assignments", x => x.id);
                table.ForeignKey(
                    name: "fk_data_collection_form_module_assignments_data_collection_for",
                    column: x => x.form_module_id,
                    principalTable: "data_collection_form_modules",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_data_collection_form_module_assignments_data_collections_da",
                    column: x => x.data_collection_id,
                    principalTable: "data_collections",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_data_collection_form_module_assignments_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_data_collection_form_module_assignments_users_last_modified",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_form_module_assignments_created_by_id",
            table: "data_collection_form_module_assignments",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_form_module_assignments_data_collection_id_",
            table: "data_collection_form_module_assignments",
            columns: new[] { "data_collection_id", "form_module_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_form_module_assignments_form_module_id",
            table: "data_collection_form_module_assignments",
            column: "form_module_id");

        migrationBuilder.CreateIndex(
            name: "ix_data_collection_form_module_assignments_last_modified_by_id",
            table: "data_collection_form_module_assignments",
            column: "last_modified_by_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "data_collection_form_module_assignments");
    }
}