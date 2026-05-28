using Domain.Locations;
using Domain.Organisations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class LocationsConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new LocationId(x));
        builder.Property(x => x.OrganisationId).HasConversion(x => x.Value, x => new OrganisationId(x));

        builder.Property(x => x.Name).HasColumnType("varchar(255)").IsRequired();
        builder.Property(x => x.PostCode).HasColumnType("varchar(20)");
        builder.Property(x => x.ReferenceNumber).HasColumnType("varchar(255)");
        builder.HasIndex(x => x.ReferenceNumber);
        builder.Property(x => x.AddressLine1).HasColumnType("varchar(255)");
        builder.Property(x => x.AddressLine2).HasColumnType("varchar(255)");
        builder.Property(x => x.TownOrCity).HasColumnType("varchar(100)");
        builder.Property(x => x.County).HasColumnType("varchar(100)");
        builder.Property(x => x.IsActive);

        builder.HasOne<Organisation>(x => x.Organisation)
            .WithMany()
            .HasForeignKey(x => x.OrganisationId)
            .HasConstraintName("fk_locations_organisations_organisation_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(x => x.Answers).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.Answers)
            .WithOne(x => x.Location)
            .HasForeignKey(x => x.LocationId)
            .HasConstraintName("fk_site_answers_locations_location_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}