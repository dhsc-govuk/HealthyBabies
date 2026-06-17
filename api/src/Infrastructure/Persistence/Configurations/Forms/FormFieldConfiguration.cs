using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Users;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Forms;

public class FormFieldConfiguration : IEntityTypeConfiguration<FormField>
{
    public void Configure(EntityTypeBuilder<FormField> builder)
    {
        builder.ToTable("form_fields");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new FormFieldId(x));

        builder.Property(x => x.FormModuleId)
            .HasConversion(x => x.Value, x => new DataCollectionFormModuleId(x))
            .IsRequired();

        builder.Property(x => x.FormSectionId)
            .HasConversion(x => x!.Value, x => new FormSectionId(x))
            .IsRequired(false);

        builder.Property(x => x.FieldKey)
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.HasIndex(x => new { x.FormModuleId, x.FieldKey }).IsUnique();

        builder.Property(x => x.Label)
            .HasColumnType("varchar(500)")
            .IsRequired();

        builder.Property(x => x.FieldType)
            .HasColumnType("varchar(50)")
            .HasConversion(x => x.ToString(), x => FieldType.From(x))
            .IsRequired();

        builder.Property(x => x.DisplayOrder);

        builder.Property(x => x.IsRequired)
            .HasDefaultValue(false);

        builder.Property(x => x.Placeholder)
            .HasColumnType("varchar(500)")
            .IsRequired(false);

        builder.Property(x => x.HelpText)
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.DefaultValue)
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.ValidationRules)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.ConditionalRules)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.Configuration)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(x => x.ParentFieldId)
            .HasConversion(x => x!.Value, x => new FormFieldId(x))
            .IsRequired(false);

        builder.Property(x => x.DataSourceId)
            .HasConversion(x => x!.Value, x => new DataSourceId(x))
            .IsRequired(false);

        builder.Property(x => x.DataSourceParentValue)
            .HasColumnType("varchar(255)")
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

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
            .HasConstraintName("fk_form_fields_users_created_by_id")
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
            .HasConstraintName("fk_form_fields_users_last_modified_by_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Relationships
        builder.HasOne(x => x.FormSection)
            .WithMany()
            .HasForeignKey(x => x.FormSectionId)
            .HasConstraintName("fk_form_fields_form_sections_form_section_id")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.ParentField)
            .WithMany()
            .HasForeignKey(x => x.ParentFieldId)
            .HasConstraintName("fk_form_fields_form_fields_parent_field_id")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.DataSource)
            .WithMany()
            .HasForeignKey(x => x.DataSourceId)
            .HasConstraintName("fk_form_fields_data_sources_data_source_id")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Options)
            .WithOne(x => x.FormField)
            .HasForeignKey(x => x.FormFieldId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship to FormModule is configured in DataCollectionFormModulesConfiguration
    }
}