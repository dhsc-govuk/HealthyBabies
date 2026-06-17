using Domain.Systems;
using Domain.Users;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class GlobalDataConfiguration : IEntityTypeConfiguration<GlobalData>
{
    public void Configure(EntityTypeBuilder<GlobalData> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new GlobalDataId(x));
        builder.Property(x => x.Entity).HasColumnType("varchar(255)");
        builder.Property(x => x.Value).HasColumnType("varchar(255)");
        builder.Property(x => x.Description).HasColumnType("varchar(500)");

        builder.HasIndex(x => x.Entity);

        builder.Property(x => x.CreatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .HasDefaultValueSql("timezone('utc', now())");

        builder.Property(x => x.UpdatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);

        builder.Property(x => x.CreatedById).HasConversion(x => x!.Value, x => new UserId(x));
        builder.HasOne<User>(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .HasConstraintName("fk_organisations_users_created_by_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.LastModifiedById).HasConversion(x => x!.Value, x => new UserId(x)).IsRequired(false);
        builder.HasOne<User>(x => x.LastModifiedBy)
            .WithMany()
            .HasForeignKey(x => x.LastModifiedById)
            .HasConstraintName("fk_organisations_users_updated_by_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}