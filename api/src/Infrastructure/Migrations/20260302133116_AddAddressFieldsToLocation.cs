using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddAddressFieldsToLocation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "address_line1",
            table: "locations",
            type: "varchar(255)",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "address_line2",
            table: "locations",
            type: "varchar(255)",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "county",
            table: "locations",
            type: "varchar(100)",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "town_or_city",
            table: "locations",
            type: "varchar(100)",
            nullable: false,
            defaultValue: string.Empty);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "address_line1",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "address_line2",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "county",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "town_or_city",
            table: "locations");
    }
}