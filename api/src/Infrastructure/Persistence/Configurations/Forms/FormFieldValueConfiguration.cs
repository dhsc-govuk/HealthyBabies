using Domain.DataCollections.Forms;
using Domain.Users;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Forms;

public class FormFieldValueConfiguration : IEntityTypeConfiguration<FormFieldValue>
{
    public void Configure(EntityTypeBuilder<FormFieldValue> builder)
    {
        builder.ToTable("form_field_values");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(x => x.Value, x => new FormFieldValueId(x));

        builder.Property(x => x.FormSubmissionId)
            .HasColumnName("form_submission_id")
            .HasConversion(x => x.Value, x => new FormSubmissionId(x))
            .IsRequired();

        builder.Property(x => x.FormFieldId)
            .HasColumnName("form_field_id")
            .HasConversion(x => x.Value, x => new FormFieldId(x))
            .IsRequired();

        builder.HasIndex(x => new { x.FormSubmissionId, x.FormFieldId }).IsUnique();

        builder.Property(x => x.Value)
            .HasColumnName("value")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.DisplayValue)
            .HasColumnName("display_value")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.FileReference)
            .HasColumnName("file_reference")
            .HasColumnType("varchar(1000)")
            .IsRequired(false);

        builder.Property(x => x.FileName)
            .HasColumnName("file_name")
            .HasColumnType("varchar(255)")
            .IsRequired(false);

        builder.Property(x => x.FileSize)
            .HasColumnName("file_size")
            .IsRequired(false);

        builder.Property(x => x.FileMimeType)
            .HasColumnName("file_mime_type")
            .HasColumnType("varchar(100)")
            .IsRequired(false);

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
            .HasConstraintName("fk_form_field_values_users_created_by_id")
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
            .HasConstraintName("fk_form_field_values_users_last_modified_by_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Relationships
        builder.HasOne(x => x.FormField)
            .WithMany()
            .HasForeignKey(x => x.FormFieldId)
            .HasConstraintName("fk_form_field_values_form_fields_form_field_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Query filter to match parent's soft delete filter
        builder.HasQueryFilter(x => !x.FormSubmission!.IsDeleted);
    }
}