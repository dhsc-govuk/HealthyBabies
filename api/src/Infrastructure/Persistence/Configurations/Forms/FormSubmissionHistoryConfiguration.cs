using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Users;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Forms;

public class FormSubmissionHistoryConfiguration : IEntityTypeConfiguration<FormSubmissionHistory>
{
    public void Configure(EntityTypeBuilder<FormSubmissionHistory> builder)
    {
        builder.ToTable("form_submission_histories");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(x => x.Value, x => new FormSubmissionHistoryId(x));

        builder.Property(x => x.FormSubmissionId)
            .HasColumnName("form_submission_id")
            .HasConversion(x => x.Value, x => new FormSubmissionId(x))
            .IsRequired();

        builder.Property(x => x.PreviousStatus)
            .HasColumnName("previous_status")
            .HasColumnType("varchar(50)")
            .HasConversion(x => x.ToString(), x => SubmissionStatus.From(x))
            .IsRequired();

        builder.Property(x => x.NewStatus)
            .HasColumnName("new_status")
            .HasColumnType("varchar(50)")
            .HasConversion(x => x.ToString(), x => SubmissionStatus.From(x))
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.ChangedAt)
            .HasColumnName("changed_at")
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired();

        // Audit fields
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasConversion(new DateTimeUtcConverter())
            .HasDefaultValueSql("timezone('utc', now())");

        builder.Property(x => x.CreatedById)
            .HasColumnName("created_by_id")
            .HasConversion(x => x!.Value, x => new UserId(x))
            .IsRequired(false);
        builder.HasOne<User>(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .HasConstraintName("fk_form_submission_histories_users_created_by_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);

        builder.Property(x => x.LastModifiedById)
            .HasColumnName("last_modified_by_id")
            .HasConversion(x => x!.Value, x => new UserId(x))
            .IsRequired(false);
        builder.HasOne<User>(x => x.LastModifiedBy)
            .WithMany()
            .HasForeignKey(x => x.LastModifiedById)
            .HasConstraintName("fk_form_submission_histories_users_last_modified_by_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.FormSubmissionId);

        // Query filter to match parent's soft delete filter
        builder.HasQueryFilter(x => !x.FormSubmission!.IsDeleted);
    }
}