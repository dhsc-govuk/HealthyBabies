using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Organisations;
using Domain.Users;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Forms;

public class FormSubmissionConfiguration : IEntityTypeConfiguration<FormSubmission>
{
    public void Configure(EntityTypeBuilder<FormSubmission> builder)
    {
        builder.ToTable("form_submissions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new FormSubmissionId(x));

        builder.Property(x => x.FormModuleId)
            .HasConversion(x => x.Value, x => new DataCollectionFormModuleId(x))
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnType("varchar(50)")
            .HasConversion(x => x.ToString(), x => SubmissionStatus.From(x))
            .HasDefaultValue(SubmissionStatus.Draft)
            .IsRequired();

        builder.Property(x => x.EntityType)
            .HasColumnType("varchar(100)")
            .IsRequired(false);

        builder.Property(x => x.EntityId)
            .IsRequired(false);

        builder.Property(x => x.OrganisationId)
            .HasConversion(x => x!.Value, x => new OrganisationId(x))
            .IsRequired(false);

        builder.Property(x => x.DataCollectionId)
            .HasConversion(x => x!.Value, x => new DataCollectionId(x))
            .IsRequired(false);

        builder.HasIndex(x => new { x.OrganisationId, x.DataCollectionId, x.FormModuleId });
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.FormModuleId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.SubmittedAt);

        builder.Property(x => x.DraftSavedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);

        builder.Property(x => x.SubmittedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);

        builder.Property(x => x.SubmittedById)
            .HasConversion(x => x!.Value, x => new UserId(x))
            .IsRequired(false);
        builder.HasOne<User>(x => x.SubmittedBy)
            .WithMany()
            .HasForeignKey(x => x.SubmittedById)
            .HasConstraintName("fk_form_submissions_users_submitted_by_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.ReviewedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);

        builder.Property(x => x.ReviewedById)
            .HasConversion(x => x!.Value, x => new UserId(x))
            .IsRequired(false);
        builder.HasOne<User>(x => x.ReviewedBy)
            .WithMany()
            .HasForeignKey(x => x.ReviewedById)
            .HasConstraintName("fk_form_submissions_users_reviewed_by_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.ReviewNotes)
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.SubmitterIpAddress)
            .HasColumnType("varchar(45)")
            .IsRequired(false);

        builder.Property(x => x.SubmitterUserAgent)
            .HasColumnType("varchar(500)")
            .IsRequired(false);

        // Audit fields
        builder.Property(x => x.CreatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .HasDefaultValueSql("timezone('utc', now())");

        builder.Property(x => x.CreatedById)
            .HasConversion(x => x!.Value, x => new UserId(x))
            .IsRequired(false);
        builder.HasOne<User>(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .HasConstraintName("fk_form_submissions_users_created_by_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.UpdatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);

        builder.Property(x => x.LastModifiedById)
            .HasConversion(x => x!.Value, x => new UserId(x))
            .IsRequired(false);
        builder.HasOne<User>(x => x.LastModifiedBy)
            .WithMany()
            .HasForeignKey(x => x.LastModifiedById)
            .HasConstraintName("fk_form_submissions_users_last_modified_by_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Soft delete fields
        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(x => x.DeletedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);

        builder.Property(x => x.DeletedById)
            .HasConversion(x => x!.Value, x => new UserId(x))
            .IsRequired(false);
        builder.HasOne<User>(x => x.DeletedBy)
            .WithMany()
            .HasForeignKey(x => x.DeletedById)
            .HasConstraintName("fk_form_submissions_users_deleted_by_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);

        // Relationships
        builder.HasOne(x => x.FormModule)
            .WithMany()
            .HasForeignKey(x => x.FormModuleId)
            .HasConstraintName("fk_form_submissions_form_modules_form_module_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.FieldValues)
            .WithOne(x => x.FormSubmission)
            .HasForeignKey(x => x.FormSubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.History)
            .WithOne(x => x.FormSubmission)
            .HasForeignKey(x => x.FormSubmissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}