using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RepairDeliveryPlanFieldKeyIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // field_key on these modules may already be wrong on databases that had submission data,
            // because an earlier reconciler matched fields by DisplayOrder rather than identity. Label
            // is the only property that survived unchanged and is unique per module, so it's the only
            // safe join key here. Every row in a module is parked on its own row id (always unique)
            // first, then rows whose label is still defined settle onto the real key by matching on
            // label. Parking is unconditional, not just for rows whose label matches something: a
            // retired field can already be squatting on a key another field needs to settle onto (the
            // same DisplayOrder corruption can leave a wrong-but-plausible code behind), so anything
            // left unmatched after settling keeps its parked value rather than colliding. The seeder
            // then decides a retired field's fate based on whether it still has answers.
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    module_id UUID;
                BEGIN
                    SELECT id INTO module_id FROM data_collection_form_modules WHERE code = 'focus-2026-2027';

                    IF module_id IS NOT NULL THEN
                        UPDATE form_fields
                        SET field_key = 'RETIRED_' || id, updated_at = NOW()
                        WHERE form_module_id = module_id;

                        UPDATE form_fields AS f
                        SET field_key = v.field_key, updated_at = NOW()
                        FROM (VALUES
                          ('F01', 'What is the top priority for the Delivery Grant?'),
                          ('F02', 'Describe the top priority for the Delivery Grant.'),
                          ('F03', 'You may provide a short rationale for your chosen top priority for the Delivery Grant.'),
                          ('F04', 'What is the top priority for Parenting?'),
                          ('F05', 'Describe the top priority for Parenting.'),
                          ('F06', 'You may provide a short rationale for your chosen top priority for Parenting.'),
                          ('F07', 'What is the top priority for HLE?'),
                          ('F08', 'Describe the top priority for HLE.'),
                          ('F09', 'You may provide a short rationale for your chosen top priority for HLE.'),
                          ('F10', 'What is the top priority for SEND?'),
                          ('F11', 'Describe the top priority for SEND.'),
                          ('F12', 'You may provide a short rationale for your chosen top priority for SEND.'),
                          ('F13', 'What is the top priority for PNMH & PIR?'),
                          ('F14', 'Describe the top priority for PNMH & PIR.'),
                          ('F15', 'You may provide a short rationale for your chosen top priority for PNMH & PIR.'),
                          ('F16', 'What is the top priority for Infant Feeding?'),
                          ('F17', 'Describe the top priority for Infant Feeding.'),
                          ('F18', 'You may provide a short rationale for your chosen top priority for Infant Feeding.'),
                          ('F19', 'What is the top priority for the Healthy Babies Offer?'),
                          ('F20', 'Describe the top priority for Healthy Babies Offer.'),
                          ('F21', 'You may provide a short rationale for your chosen top priority for the Healthy Babies Offer.')
                        ) AS v(field_key, label)
                        WHERE f.form_module_id = module_id AND f.label = v.label;
                    END IF;

                    SELECT id INTO module_id FROM data_collection_form_modules WHERE code = 'delivery-grant';

                    IF module_id IS NOT NULL THEN
                        UPDATE form_fields
                        SET field_key = 'RETIRED_' || id, updated_at = NOW()
                        WHERE form_module_id = module_id;

                        UPDATE form_fields AS f
                        SET field_key = v.field_key, updated_at = NOW()
                        FROM (VALUES
                          ('DG01', 'Activity ID'),
                          ('DG02', 'Select the overarching theme or area of focus'),
                          ('DG03', 'Provide further details for the theme selected'),
                          ('DG04', 'Enter the name and description of all activities delivered within this theme'),
                          ('DG05', 'Is this a new or continuing activity?'),
                          ('DG05.1', 'Is this activity funding service(s)?'),
                          ('DG05.2', 'Which service is the activity funding?'),
                          ('DG06', 'Select the activity status'),
                          ('DG07', 'Select the delivery status'),
                          ('DG08', 'Is this activity funded by other sources than Best Start Family Hubs and Healthy Babies?'),
                          ('DG09', 'Total amount of additional funding'),
                          ('DG10', 'Source of additional funding'),
                          ('DG11', 'How much Delivery Grant funding are you forecasting to spend on this activity in 2026-2027?'),
                          ('DG12', 'Total Activity Budget (26/27)'),
                          ('DG13', 'What is the total amount of actual spend on this activity to date?'),
                          ('DG14', 'What is the amount of funding committed to this activity?'),
                          ('DG15', 'Planned Spend (Uncommitted)'),
                          ('DG16', 'How much funding are you forecasting to spend on this activity in 2027-2028?'),
                          ('DG17', 'How much funding are you forecasting to spend on this activity in 2028-2029?'),
                          ('DG18', 'Briefly outline what activity changed, why and when.'),
                          ('DG19', 'Are there any risks associated with this activity?'),
                          ('DG20', 'Outline the identified risks'),
                          ('DG21', 'Select the likelihood of this risk'),
                          ('DG22', 'Select the impact of this risk'),
                          ('DG23', 'Give details of the mitigations in place or planned')
                        ) AS v(field_key, label)
                        WHERE f.form_module_id = module_id AND f.label = v.label;
                    END IF;

                    SELECT id INTO module_id FROM data_collection_form_modules WHERE code = 'parenting';

                    IF module_id IS NOT NULL THEN
                        UPDATE form_fields
                        SET field_key = 'RETIRED_' || id, updated_at = NOW()
                        WHERE form_module_id = module_id;

                        UPDATE form_fields AS f
                        SET field_key = v.field_key, updated_at = NOW()
                        FROM (VALUES
                          ('P01', 'Activity ID'),
                          ('P02', 'Select the overarching theme or area of focus'),
                          ('P03', 'Describe the theme or area of focus'),
                          ('P04', 'Enter the name and description of all activities delivered within this theme'),
                          ('P05', 'Which expectations do these activities meet?'),
                          ('P05.1', 'Is this activity funding service(s)?'),
                          ('P05.2', 'Which service is the activity funding?'),
                          ('P06', 'Is this a new or continuing activity?'),
                          ('P07', 'Select the activity status'),
                          ('P08', 'Select the delivery status of this activity'),
                          ('P09', 'Is this activity receiving any additional funding from other sources?'),
                          ('P10', 'Total amount of additional funding'),
                          ('P11', 'Source of additional funding'),
                          ('P12', 'How much Parenting funding are you forecasting to spend in 2026-27?'),
                          ('P13', 'Total Activity Budget (26/27)'),
                          ('P14', 'What is the total amount of actual spend on this activity to date?'),
                          ('P15', 'What is the amount of funding committed to this activity?'),
                          ('P16', 'Planned Spend (Uncommitted)'),
                          ('P17', 'How much funding are you forecasting to spend in 2027-2028?'),
                          ('P18', 'How much funding are you forecasting to spend in 2028-2029?'),
                          ('P19', 'Briefly outline what activity changed, why and when.'),
                          ('P20', 'Are there any risks associated with this activity?'),
                          ('P21', 'Outline the identified risks'),
                          ('P22', 'Select the likelihood of this risk'),
                          ('P23', 'Select the impact of this risk'),
                          ('P24', 'Give details of the mitigations in place or planned')
                        ) AS v(field_key, label)
                        WHERE f.form_module_id = module_id AND f.label = v.label;
                    END IF;

                    SELECT id INTO module_id FROM data_collection_form_modules WHERE code = 'hle';

                    IF module_id IS NOT NULL THEN
                        UPDATE form_fields
                        SET field_key = 'RETIRED_' || id, updated_at = NOW()
                        WHERE form_module_id = module_id;

                        UPDATE form_fields AS f
                        SET field_key = v.field_key, updated_at = NOW()
                        FROM (VALUES
                          ('H01', 'Activity ID'),
                          ('H02', 'Select the overarching theme or area of focus'),
                          ('H03', 'Describe the theme or area of focus'),
                          ('H04', 'Enter the name and description of all activities delivered within this theme'),
                          ('H05', 'Which expectations do these activities meet?'),
                          ('H05.1', 'Is this activity funding service(s)?'),
                          ('H05.2', 'Which service is the activity funding?'),
                          ('H06', 'Is this a new or continuing activity?'),
                          ('H07', 'Select the activity status'),
                          ('H08', 'Select the delivery status of this activity'),
                          ('H09', 'Is this activity receiving any additional funding from other sources?'),
                          ('H10', 'Total amount of additional funding'),
                          ('H11', 'Source of additional funding'),
                          ('H12', 'How much HLE funding are you forecasting to spend in 2026-27?'),
                          ('H13', 'Total Activity Budget (26/27)'),
                          ('H14', 'What is the total amount of actual spend on this activity to date?'),
                          ('H15', 'What is the amount of funding committed to this activity?'),
                          ('H16', 'Planned Spend (Uncommitted)'),
                          ('H17', 'How much funding are you forecasting to spend in 2027-2028?'),
                          ('H18', 'How much funding are you forecasting to spend in 2028-2029?'),
                          ('H19', 'Briefly outline what activity changed, why and when.'),
                          ('H20', 'Are there any risks associated with this activity?'),
                          ('H21', 'Outline the identified risks'),
                          ('H22', 'Select the likelihood of this risk'),
                          ('H23', 'Select the impact of this risk'),
                          ('H24', 'Give details of the mitigations in place or planned')
                        ) AS v(field_key, label)
                        WHERE f.form_module_id = module_id AND f.label = v.label;
                    END IF;

                    SELECT id INTO module_id FROM data_collection_form_modules WHERE code = 'send';

                    IF module_id IS NOT NULL THEN
                        UPDATE form_fields
                        SET field_key = 'RETIRED_' || id, updated_at = NOW()
                        WHERE form_module_id = module_id;

                        UPDATE form_fields AS f
                        SET field_key = v.field_key, updated_at = NOW()
                        FROM (VALUES
                          ('SEND01', 'Activity ID'),
                          ('SEND02', 'Select the overarching theme or area of focus'),
                          ('SEND03', 'Provide further details for the theme selected'),
                          ('SEND04', 'Enter the name and description of all activities delivered within this funded strand'),
                          ('SEND05', 'Which expectations do these activities meet?'),
                          ('SEND05.1', 'Is this activity funding service(s)?'),
                          ('SEND05.2', 'Which service is the activity funding?'),
                          ('SEND06', 'Is this a new or continuing activity?'),
                          ('SEND07', 'Select the activity status'),
                          ('SEND08', 'Select the delivery status of this activity'),
                          ('SEND09', 'Is this activity receiving any additional funding from other sources?'),
                          ('SEND10', 'Total amount of additional funding'),
                          ('SEND11', 'Source of additional funding'),
                          ('SEND12', 'How much SEND funding are you forecasting to spend in 2026-27?'),
                          ('SEND13', 'Total Activity Budget (26/27)'),
                          ('SEND14', 'What is the total amount of actual spend on this activity to date?'),
                          ('SEND15', 'What is the amount of funding committed to this activity?'),
                          ('SEND16', 'Planned Spend (Uncommitted)'),
                          ('SEND17', 'How much funding are you forecasting to spend in 2027-2028?'),
                          ('SEND18', 'How much funding are you forecasting to spend in 2028-2029?'),
                          ('SEND19', 'Briefly outline what activity changed, why and when.'),
                          ('SEND20', 'Are there any risks associated with this activity?'),
                          ('SEND21', 'Outline the identified risks'),
                          ('SEND22', 'Select the likelihood of this risk'),
                          ('SEND23', 'Select the impact of this risk'),
                          ('SEND24', 'Give details of the mitigations in place or planned')
                        ) AS v(field_key, label)
                        WHERE f.form_module_id = module_id AND f.label = v.label;
                    END IF;

                    SELECT id INTO module_id FROM data_collection_form_modules WHERE code = 'pnmh-and-pir';

                    IF module_id IS NOT NULL THEN
                        UPDATE form_fields
                        SET field_key = 'RETIRED_' || id, updated_at = NOW()
                        WHERE form_module_id = module_id;

                        UPDATE form_fields AS f
                        SET field_key = v.field_key, updated_at = NOW()
                        FROM (VALUES
                          ('PN01', 'Activity ID'),
                          ('PN02', 'Provide the name and a brief description of the activity.'),
                          ('PN03', 'Select the activity type'),
                          ('PN04', 'Which expectations does this activity meet?'),
                          ('PN05', 'Is this a new or continuing activity?'),
                          ('PN05.1', 'Is this activity funding service(s)?'),
                          ('PN05.2', 'Which service is the activity funding?'),
                          ('PN06', 'Provide the planned start date for this activity'),
                          ('PN07', 'Provide the actual date from which this activity commenced'),
                          ('PN08', 'Provide the date you expect this activity to be completed'),
                          ('PN09', 'Provide the date this activity was completed'),
                          ('PN10', 'Select the activity status'),
                          ('PN11', 'Select the delivery status of this activity'),
                          ('PN12', 'Is this activity receiving any additional funding from other sources?'),
                          ('PN13', 'Total amount of additional funding'),
                          ('PN14', 'Source of additional funding'),
                          ('PN15', 'How much PNMH & PIR funding are you forecasting to spend in 2026-27?'),
                          ('PN16', 'Total Activity Budget (26/27)'),
                          ('PN17', 'What is the total amount of actual spend on this activity to date?'),
                          ('PN18', 'What is the amount of funding committed to this activity?'),
                          ('PN19', 'Planned Spend (Uncommitted)'),
                          ('PN20', 'How much funding are you forecasting to spend in 2027-2028?'),
                          ('PN21', 'How much funding are you forecasting to spend in 2028-2029?'),
                          ('PN22', 'Briefly outline what activity changed, why and when.'),
                          ('PN23', 'Provide a narrative of how this activity is progressing.')
                        ) AS v(field_key, label)
                        WHERE f.form_module_id = module_id AND f.label = v.label;
                    END IF;

                    SELECT id INTO module_id FROM data_collection_form_modules WHERE code = 'pnmh-and-pir-risk';

                    IF module_id IS NOT NULL THEN
                        UPDATE form_fields
                        SET field_key = 'RETIRED_' || id, updated_at = NOW()
                        WHERE form_module_id = module_id;

                        UPDATE form_fields AS f
                        SET field_key = v.field_key, updated_at = NOW()
                        FROM (VALUES
                          ('PN24', 'What are the risks to successful delivery of this strand?')
                        ) AS v(field_key, label)
                        WHERE f.form_module_id = module_id AND f.label = v.label;
                    END IF;

                    SELECT id INTO module_id FROM data_collection_form_modules WHERE code = 'infant-feeding';

                    IF module_id IS NOT NULL THEN
                        UPDATE form_fields
                        SET field_key = 'RETIRED_' || id, updated_at = NOW()
                        WHERE form_module_id = module_id;

                        UPDATE form_fields AS f
                        SET field_key = v.field_key, updated_at = NOW()
                        FROM (VALUES
                          ('IF01', 'Activity ID'),
                          ('IF02', 'Provide the name and a brief description of the activity.'),
                          ('IF03', 'Select the activity type'),
                          ('IF04', 'Which expectations does this activity meet?'),
                          ('IF05', 'Is this a new or continuing activity?'),
                          ('IF05.1', 'Is this activity funding service(s)?'),
                          ('IF05.2', 'Which service is the activity funding?'),
                          ('IF06', 'Provide the planned start date for this activity'),
                          ('IF07', 'Provide the actual date from which this activity commenced'),
                          ('IF08', 'Provide the date you expect this activity to be completed'),
                          ('IF09', 'Provide the date this activity was completed'),
                          ('IF10', 'Select the activity status'),
                          ('IF11', 'Select the delivery status of this activity'),
                          ('IF12', 'Is this activity receiving any additional funding from other sources?'),
                          ('IF13', 'Total amount of additional funding'),
                          ('IF14', 'Source of additional funding'),
                          ('IF15', 'How much Infant Feeding funding are you forecasting to spend in 2026-27?'),
                          ('IF16', 'Total Activity Budget (26/27)'),
                          ('IF17', 'What is the total amount of actual spend on this activity to date?'),
                          ('IF18', 'What is the amount of funding committed to this activity?'),
                          ('IF19', 'Planned Spend (Uncommitted)'),
                          ('IF20', 'How much funding are you forecasting to spend in 2027-2028?'),
                          ('IF21', 'How much funding are you forecasting to spend in 2028-2029?'),
                          ('IF22', 'Briefly outline what activity changed, why and when.'),
                          ('IF23', 'Provide a narrative of how this activity is progressing.')
                        ) AS v(field_key, label)
                        WHERE f.form_module_id = module_id AND f.label = v.label;
                    END IF;

                    SELECT id INTO module_id FROM data_collection_form_modules WHERE code = 'infant-feeding-risk';

                    IF module_id IS NOT NULL THEN
                        UPDATE form_fields
                        SET field_key = 'RETIRED_' || id, updated_at = NOW()
                        WHERE form_module_id = module_id;

                        UPDATE form_fields AS f
                        SET field_key = v.field_key, updated_at = NOW()
                        FROM (VALUES
                          ('IF24', 'What are the risks to successful delivery of this strand?')
                        ) AS v(field_key, label)
                        WHERE f.form_module_id = module_id AND f.label = v.label;
                    END IF;

                    SELECT id INTO module_id FROM data_collection_form_modules WHERE code = 'healthy-babies-offer';

                    IF module_id IS NOT NULL THEN
                        UPDATE form_fields
                        SET field_key = 'RETIRED_' || id, updated_at = NOW()
                        WHERE form_module_id = module_id;

                        UPDATE form_fields AS f
                        SET field_key = v.field_key, updated_at = NOW()
                        FROM (VALUES
                          ('HBO01', 'Activity ID'),
                          ('HBO02', 'Provide the name and a brief description of the activity.'),
                          ('HBO03', 'Select the activity type'),
                          ('HBO04', 'Which expectations does this activity meet?'),
                          ('HBO05', 'Is this a new or continuing activity?'),
                          ('HBO05.1', 'Is this activity funding service(s)?'),
                          ('HBO05.2', 'Which service is the activity funding?'),
                          ('HBO06', 'Provide the planned start date for this activity'),
                          ('HBO07', 'Provide the actual date from which this activity commenced'),
                          ('HBO08', 'Provide the date you expect this activity to be completed'),
                          ('HBO09', 'Provide the date this activity was completed'),
                          ('HBO10', 'Select the activity status'),
                          ('HBO11', 'Select the delivery status of this activity'),
                          ('HBO12', 'Is this activity receiving any additional funding from other sources?'),
                          ('HBO13', 'Total amount of additional funding'),
                          ('HBO14', 'Source of additional funding'),
                          ('HBO15', 'How much Healthy Babies Offer funding are you forecasting to spend in 2026-27?'),
                          ('HBO16', 'Total Activity Budget (26/27)'),
                          ('HBO17', 'What is the total amount of actual spend on this activity to date?'),
                          ('HBO18', 'What is the amount of funding committed to this activity?'),
                          ('HBO19', 'Planned Spend (Uncommitted)'),
                          ('HBO20', 'How much funding are you forecasting to spend in 2027-2028?'),
                          ('HBO21', 'How much funding are you forecasting to spend in 2028-2029?'),
                          ('HBO22', 'Briefly outline what activity changed, why and when.'),
                          ('HBO23', 'Provide a narrative of how this activity is progressing.')
                        ) AS v(field_key, label)
                        WHERE f.form_module_id = module_id AND f.label = v.label;
                    END IF;

                    SELECT id INTO module_id FROM data_collection_form_modules WHERE code = 'healthy-babies-offer-risk';

                    IF module_id IS NOT NULL THEN
                        UPDATE form_fields
                        SET field_key = 'RETIRED_' || id, updated_at = NOW()
                        WHERE form_module_id = module_id;

                        UPDATE form_fields AS f
                        SET field_key = v.field_key, updated_at = NOW()
                        FROM (VALUES
                          ('HBO24', 'What are the risks to successful delivery of this strand?')
                        ) AS v(field_key, label)
                        WHERE f.form_module_id = module_id AND f.label = v.label;
                    END IF;

                    SELECT id INTO module_id FROM data_collection_form_modules WHERE code = 'rollover-spend';

                    IF module_id IS NOT NULL THEN
                        UPDATE form_fields
                        SET field_key = 'RETIRED_' || id, updated_at = NOW()
                        WHERE form_module_id = module_id;

                        UPDATE form_fields AS f
                        SET field_key = v.field_key, updated_at = NOW()
                        FROM (VALUES
                          ('RO01', 'Select the strand this activity refers to'),
                          ('RO02', 'Provide the name and a brief description of the activity'),
                          ('RO03', 'Select the activity type'),
                          ('RO03.1', 'Is this activity funding service(s)?'),
                          ('RO03.2', 'Which service is the activity funding?'),
                          ('RO04', 'Provide the planned start date for this activity'),
                          ('RO05', 'Provide the actual date from which this activity commenced'),
                          ('RO06', 'Provide the date you expect this activity to be completed'),
                          ('RO07', 'Provide the date this activity was completed'),
                          ('RO08', 'What is the status of this activity?'),
                          ('RO09', 'Select the year of this rollover'),
                          ('RO10', 'Provide the total amount of 2025-26 funding planned for this activity'),
                          ('RO11', 'Provide the total amount of actual spend on this activity to date'),
                          ('RO12', 'Provide the amount of funding that is committed to this activity'),
                          ('RO13', '2025-26 Planned Spend'),
                          ('RO14', 'Provide a narrative of how this activity is progressing')
                        ) AS v(field_key, label)
                        WHERE f.form_module_id = module_id AND f.label = v.label;
                    END IF;

                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not reversible: the pre-migration field_key values were themselves wrong, so there is no
            // correct prior state to restore.
        }
    }
}