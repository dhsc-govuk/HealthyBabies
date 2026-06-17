using Domain.DataCollections;
using Domain.Users;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DataCollectionsConfiguration : IEntityTypeConfiguration<DataCollection>
{
    public void Configure(EntityTypeBuilder<DataCollection> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new DataCollectionId(x));
        builder.Property(x => x.Name).HasColumnType("varchar(255)").IsRequired();
        builder.Property(x => x.Description).HasColumnType("varchar(1000)");
        builder.Property(x => x.StartDate)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired();
        builder.Property(x => x.EndDate)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired();
        builder.Property(x => x.IsDraft)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.IsSubmittedByAllLocalAuthorities)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.IsClosed)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);
        builder.Property(x => x.CreatedById).HasConversion(x => x!.Value, x => new UserId(x));
        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .HasConstraintName("fk_data_collections_users_created_by_id")
            .OnDelete(DeleteBehavior.SetNull);
        builder.Property(x => x.LastModifiedById).HasConversion(x => x!.Value, x => new UserId(x));
        builder.HasOne(x => x.LastModifiedBy)
            .WithMany()
            .HasForeignKey(x => x.LastModifiedById)
            .HasConstraintName("fk_data_collections_users_last_modified_by_id")
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);
        builder.Property(x => x.DeletedById).HasConversion(x => x!.Value, x => new UserId(x));
        builder.HasOne(x => x.DeletedBy)
            .WithMany()
            .HasForeignKey(x => x.DeletedById)
            .HasConstraintName("fk_data_collections_users_deleted_by_id")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasMany(x => x.FormModuleAssignments)
            .WithOne(x => x.DataCollection)
            .HasForeignKey(x => x.DataCollectionId);

        builder.Navigation(x => x.FormModuleAssignments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.LocalAuthorities)
            .WithOne(x => x.DataCollection)
            .HasForeignKey(x => x.DataCollectionId);

        builder.Navigation(x => x.LocalAuthorities)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}