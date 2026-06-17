using Domain.Organisations;
using Domain.Services;
using Domain.Users;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ServicesConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new ServiceId(x));
        builder.Property(x => x.OrganisationId).HasConversion(x => x.Value, x => new OrganisationId(x));
        builder.Property(x => x.Name).HasColumnType("varchar(255)").IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.CurrentStep);

        // Soft delete fields
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);
        builder.Property(x => x.DeletedById).HasConversion(
            x => x != null ? x.Value : (Guid?)null,
            x => x.HasValue ? new UserId(x.Value) : null);

        // Audit fields
        builder.Property(x => x.CreatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.CreatedById).HasConversion(
            x => x != null ? x.Value : (Guid?)null,
            x => x.HasValue ? new UserId(x.Value) : null);
        builder.Property(x => x.UpdatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);
        builder.Property(x => x.LastModifiedById).HasConversion(
            x => x != null ? x.Value : (Guid?)null,
            x => x.HasValue ? new UserId(x.Value) : null);

        // Relationships
        builder.HasOne(x => x.Organisation)
            .WithMany()
            .HasForeignKey(x => x.OrganisationId)
            .HasConstraintName("fk_services_organisations_organisation_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Answers)
            .WithOne(x => x.Service)
            .HasForeignKey(x => x.ServiceId)
            .HasConstraintName("fk_service_answers_services_service_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .HasConstraintName("fk_services_users_created_by_id");

        builder.HasOne(x => x.DeletedBy)
            .WithMany()
            .HasForeignKey(x => x.DeletedById)
            .HasConstraintName("fk_services_users_deleted_by_id");

        builder.HasOne(x => x.LastModifiedBy)
            .WithMany()
            .HasForeignKey(x => x.LastModifiedById)
            .HasConstraintName("fk_services_users_last_modified_by_id");

        // Global query filter for soft delete
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}