using Domain.DataCollections;
using Domain.DataCollections.Forms.ValueObjects;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DataCollectionFormModulesConfiguration : IEntityTypeConfiguration<DataCollectionFormModule>
{
    public void Configure(EntityTypeBuilder<DataCollectionFormModule> builder)
    {
        builder.ToTable("data_collection_form_modules");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new DataCollectionFormModuleId(x));

        builder.Property(x => x.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("ix_data_collection_form_modules_code");

        builder.Property(x => x.SectionNumber)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.LastChangedOn)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasConversion(new DateTimeUtcConverter());

        builder.Property(x => x.UpdatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);

        builder.Property(x => x.CreatedById)
            .HasConversion(
                x => x != null ? x.Value : (Guid?)null,
                x => x.HasValue ? new Domain.Users.UserId(x.Value) : null);

        builder.Property(x => x.LastModifiedById)
            .HasConversion(
                x => x != null ? x.Value : (Guid?)null,
                x => x.HasValue ? new Domain.Users.UserId(x.Value) : null);

        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LastModifiedBy)
            .WithMany()
            .HasForeignKey(x => x.LastModifiedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Status)
            .HasConversion(
                x => x.Value,
                x => FormStatus.From(x))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.PublishedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);

        builder.Property(x => x.PublishedById)
            .HasConversion(
                x => x != null ? x.Value : (Guid?)null,
                x => x.HasValue ? new Domain.Users.UserId(x.Value) : null);

        builder.HasOne(x => x.PublishedBy)
            .WithMany()
            .HasForeignKey(x => x.PublishedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.ValidationSchema);

        builder.HasMany(x => x.Sections)
            .WithOne(x => x.FormModule)
            .HasForeignKey(x => x.FormModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Fields)
            .WithOne(x => x.FormModule)
            .HasForeignKey(x => x.FormModuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}