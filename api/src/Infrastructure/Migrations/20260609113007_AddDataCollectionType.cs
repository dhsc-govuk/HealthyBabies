using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddDataCollectionType : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "type",
            table: "data_collections",
            type: "character varying(50)",
            maxLength: 50,
            nullable: false,
            defaultValueSql: "'delivery-plan'");

        migrationBuilder.AddColumn<string>(
            name: "type",
            table: "data_collection_form_modules",
            type: "character varying(50)",
            maxLength: 50,
            nullable: false,
            defaultValueSql: "'delivery-plan'");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "type",
            table: "data_collections");

        migrationBuilder.DropColumn(
            name: "type",
            table: "data_collection_form_modules");
    }
}