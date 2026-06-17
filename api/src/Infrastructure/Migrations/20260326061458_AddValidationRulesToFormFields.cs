using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddValidationRulesToFormFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = jsonb_set(jsonb_set(COALESCE(configuration, '{}'::jsonb), '{min}', '0'::jsonb), '{max}', '100'::jsonb)
                WHERE field_key IN ('BR01a', 'BR02a');
            ");

            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = jsonb_set(jsonb_set(jsonb_set(COALESCE(configuration, '{}'::jsonb), '{sumGroup}', '""ethnicity""'::jsonb), '{maxSumField}', '""QSU03""'::jsonb), '{min}', '0'::jsonb)
                WHERE field_key IN ('QSU08_white', 'QSU08_mixed', 'QSU08_asian', 'QSU08_black', 'QSU08_other', 'QSU08_prefer_not_to_say');
            ");

            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = jsonb_set(jsonb_set(jsonb_set(COALESCE(configuration, '{}'::jsonb), '{sumGroup}', '""imd""'::jsonb), '{maxSumField}', '""QSU03""'::jsonb), '{min}', '0'::jsonb)
                WHERE field_key IN ('QSU10_decile1', 'QSU10_decile2', 'QSU10_decile3', 'QSU10_decile4', 'QSU10_decile5', 'QSU10_decile6', 'QSU10_decile7', 'QSU10_decile8', 'QSU10_decile9', 'QSU10_decile10');
            ");

            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = jsonb_set(jsonb_set(jsonb_set(COALESCE(configuration, '{}'::jsonb), '{sumGroup}', '""sex""'::jsonb), '{maxSumField}', '""QSU03""'::jsonb), '{min}', '0'::jsonb)
                WHERE field_key IN ('QSU12_female', 'QSU12_male', 'QSU12_other', 'QSU12_prefer_not_to_say');
            ");

            migrationBuilder.Sql(@"UPDATE form_fields SET configuration = jsonb_set(jsonb_set(COALESCE(configuration, '{}'::jsonb), '{min}', '0'::jsonb), '{max}', '21'::jsonb) WHERE field_key IN ('PPS08_pre', 'PPS08_post');");
            migrationBuilder.Sql(@"UPDATE form_fields SET configuration = jsonb_set(jsonb_set(COALESCE(configuration, '{}'::jsonb), '{min}', '0'::jsonb), '{max}', '27'::jsonb) WHERE field_key IN ('PPS12_pre', 'PPS12_post');");
            migrationBuilder.Sql(@"UPDATE form_fields SET configuration = jsonb_set(jsonb_set(COALESCE(configuration, '{}'::jsonb), '{min}', '7'::jsonb), '{max}', '35'::jsonb) WHERE field_key IN ('PPS14_pre', 'PPS14_post');");
            migrationBuilder.Sql(@"UPDATE form_fields SET configuration = jsonb_set(jsonb_set(COALESCE(configuration, '{}'::jsonb), '{min}', '0'::jsonb), '{max}', '45'::jsonb) WHERE field_key IN ('PPS10_pre', 'PPS10_post');");
            migrationBuilder.Sql(@"UPDATE form_fields SET configuration = jsonb_set(jsonb_set(COALESCE(configuration, '{}'::jsonb), '{min}', '0'::jsonb), '{max}', '56'::jsonb) WHERE field_key IN ('PPS11_pre', 'PPS11_post');");
            migrationBuilder.Sql(@"UPDATE form_fields SET configuration = jsonb_set(jsonb_set(COALESCE(configuration, '{}'::jsonb), '{min}', '0'::jsonb), '{max}', '40'::jsonb) WHERE field_key IN ('PPS13_pre', 'PPS13_post');");
            migrationBuilder.Sql(@"UPDATE form_fields SET configuration = jsonb_set(jsonb_set(COALESCE(configuration, '{}'::jsonb), '{min}', '0'::jsonb), '{max}', '60'::jsonb) WHERE field_key IN ('PPS07_pre', 'PPS07_post');");
            migrationBuilder.Sql(@"UPDATE form_fields SET configuration = jsonb_set(jsonb_set(COALESCE(configuration, '{}'::jsonb), '{min}', '0'::jsonb), '{max}', '56'::jsonb) WHERE field_key IN ('PPS09_pre', 'PPS09_post');");
            migrationBuilder.Sql(@"UPDATE form_fields SET configuration = jsonb_set(jsonb_set(COALESCE(configuration, '{}'::jsonb), '{min}', '0'::jsonb), '{max}', '300'::jsonb) WHERE field_key IN ('PPS06_pre', 'PPS06_post');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE form_fields SET configuration = configuration - 'min' - 'max' WHERE field_key IN ('BR01a', 'BR02a');");
            migrationBuilder.Sql(@"UPDATE form_fields SET configuration = configuration - 'sumGroup' - 'maxSumField' - 'min' WHERE field_key LIKE 'QSU08_%' OR field_key LIKE 'QSU10_%' OR field_key LIKE 'QSU12_%';");
            migrationBuilder.Sql(@"UPDATE form_fields SET configuration = configuration - 'min' - 'max' WHERE field_key LIKE 'PPS%_pre' OR field_key LIKE 'PPS%_post';");
        }
    }
}