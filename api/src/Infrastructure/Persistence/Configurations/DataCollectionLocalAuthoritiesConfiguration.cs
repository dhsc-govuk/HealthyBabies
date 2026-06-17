using Domain.DataCollections;
using Domain.Organisations;
using Domain.Users;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DataCollectionLocalAuthoritiesConfiguration : IEntityTypeConfiguration<DataCollectionLocalAuthority>
{
    public void Configure(EntityTypeBuilder<DataCollectionLocalAuthority> builder)
    {
        builder.ToTable("data_collection_local_authorities");

        builder.HasKey(x => new { x.DataCollectionId, x.LocalAuthorityId });

        builder.Property(x => x.DataCollectionId)
            .HasConversion(x => x.Value, x => new DataCollectionId(x));

        builder.Property(x => x.LocalAuthorityId)
            .HasConversion(x => x.Value, x => new OrganisationId(x));

        builder.Property(x => x.AssignedAt)
            .HasConversion(new DateTimeUtcConverter())
            .HasDefaultValueSql("timezone('utc', now())");

        builder.Property(x => x.EndDate)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);

        builder.Property(x => x.AssignedById)
            .HasConversion(
                x => x != null ? x.Value : (Guid?)null,
                x => x.HasValue ? new UserId(x.Value) : null);

        builder.HasOne(x => x.DataCollection)
            .WithMany(dc => dc.LocalAuthorities)
            .HasForeignKey(x => x.DataCollectionId)
            .HasConstraintName("fk_data_collection_local_authorities_data_collections")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.LocalAuthority)
            .WithMany()
            .HasForeignKey(x => x.LocalAuthorityId)
            .HasConstraintName("fk_data_collection_local_authorities_organisations")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.AssignedBy)
            .WithMany()
            .HasForeignKey(x => x.AssignedById)
            .HasConstraintName("fk_data_collection_local_authorities_users_assigned_by")
            .OnDelete(DeleteBehavior.SetNull);
    }
}