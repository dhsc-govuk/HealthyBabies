using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateParentCarerPanelSumValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update SPCP08a fields (sex breakdown) to add sumGroup and maxSumField
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP08a"",""groupLabel"":""How many members of the Parent Carer Panel are there, broken down by sex?"",""groupHint"":""For each sex, give a figure as of 31 March 2026."",""sumGroup"":""sex"",""maxSumField"":""SPCP07"",""min"":0}'
                WHERE field_key = 'SPCP08a_female';
            ");
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP08a"",""sumGroup"":""sex"",""maxSumField"":""SPCP07"",""min"":0}'
                WHERE field_key = 'SPCP08a_male';
            ");
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP08a"",""sumGroup"":""sex"",""maxSumField"":""SPCP07"",""min"":0}'
                WHERE field_key = 'SPCP08a_other';
            ");
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP08a"",""sumGroup"":""sex"",""maxSumField"":""SPCP07"",""min"":0}'
                WHERE field_key = 'SPCP08a_prefer_not_to_say';
            ");

            // Update SPCP09a fields (age breakdown) to add sumGroup and maxSumField
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP09a"",""groupLabel"":""How many members of the Parent Carer Panel are there, broken down by age?"",""groupHint"":""For each age group, give a figure as of 31 March 2026."",""sumGroup"":""age"",""maxSumField"":""SPCP07"",""min"":0}'
                WHERE field_key = 'SPCP09a_18_24';
            ");
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP09a"",""sumGroup"":""age"",""maxSumField"":""SPCP07"",""min"":0}'
                WHERE field_key = 'SPCP09a_25_34';
            ");
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP09a"",""sumGroup"":""age"",""maxSumField"":""SPCP07"",""min"":0}'
                WHERE field_key = 'SPCP09a_35_44';
            ");
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP09a"",""sumGroup"":""age"",""maxSumField"":""SPCP07"",""min"":0}'
                WHERE field_key = 'SPCP09a_45_54';
            ");
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP09a"",""sumGroup"":""age"",""maxSumField"":""SPCP07"",""min"":0}'
                WHERE field_key = 'SPCP09a_55_64';
            ");
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP09a"",""sumGroup"":""age"",""maxSumField"":""SPCP07"",""min"":0}'
                WHERE field_key = 'SPCP09a_65_plus';
            ");

            // Update SPCP010a fields (ethnicity breakdown) to add sumGroup and maxSumField
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP010a"",""groupLabel"":""How many members of the Parent Carer Panel are there, broken down by ethnicity?"",""groupHint"":""For each ethnicity, give a figure as of 31 March 2026."",""sumGroup"":""ethnicity"",""maxSumField"":""SPCP07"",""min"":0}'
                WHERE field_key = 'SPCP010a_white';
            ");
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP010a"",""sumGroup"":""ethnicity"",""maxSumField"":""SPCP07"",""min"":0}'
                WHERE field_key = 'SPCP010a_mixed';
            ");
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP010a"",""sumGroup"":""ethnicity"",""maxSumField"":""SPCP07"",""min"":0}'
                WHERE field_key = 'SPCP010a_asian';
            ");
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP010a"",""sumGroup"":""ethnicity"",""maxSumField"":""SPCP07"",""min"":0}'
                WHERE field_key = 'SPCP010a_black';
            ");
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP010a"",""sumGroup"":""ethnicity"",""maxSumField"":""SPCP07"",""min"":0}'
                WHERE field_key = 'SPCP010a_other';
            ");
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP010a"",""sumGroup"":""ethnicity"",""maxSumField"":""SPCP07"",""min"":0}'
                WHERE field_key = 'SPCP010a_prefer_not_to_say';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert SPCP08a fields
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP08a"",""groupLabel"":""How many members of the Parent Carer Panel are there, broken down by sex?"",""groupHint"":""For each sex, give a figure as of 31 March 2026.""}'
                WHERE field_key = 'SPCP08a_female';
            ");
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP08a""}'
                WHERE field_key IN ('SPCP08a_male', 'SPCP08a_other', 'SPCP08a_prefer_not_to_say');
            ");

            // Revert SPCP09a fields
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP09a"",""groupLabel"":""How many members of the Parent Carer Panel are there, broken down by age?"",""groupHint"":""For each age group, give a figure as of 31 March 2026.""}'
                WHERE field_key = 'SPCP09a_18_24';
            ");
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP09a""}'
                WHERE field_key IN ('SPCP09a_25_34', 'SPCP09a_35_44', 'SPCP09a_45_54', 'SPCP09a_55_64', 'SPCP09a_65_plus');
            ");

            // Revert SPCP010a fields
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP010a"",""groupLabel"":""How many members of the Parent Carer Panel are there, broken down by ethnicity?"",""groupHint"":""For each ethnicity, give a figure as of 31 March 2026.""}'
                WHERE field_key = 'SPCP010a_white';
            ");
            migrationBuilder.Sql(@"
                UPDATE form_fields 
                SET configuration = '{""suffix"":""parents/carers"",""width"":""5"",""group"":""SPCP010a""}'
                WHERE field_key IN ('SPCP010a_mixed', 'SPCP010a_asian', 'SPCP010a_black', 'SPCP010a_other', 'SPCP010a_prefer_not_to_say');
            ");
        }
    }
}