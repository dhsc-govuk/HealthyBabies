using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddedMFA : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "mfa_sessions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                ip_address_hash = table.Column<string>(type: "varchar(64)", nullable: false),
                user_agent_hash = table.Column<string>(type: "varchar(64)", nullable: false),
                expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_mfa_sessions", x => x.id);
                table.ForeignKey(
                    name: "fk_mfa_sessions_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_mfa",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                encrypted_secret = table.Column<string>(type: "text", nullable: false),
                is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                enabled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                hashed_recovery_codes = table.Column<List<string>>(type: "jsonb", nullable: false),
                failed_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_mfa", x => x.id);
                table.ForeignKey(
                    name: "fk_user_mfa_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_mfa_sessions_expires_at",
            table: "mfa_sessions",
            column: "expires_at");

        migrationBuilder.CreateIndex(
            name: "ix_mfa_sessions_user_id",
            table: "mfa_sessions",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_user_mfa_user_id",
            table: "user_mfa",
            column: "user_id",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "mfa_sessions");

        migrationBuilder.DropTable(
            name: "user_mfa");
    }
}