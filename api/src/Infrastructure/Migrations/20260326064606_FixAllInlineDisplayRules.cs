using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixAllInlineDisplayRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix QSU08 ethnicity breakdown fields to display inline under QSU07's Yes option
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET conditional_rules = '{""showWhen"":{""fieldKey"":""QSU07"",""equals"":""yes""},""displayInline"":true,""parentOption"":""yes""}'
                WHERE field_key LIKE 'QSU08_%';
            ");

            // Fix QSU10 IMD breakdown fields to display inline under QSU09's Yes option
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET conditional_rules = '{""showWhen"":{""fieldKey"":""QSU09"",""equals"":""yes""},""displayInline"":true,""parentOption"":""yes""}'
                WHERE field_key LIKE 'QSU10_%';
            ");

            // Fix QSU12 sex breakdown fields to display inline under QSU11's Yes option
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET conditional_rules = '{""showWhen"":{""fieldKey"":""QSU11"",""equals"":""yes""},""displayInline"":true,""parentOption"":""yes""}'
                WHERE field_key LIKE 'QSU12_%';
            ");

            // Fix QSU14 to display inline under QSU13's Yes option
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET conditional_rules = '{""showWhen"":{""fieldKey"":""QSU13"",""equals"":""yes""},""displayInline"":true,""parentOption"":""yes""}'
                WHERE field_key = 'QSU14';
            ");

            // Fix QSU16 to display inline under QSU15's Yes option
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET conditional_rules = '{""showWhen"":{""fieldKey"":""QSU15"",""equals"":""yes""},""displayInline"":true,""parentOption"":""yes""}'
                WHERE field_key = 'QSU16';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert conditional rules (remove displayInline and parentOption)
            migrationBuilder.Sql(@"UPDATE form_fields SET conditional_rules = '{""showWhen"":{""fieldKey"":""QSU07"",""equals"":""yes""}}' WHERE field_key LIKE 'QSU08_%';");
            migrationBuilder.Sql(@"UPDATE form_fields SET conditional_rules = '{""showWhen"":{""fieldKey"":""QSU09"",""equals"":""yes""}}' WHERE field_key LIKE 'QSU10_%';");
            migrationBuilder.Sql(@"UPDATE form_fields SET conditional_rules = '{""showWhen"":{""fieldKey"":""QSU11"",""equals"":""yes""}}' WHERE field_key LIKE 'QSU12_%';");
            migrationBuilder.Sql(@"UPDATE form_fields SET conditional_rules = '{""showWhen"":{""fieldKey"":""QSU13"",""equals"":""yes""}}' WHERE field_key = 'QSU14';");
            migrationBuilder.Sql(@"UPDATE form_fields SET conditional_rules = '{""showWhen"":{""fieldKey"":""QSU15"",""equals"":""yes""}}' WHERE field_key = 'QSU16';");
        }
    }
}