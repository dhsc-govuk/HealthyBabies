using Domain.Organisations;
using Domain.Users;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class OrganisationContactsConfiguration : IEntityTypeConfiguration<OrganisationContact>
{
    public void Configure(EntityTypeBuilder<OrganisationContact> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new OrganisationContactId(x));
        builder.Property(x => x.OrganisationId).HasConversion(x => x.Value, x => new OrganisationId(x));
        builder.Property(x => x.Name).HasColumnType("varchar(255)");
        builder.Property(x => x.Email).HasColumnType("varchar(255)");
        builder.Property(x => x.Role).HasColumnType("varchar(100)");

        builder.Property(x => x.CreatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .HasDefaultValueSql("timezone('utc', now())");

        builder.Property(x => x.UpdatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);

        builder.Property(x => x.DeletedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);

        builder.Property(x => x.DeletedById).HasConversion(x => x!.Value, x => new UserId(x));
        builder.HasOne<User>(x => x.DeletedBy)
            .WithMany()
            .HasForeignKey(x => x.DeletedById)
            .HasConstraintName("fk_organisations_users_deleted_by_id")
            .OnDelete(DeleteBehavior.Restrict);

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

        builder.HasOne<Organisation>(x => x.Organisation)
            .WithMany()
            .HasForeignKey(x => x.OrganisationId)
            .HasConstraintName("fk_organisation_contacts_organisations_organisation_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>(x => x.DeletedBy)
            .WithMany()
            .HasForeignKey(x => x.DeletedById)
            .HasConstraintName("fk_organisation_contacts_users_deleted_by_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}