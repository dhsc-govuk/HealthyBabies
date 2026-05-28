using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class Location_Types : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "bsfh_branding",
            table: "locations",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "clarification_comments",
            table: "locations",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<DateTime>(
            name: "date_opened",
            table: "locations",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

        migrationBuilder.AddColumn<string>(
            name: "delivery_site_name",
            table: "locations",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "location_types",
            table: "locations",
            type: "jsonb",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<bool>(
            name: "name_change",
            table: "locations",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "post_code",
            table: "locations",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "reference_number",
            table: "locations",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "status_of_site",
            table: "locations",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "type_of_site",
            table: "locations",
            type: "text",
            nullable: false,
            defaultValue: string.Empty);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "bsfh_branding",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "clarification_comments",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "date_opened",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "delivery_site_name",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "location_types",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "name_change",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "post_code",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "reference_number",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "status_of_site",
            table: "locations");

        migrationBuilder.DropColumn(
            name: "type_of_site",
            table: "locations");
    }
}