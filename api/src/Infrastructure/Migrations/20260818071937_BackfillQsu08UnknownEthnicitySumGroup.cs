using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillQsu08UnknownEthnicitySumGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BLUM-7184. 20260326061458 gave the ethnicity boxes the configuration that ties them to
            // one another and to the QSU03 total - sumGroup 'ethnicity', maxSumField 'QSU03' - but
            // its key list stopped at QSU08_prefer_not_to_say and never mentioned QSU08_unknown.
            //
            // Every validator groups on configuration->>'sumGroup' and takes the ceiling from
            // configuration->>'maxSumField' (BulkUploadFieldValueValidator.ValidateSumGroups, the
            // client's validateSumGroups, ServiceForm.validateBreakdownSums), so a field carrying
            // neither is not part of the breakdown as far as any of them is concerned: users counted
            // under "Unknown" were left out of the ethnicity total, and an LA could report more people
            // by ethnicity than it reported in total without anything objecting.
            //
            // Data-only. jsonb_set on three keys, so the rest of the field's configuration - its
            // group, label, width, suffix - survives untouched. min is set to 0 rather than preserved,
            // matching what 20260326061458 did to the six siblings, so all seven agree. Idempotent:
            // the UPDATE is predicated on the row actually changing, so a second run touches nothing.
            migrationBuilder.Sql(@"
                UPDATE form_fields
                SET configuration = jsonb_set(
                        jsonb_set(
                            jsonb_set(COALESCE(configuration, '{}'::jsonb), '{sumGroup}', '""ethnicity""'::jsonb),
                            '{maxSumField}', '""QSU03""'::jsonb),
                        '{min}', '0'::jsonb),
                    updated_at = NOW()
                WHERE field_key = 'QSU08_unknown'
                  AND configuration IS DISTINCT FROM jsonb_set(
                        jsonb_set(
                            jsonb_set(COALESCE(configuration, '{}'::jsonb), '{sumGroup}', '""ethnicity""'::jsonb),
                            '{maxSumField}', '""QSU03""'::jsonb),
                        '{min}', '0'::jsonb);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only QSU08_unknown, unlike 20260326061458's Down which strips the whole QSU08_ prefix -
            // rolling this back must not take the six siblings that migration configured with it.
            migrationBuilder.Sql(@"
                UPDATE form_fields
                SET configuration = configuration - 'sumGroup' - 'maxSumField' - 'min',
                    updated_at = NOW()
                WHERE field_key = 'QSU08_unknown'
                  AND configuration IS NOT NULL;
            ");
        }
    }
}