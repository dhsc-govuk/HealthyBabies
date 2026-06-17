using Domain.DataCollections.Forms;
using Domain.Users;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations.Forms;

public class DataSourceItemConfiguration : IEntityTypeConfiguration<DataSourceItem>
{
    public void Configure(EntityTypeBuilder<DataSourceItem> builder)
    {
        builder.ToTable("data_source_items");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(x => x.Value, x => new DataSourceItemId(x));

        builder.Property(x => x.DataSourceId)
            .HasColumnName("data_source_id")
            .HasConversion(x => x.Value, x => new DataSourceId(x))
            .IsRequired();

        builder.Property(x => x.ParentItemId)
            .HasColumnName("parent_item_id")
            .HasConversion(x => x!.Value, x => new DataSourceItemId(x))
            .IsRequired(false);

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

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(x => x.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
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
            .HasConstraintName("fk_data_source_items_users_created_by_id")
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
            .HasConstraintName("fk_data_source_items_users_last_modified_by_id")
            .OnDelete(DeleteBehavior.Restrict);

        // Relationships
        builder.HasOne(x => x.ParentItem)
            .WithMany()
            .HasForeignKey(x => x.ParentItemId)
            .HasConstraintName("fk_data_source_items_data_source_items_parent_item_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DataSourceId, x.Value }).IsUnique();
        builder.HasIndex(x => new { x.DataSourceId, x.ParentItemId, x.DisplayOrder });
    }
}