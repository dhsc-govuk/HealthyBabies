using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixWSDM02QuestionType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE service_category_form_questions 
                SET question_type = 2,
                    updated_at = NOW()
                WHERE code = 'WSDM02';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE service_category_form_questions 
                SET question_type = 4,
                    updated_at = NOW()
                WHERE code = 'WSDM02';
            ");
        }
    }
}