using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixQSU02InlineDisplay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix QSU02 to display inline under QSU01's Yes option
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET conditional_rules = '{""showWhen"":{""fieldKey"":""QSU01"",""equals"":""yes""},""displayInline"":true,""parentOption"":""yes""}'
                WHERE field_key = 'QSU02';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert QSU02 conditional rules
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET conditional_rules = '{""showWhen"":{""fieldKey"":""QSU01"",""equals"":""yes""}}'
                WHERE field_key = 'QSU02';
            ");
        }
    }
}