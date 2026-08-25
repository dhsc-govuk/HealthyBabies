using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillFormFieldValueSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BLUM-7178 / BLUM-7614. 20260727095439 added the self-describing snapshot columns to
            // form_field_values, but only rows saved after it was deployed carry one - every answer
            // submitted before that (and every bulk-uploaded row, which used the snapshot-less
            // SetFieldValue overload until BLUM-7614) still has question_label NULL and so keeps
            // being re-rendered against the live form. A prod audit on 22 July 2026 found ~13,000 of
            // 203,450 answers already displaced this way.
            //
            // Copying today's live definition onto those rows freezes them as they read now, which is
            // the point: from here on a question relabel in Admin Configuration cannot reach back and
            // change what a historic answer says. It is the best reconstruction available - the field
            // definition is mutated in place (FormField.UpdateDetails) with no history table - and it
            // is strictly better than leaving them following the live form forever.
            //
            // Matches what FormSubmission.SetFieldValue(FormField, ...) writes, so backfilled rows are
            // indistinguishable from freshly snapshotted ones:
            //   - form_fields.field_type holds FieldType.ToString(), which returns Value - the same
            //     string SetSnapshot stores.
            //   - json_agg yields NULL when a field has no options, matching SetSnapshot's
            //     Options.Count > 0 guard, and the Value/Label keys match the anonymous type
            //     System.Text.Json serialises there.
            //   - options are not filtered on is_active: the repositories Include(f => f.Options)
            //     unfiltered, so field.Options at save time includes inactive ones too.
            //
            // Data-only and additive. Only the six nullable snapshot columns are written - value and
            // display_value are untouched, and updated_at is deliberately left alone so an answer's
            // audit trail still reflects when the answer itself last changed. Idempotent: predicated
            // on question_label IS NULL, so a second run touches nothing and a genuine snapshot
            // written since 20260727095439 is never overwritten.
            migrationBuilder.Sql(@"
                UPDATE form_field_values v
                SET question_code    = f.field_key,
                    question_label   = f.label,
                    question_hint    = f.help_text,
                    field_type       = f.field_type,
                    display_order    = f.display_order,
                    options_snapshot = o.snapshot
                FROM form_fields f
                LEFT JOIN LATERAL (
                    SELECT json_agg(json_build_object('Value', opt.value, 'Label', opt.label)
                                    ORDER BY opt.display_order)::text AS snapshot
                    FROM form_field_options opt
                    WHERE opt.form_field_id = f.id
                ) o ON TRUE
                WHERE v.form_field_id = f.id
                  AND v.question_label IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty. Nulling the snapshot columns cannot tell a row this migration
            // backfilled from one SetFieldValue snapshotted at save time, so rolling back would
            // destroy genuine snapshots written since 20260727095439. The columns are nullable and
            // every read path falls back to the live field, so leaving them populated is harmless.
        }
    }
}