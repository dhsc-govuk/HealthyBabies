using Domain.Users;
using Domain.ValueObjects;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class UsersConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new UserId(x));
        builder.Property(x => x.Name).HasColumnType("jsonb");
        builder.Property(x => x.Email).HasColumnType("varchar(320)");
        builder.Property(x => x.SubId).HasConversion(x => x.Value, x => new SubId(x));
        builder.Property(x => x.IsActive);
        builder.Property(x => x.Role)
            .HasColumnType("varchar(255)")
            .HasConversion(x => x.ToString(), x => UserRole.From(x));

        // Audit fields configuration
        builder.Property(x => x.CreatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .HasDefaultValueSql("timezone('utc', now())")
            .IsRequired(false);
        builder.Property(x => x.CreatedById).HasConversion(x => x!.Value, x => new UserId(x));
        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .HasConstraintName("fk_users_users_created_by_id")
            .OnDelete(DeleteBehavior.SetNull);
        builder.Property(x => x.LastModifiedById).HasConversion(x => x!.Value, x => new UserId(x));
        builder.HasOne(x => x.LastModifiedBy)
            .WithMany()
            .HasForeignKey(x => x.LastModifiedById)
            .HasConstraintName("fk_users_users_last_modified_by_id")
            .OnDelete(DeleteBehavior.SetNull);

        // Soft delete configuration
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAt)
                .HasConversion(new DateTimeUtcConverter())
                .HasDefaultValueSql("timezone('utc', now())")
                .IsRequired(false);
        builder.Property(x => x.DeletedById).HasConversion(x => x!.Value, x => new UserId(x));
        builder.HasOne(x => x.DeletedBy)
            .WithMany()
            .HasForeignKey(x => x.DeletedById)
            .HasConstraintName("fk_users_users_deleted_by_id")
            .OnDelete(DeleteBehavior.SetNull);

        // Global query filter to exclude soft-deleted users by default
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}