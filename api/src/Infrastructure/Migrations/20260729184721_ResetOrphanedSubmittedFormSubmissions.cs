using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ResetOrphanedSubmittedFormSubmissions : Migration
    {
        private const string RevertNote = "Reverted by data-fix migration: SubmitSubmission previously committed one FormSubmission at a time, so a mid-request " +
            "timeout (BLUM-7047) could leave some records flipped to Submitted while the parent DataCollectionSubmission was never created.";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SubmitSubmission used to call SaveChangesAsync once per FormSubmission record instead of once
            // for the whole operation. If the request was cut off part-way through (e.g. by the platform's
            // ingress timeout on a large submission), some records were left committed as "submitted" even
            // though the parent DataCollectionSubmission row was never written - the quarter never actually
            // finished submitting. This reverts any such orphaned "submitted" records back to "approved" (the
            // pre-submission complete state) so the organisation can cleanly resubmit once the atomicity fix
            // for BLUM-7047 is deployed.
            //
            // Detection is deliberately narrow: only "no DataCollectionSubmission row exists at all" counts as
            // orphaned. A legitimately reopened submission also has "submitted" children with no matching
            // Submitted/Approved parent (Reopen() flips the parent to InProgress but never touches children),
            // so checking the parent's *status* instead of its *existence* would misfire on every reopened,
            // not-yet-resubmitted quarter. A missing parent row can only happen if SubmitSubmission's loop
            // never reached the parent-creation code, which is exactly and only the crash scenario.
            migrationBuilder.Sql($@"
                DO $$
                BEGIN
                    CREATE TEMP TABLE _orphaned_submitted_submissions ON COMMIT DROP AS
                    SELECT DISTINCT fs.id
                    FROM form_submissions fs
                    WHERE fs.status = 'submitted'
                      AND NOT fs.is_deleted
                      AND NOT EXISTS (
                          SELECT 1 FROM data_collection_submissions dcs
                          WHERE dcs.organisation_id = fs.organisation_id
                            AND dcs.data_collection_id = fs.data_collection_id
                      );

                    INSERT INTO form_submission_histories (id, form_submission_id, previous_status, new_status, notes, changed_at, created_at)
                    SELECT gen_random_uuid(), id, 'submitted', 'approved', '{RevertNote}', NOW(), NOW()
                    FROM _orphaned_submitted_submissions;

                    UPDATE form_submissions
                    SET status = 'approved', submitted_at = NULL, submitted_by_id = NULL
                    WHERE id IN (SELECT id FROM _orphaned_submitted_submissions);
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
                    SET status = 'submitted'
                    WHERE fs.status = 'approved'
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
}