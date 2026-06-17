using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class UpdateQSU14SumValidation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Update QSU14 field configuration to add maxSumField validation against QSU03
        migrationBuilder.Sql(@"
                UPDATE form_fields
                SET configuration = '{""suffix"":""users"",""width"":""5"",""maxSumField"":""QSU03"",""min"":1}'
                WHERE field_key = 'QSU14'
                AND configuration = '{""suffix"":""users"",""width"":""5""}'
            ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Revert QSU14 field configuration
        migrationBuilder.Sql(@"
                UPDATE form_fields
                SET configuration = '{""suffix"":""users"",""width"":""5""}'
                WHERE field_key = 'QSU14'
                AND configuration = '{""suffix"":""users"",""width"":""5"",""maxSumField"":""QSU03"",""min"":1}'
            ");
    }
}