using Domain.DataCollections.Forms;
using Domain.Users;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Forms;

public class FormFieldOptionConfiguration : IEntityTypeConfiguration<FormFieldOption>
{
    public void Configure(EntityTypeBuilder<FormFieldOption> builder)
    {
        builder.ToTable("form_field_options");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(x => x.Value, x => new FormFieldOptionId(x));

        builder.Property(x => x.FormFieldId)
            .HasColumnName("form_field_id")
            .HasConversion(x => x.Value, x => new FormFieldId(x))
            .IsRequired();

        builder.Property(x => x.Value)
            .HasColumnName("value")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(x => x.Label)
            .HasColumnName("label")
            .HasColumnType("varchar(500)")
            .IsRequired();

        builder.Property(x => x.DisplayOrder)
            .HasColumnName("display_order");

        builder.Property(x => x.IsDefault)
            .HasColumnName("is_default")
            .HasDefaultValue(false);

        builder.Property(x => x.TriggeredFieldId)
            .HasColumnName("triggered_field_id")
            .HasConversion(x => x!.Value, x => new FormFieldId(x))
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

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
            .HasConstraintName("fk_form_field_options_users_created_by_id")
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
            .HasConstraintName("fk_form_field_options_users_last_modified_by_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Relationships
        builder.HasOne(x => x.TriggeredField)
            .WithMany()
            .HasForeignKey(x => x.TriggeredFieldId)
            .HasConstraintName("fk_form_field_options_form_fields_triggered_field_id")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.FormFieldId, x.DisplayOrder });
    }
}