using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class RevertIncompleteBulkUploadSubmissions : Migration
{
    private const string RevertNote = "Reverted by data-fix migration: bulk upload previously marked this submission Approved without all required fields present.";

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // BulkUploadRowProcessor used to call MarkAsComplete() unconditionally on every row it
        // processed, regardless of whether the row supplied values for the module's required fields.
        // This reverts any "Service"/"OutcomeScore" submission that was marked Approved that way but
        // is still missing at least one required field's value, back to Draft.
        migrationBuilder.Sql($@"
                DO $$
                BEGIN
                    CREATE TEMP TABLE _incomplete_submissions ON COMMIT DROP AS
                    SELECT DISTINCT fs.id
                    FROM form_submissions fs
                    JOIN form_fields ff ON ff.form_module_id = fs.form_module_id
                        AND ff.is_required = true
                        AND ff.is_active = true
                        AND NOT (fs.entity_type = 'OutcomeScore' AND ff.field_key = 'PPS01')
                    WHERE fs.status = 'approved'
                      AND fs.entity_type IN ('Service', 'OutcomeScore')
                      AND NOT fs.is_deleted
                      AND NOT EXISTS (
                          SELECT 1 FROM form_field_values ffv
                          WHERE ffv.form_submission_id = fs.id
                            AND ffv.form_field_id = ff.id
                            AND ffv.value IS NOT NULL AND ffv.value <> ''
                      );

                    INSERT INTO form_submission_histories (id, form_submission_id, previous_status, new_status, notes, changed_at, created_at)
                    SELECT gen_random_uuid(), id, 'approved', 'draft', '{RevertNote}', NOW(), NOW()
                    FROM _incomplete_submissions;

                    UPDATE form_submissions
                    SET status = 'draft', reviewed_at = NULL
                    WHERE id IN (SELECT id FROM _incomplete_submissions);
                END $$;
            ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($@"
                DO $$
                BEGIN
                    UPDATE form_submissions fs
                    SET status = 'approved'
                    WHERE fs.status = 'draft'
                      AND EXISTS (
                          SELECT 1 FROM form_submission_histories h
                          WHERE h.form_submission_id = fs.id
                            AND h.notes = '{RevertNote}'
                      );

                    DELETE FROM form_submission_histories
                    WHERE notes = '{RevertNote}';
                END $$;
            ");
    }
}