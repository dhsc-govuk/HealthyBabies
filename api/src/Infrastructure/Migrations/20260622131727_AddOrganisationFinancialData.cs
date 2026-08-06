using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddOrganisationFinancialData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "organisation_financial_data",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organisation_id = table.Column<Guid>(type: "uuid", nullable: false),
                module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                year_range = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_organisation_financial_data", x => x.id);
                table.ForeignKey(
                    name: "fk_organisation_financial_data_organisations_organisation_id",
                    column: x => x.organisation_id,
                    principalTable: "organisations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_organisation_financial_data_users_created_by_id",
                    column: x => x.created_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_organisation_financial_data_users_last_modified_by_id",
                    column: x => x.last_modified_by_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_organisation_financial_data_created_by_id",
            table: "organisation_financial_data",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_organisation_financial_data_last_modified_by_id",
            table: "organisation_financial_data",
            column: "last_modified_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_organisation_financial_data_org_module_year",
            table: "organisation_financial_data",
            columns: new[] { "organisation_id", "module", "year_range" },
            unique: true);

        migrationBuilder.Sql(@"
                INSERT INTO global_data (id, entity, value, description, created_at, updated_at, created_by_id, last_modified_by_id)
                SELECT gen_random_uuid(), 'FINANCIAL_DATA_ALLOCATION_MODULE', 'delivery-grant', 'Delivery grant allocation', NOW(), NULL, NULL, NULL
                WHERE NOT EXISTS (SELECT 1 FROM global_data WHERE entity = 'FINANCIAL_DATA_ALLOCATION_MODULE' AND value = 'delivery-grant');

                INSERT INTO global_data (id, entity, value, description, created_at, updated_at, created_by_id, last_modified_by_id)
                SELECT gen_random_uuid(), 'FINANCIAL_DATA_ALLOCATION_MODULE', 'parenting', 'Parenting allocation', NOW(), NULL, NULL, NULL
                WHERE NOT EXISTS (SELECT 1 FROM global_data WHERE entity = 'FINANCIAL_DATA_ALLOCATION_MODULE' AND value = 'parenting');

                INSERT INTO global_data (id, entity, value, description, created_at, updated_at, created_by_id, last_modified_by_id)
                SELECT gen_random_uuid(), 'FINANCIAL_DATA_ALLOCATION_MODULE', 'hle', 'HLE allocation', NOW(), NULL, NULL, NULL
                WHERE NOT EXISTS (SELECT 1 FROM global_data WHERE entity = 'FINANCIAL_DATA_ALLOCATION_MODULE' AND value = 'hle');

                INSERT INTO global_data (id, entity, value, description, created_at, updated_at, created_by_id, last_modified_by_id)
                SELECT gen_random_uuid(), 'FINANCIAL_DATA_ALLOCATION_MODULE', 'send', 'SEND allocation', NOW(), NULL, NULL, NULL
                WHERE NOT EXISTS (SELECT 1 FROM global_data WHERE entity = 'FINANCIAL_DATA_ALLOCATION_MODULE' AND value = 'send');

                INSERT INTO global_data (id, entity, value, description, created_at, updated_at, created_by_id, last_modified_by_id)
                SELECT gen_random_uuid(), 'FINANCIAL_DATA_ALLOCATION_MODULE', 'pnmh-and-pir', 'Perinatal Mental Health and Parent-Infant Relationships', NOW(), NULL, NULL, NULL
                WHERE NOT EXISTS (SELECT 1 FROM global_data WHERE entity = 'FINANCIAL_DATA_ALLOCATION_MODULE' AND value = 'pnmh-and-pir');

                INSERT INTO global_data (id, entity, value, description, created_at, updated_at, created_by_id, last_modified_by_id)
                SELECT gen_random_uuid(), 'FINANCIAL_DATA_ALLOCATION_MODULE', 'infant-feeding', 'Infant Feeding', NOW(), NULL, NULL, NULL
                WHERE NOT EXISTS (SELECT 1 FROM global_data WHERE entity = 'FINANCIAL_DATA_ALLOCATION_MODULE' AND value = 'infant-feeding');

                INSERT INTO global_data (id, entity, value, description, created_at, updated_at, created_by_id, last_modified_by_id)
                SELECT gen_random_uuid(), 'FINANCIAL_DATA_ALLOCATION_MODULE', 'healthy-babies-offer', 'Healthy Babies Offer', NOW(), NULL, NULL, NULL
                WHERE NOT EXISTS (SELECT 1 FROM global_data WHERE entity = 'FINANCIAL_DATA_ALLOCATION_MODULE' AND value = 'healthy-babies-offer');

                INSERT INTO global_data (id, entity, value, description, created_at, updated_at, created_by_id, last_modified_by_id)
                SELECT gen_random_uuid(), 'FINANCIAL_DATA_ALLOCATION_YEAR', '2026-27', NULL, NOW(), NULL, NULL, NULL
                WHERE NOT EXISTS (SELECT 1 FROM global_data WHERE entity = 'FINANCIAL_DATA_ALLOCATION_YEAR' AND value = '2026-27');

                INSERT INTO global_data (id, entity, value, description, created_at, updated_at, created_by_id, last_modified_by_id)
                SELECT gen_random_uuid(), 'FINANCIAL_DATA_ALLOCATION_YEAR', '2027-28', NULL, NOW(), NULL, NULL, NULL
                WHERE NOT EXISTS (SELECT 1 FROM global_data WHERE entity = 'FINANCIAL_DATA_ALLOCATION_YEAR' AND value = '2027-28');

                INSERT INTO global_data (id, entity, value, description, created_at, updated_at, created_by_id, last_modified_by_id)
                SELECT gen_random_uuid(), 'FINANCIAL_DATA_ALLOCATION_YEAR', '2028-29', NULL, NOW(), NULL, NULL, NULL
                WHERE NOT EXISTS (SELECT 1 FROM global_data WHERE entity = 'FINANCIAL_DATA_ALLOCATION_YEAR' AND value = '2028-29');

                INSERT INTO global_data (id, entity, value, description, created_at, updated_at, created_by_id, last_modified_by_id)
                SELECT gen_random_uuid(), 'FINANCIAL_DATA_ROLLOVER_MODULE', 'rollover-spend', 'Total approved rollover spend', NOW(), NULL, NULL, NULL
                WHERE NOT EXISTS (SELECT 1 FROM global_data WHERE entity = 'FINANCIAL_DATA_ROLLOVER_MODULE' AND value = 'rollover-spend');

                INSERT INTO global_data (id, entity, value, description, created_at, updated_at, created_by_id, last_modified_by_id)
                SELECT gen_random_uuid(), 'FINANCIAL_DATA_ROLLOVER_YEAR', '2025-26', NULL, NOW(), NULL, NULL, NULL
                WHERE NOT EXISTS (SELECT 1 FROM global_data WHERE entity = 'FINANCIAL_DATA_ROLLOVER_YEAR' AND value = '2025-26');
            ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
                DELETE FROM global_data WHERE entity IN (
                    'FINANCIAL_DATA_ALLOCATION_MODULE',
                    'FINANCIAL_DATA_ALLOCATION_YEAR',
                    'FINANCIAL_DATA_ROLLOVER_MODULE',
                    'FINANCIAL_DATA_ROLLOVER_YEAR'
                );
            ");

        migrationBuilder.DropTable(
            name: "organisation_financial_data");
    }
}