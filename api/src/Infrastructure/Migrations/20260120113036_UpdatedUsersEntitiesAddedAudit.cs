using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class UpdatedUsersEntitiesAddedAudit : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "location_users");

        migrationBuilder.AlterColumn<DateTime>(
            name: "updated_at",
            table: "users",
            type: "timestamp with time zone",
            nullable: true,
            defaultValueSql: "timezone('utc', now())",
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "created_at",
            table: "users",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "timezone('utc', now())",
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.AddColumn<Guid>(
            name: "created_by_id",
            table: "users",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "deleted_at",
            table: "users",
            type: "timestamp with time zone",
            nullable: true,
            defaultValueSql: "timezone('utc', now())");

        migrationBuilder.AddColumn<Guid>(
            name: "deleted_by_id",
            table: "users",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "is_deleted",
            table: "users",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<Guid>(
            name: "last_modified_by_id",
            table: "users",
            type: "uuid",
            nullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "created_at",
            table: "user_mfa",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "timezone('utc', now())",
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<bool>(
            name: "is_deleted",
            table: "services",
            type: "boolean",
            nullable: false,
            defaultValue: false,
            oldClrType: typeof(bool),
            oldType: "boolean");

        migrationBuilder.AlterColumn<DateTime>(
            name: "created_at",
            table: "services",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "timezone('utc', now())",
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "created_at",
            table: "mfa_sessions",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "timezone('utc', now())",
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.CreateIndex(
            name: "ix_users_created_by_id",
            table: "users",
            column: "created_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_users_deleted_by_id",
            table: "users",
            column: "deleted_by_id");

        migrationBuilder.CreateIndex(
            name: "ix_users_last_modified_by_id",
            table: "users",
            column: "last_modified_by_id");

        migrationBuilder.AddForeignKey(
            name: "fk_users_users_created_by_id",
            table: "users",
            column: "created_by_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "fk_users_users_deleted_by_id",
            table: "users",
            column: "deleted_by_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "fk_users_users_last_modified_by_id",
            table: "users",
            column: "last_modified_by_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_users_users_created_by_id",
            table: "users");

        migrationBuilder.DropForeignKey(
            name: "fk_users_users_deleted_by_id",
            table: "users");

        migrationBuilder.DropForeignKey(
            name: "fk_users_users_last_modified_by_id",
            table: "users");

        migrationBuilder.DropIndex(
            name: "ix_users_created_by_id",
            table: "users");

        migrationBuilder.DropIndex(
            name: "ix_users_deleted_by_id",
            table: "users");

        migrationBuilder.DropIndex(
            name: "ix_users_last_modified_by_id",
            table: "users");

        migrationBuilder.DropColumn(
            name: "created_by_id",
            table: "users");

        migrationBuilder.DropColumn(
            name: "deleted_at",
            table: "users");

        migrationBuilder.DropColumn(
            name: "deleted_by_id",
            table: "users");

        migrationBuilder.DropColumn(
            name: "is_deleted",
            table: "users");

        migrationBuilder.DropColumn(
            name: "last_modified_by_id",
            table: "users");

        migrationBuilder.AlterColumn<DateTime>(
            name: "updated_at",
            table: "users",
            type: "timestamp with time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldNullable: true,
            oldDefaultValueSql: "timezone('utc', now())");

        migrationBuilder.AlterColumn<DateTime>(
            name: "created_at",
            table: "users",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldDefaultValueSql: "timezone('utc', now())");

        migrationBuilder.AlterColumn<DateTime>(
            name: "created_at",
            table: "user_mfa",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldDefaultValueSql: "timezone('utc', now())");

        migrationBuilder.AlterColumn<bool>(
            name: "is_deleted",
            table: "services",
            type: "boolean",
            nullable: false,
            oldClrType: typeof(bool),
            oldType: "boolean",
            oldDefaultValue: false);

        migrationBuilder.AlterColumn<DateTime>(
            name: "created_at",
            table: "services",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldDefaultValueSql: "timezone('utc', now())");

        migrationBuilder.AlterColumn<DateTime>(
            name: "created_at",
            table: "mfa_sessions",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldDefaultValueSql: "timezone('utc', now())");

        migrationBuilder.CreateTable(
            name: "location_users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                location_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false)
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
    }
}