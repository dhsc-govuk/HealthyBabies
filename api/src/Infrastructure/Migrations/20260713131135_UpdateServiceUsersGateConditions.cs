using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateServiceUsersGateConditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Brings the live service-users module's gate conditions in line with the updated spec. The seeder
            // only builds this module when it is entirely missing, so already-seeded modules need this data
            // migration to gain the new QSU03 (>= 1) and SMD02/SMD09 (cross-entity, on the Service record) clauses.
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    module_id UUID;
                BEGIN
                    SELECT id INTO module_id FROM data_collection_form_modules WHERE code = 'service-users';

                    IF module_id IS NOT NULL THEN
                        -- QSU07/09/11/13A: also require at least one user (QSU03 >= 1) and a funded/partially
                        -- funded service (SMD02 in {0,1}) in addition to the existing QSU01 AND QSU02 gate.
                        UPDATE form_fields
                        SET conditional_rules = '{""showWhen"":{""allOf"":[{""fieldKey"":""QSU01"",""equals"":""yes""},{""fieldKey"":""QSU02"",""equals"":""yes""},{""fieldKey"":""QSU03"",""gte"":""1""},{""serviceFieldKey"":""SMD02"",""in"":[""0"",""1""]}]}}'
                        WHERE form_module_id = module_id AND field_key IN ('QSU07', 'QSU09', 'QSU11', 'QSU13A');

                        -- QSU13: QSU02 dropped per the updated spec; gate on service delivery (QSU01) and a
                        -- funded/partially funded service (SMD02 in {0,1}).
                        UPDATE form_fields
                        SET conditional_rules = '{""showWhen"":{""allOf"":[{""fieldKey"":""QSU01"",""equals"":""yes""},{""serviceFieldKey"":""SMD02"",""in"":[""0"",""1""]}]}}'
                        WHERE form_module_id = module_id AND field_key = 'QSU13';

                        -- QSU15: QSU02 dropped per the updated spec; only asked of funded services (SMD02 in {0,1})
                        -- on the perinatal-mental-health/parent-infant (1) or infant-feeding (2) strands (SMD09).
                        UPDATE form_fields
                        SET conditional_rules = '{""showWhen"":{""allOf"":[{""fieldKey"":""QSU01"",""equals"":""yes""},{""serviceFieldKey"":""SMD02"",""in"":[""0"",""1""]},{""serviceFieldKey"":""SMD09"",""in"":[""1"",""2""]}]}}'
                        WHERE form_module_id = module_id AND field_key = 'QSU15';
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the previous gate (QSU01 AND QSU02) for every field this migration touched.
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    module_id UUID;
                BEGIN
                    SELECT id INTO module_id FROM data_collection_form_modules WHERE code = 'service-users';

                    IF module_id IS NOT NULL THEN
                        UPDATE form_fields
                        SET conditional_rules = '{""showWhen"":{""allOf"":[{""fieldKey"":""QSU01"",""equals"":""yes""},{""fieldKey"":""QSU02"",""equals"":""yes""}]}}'
                        WHERE form_module_id = module_id AND field_key IN ('QSU07', 'QSU09', 'QSU11', 'QSU13A', 'QSU13', 'QSU15');
                    END IF;
                END $$;
            ");
        }
    }
}