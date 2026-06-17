using Domain.Organisations;
using Domain.OrganisationUsers;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class OrganisationUsersConfiguration : IEntityTypeConfiguration<OrganisationUser>
{
    public void Configure(EntityTypeBuilder<OrganisationUser> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new OrganisationUserId(x));
        builder.Property(x => x.UserId).HasConversion(x => x.Value, x => new UserId(x));
        builder.Property(x => x.OrganisationId).HasConversion(x => x.Value, x => new OrganisationId(x));
        builder.HasOne<User>(x => x.User)
            .WithOne()
            .HasForeignKey<OrganisationUser>(x => x.UserId)
            .HasConstraintName("fk_organisation_users_users_user_id")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Organisation>(x => x.Organisation)
            .WithMany()
            .HasForeignKey(x => x.OrganisationId)
            .HasConstraintName("fk_organisation_users_organisations_organisation_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}