using Domain.DataCollections;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DataCollectionFormModuleAssignmentConfiguration : IEntityTypeConfiguration<DataCollectionFormModuleAssignment>
{
    public void Configure(EntityTypeBuilder<DataCollectionFormModuleAssignment> builder)
    {
        builder.ToTable("data_collection_form_module_assignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.DataCollectionId)
            .HasColumnName("data_collection_id")
            .HasConversion(
                x => x.Value,
                x => new DataCollectionId(x))
            .IsRequired();

        builder.Property(x => x.FormModuleId)
            .HasColumnName("form_module_id")
            .HasConversion(
                x => x.Value,
                x => new DataCollectionFormModuleId(x))
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);

        builder.Property(x => x.CreatedById)
            .HasColumnName("created_by_id")
            .HasConversion(
                x => x != null ? x.Value : (Guid?)null,
                x => x.HasValue ? new Domain.Users.UserId(x.Value) : null);

        builder.Property(x => x.LastModifiedById)
            .HasColumnName("last_modified_by_id")
            .HasConversion(
                x => x != null ? x.Value : (Guid?)null,
                x => x.HasValue ? new Domain.Users.UserId(x.Value) : null);

        builder.HasOne(x => x.DataCollection)
            .WithMany(x => x.FormModuleAssignments)
            .HasForeignKey(x => x.DataCollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.FormModule)
            .WithMany()
            .HasForeignKey(x => x.FormModuleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LastModifiedBy)
            .WithMany()
            .HasForeignKey(x => x.LastModifiedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DataCollectionId, x.FormModuleId })
            .IsUnique();
    }
}