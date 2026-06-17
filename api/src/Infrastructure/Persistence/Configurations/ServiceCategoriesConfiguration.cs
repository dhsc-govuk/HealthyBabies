using Domain.Organisations;
using Domain.ServiceCategories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ServiceCategoriesConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCategory> builder)
    {
        builder.ToTable("service_categories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => new ServiceCategoryId(value))
            .HasColumnName("id");

        builder.Property(x => x.OrganisationId)
            .HasConversion(
                id => id.Value,
                value => new OrganisationId(value))
            .HasColumnName("organisation_id")
            .IsRequired();

        builder.Property(x => x.CategoryCode)
            .HasColumnName("category_code")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CategoryName)
            .HasColumnName("category_name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CurrentStep)
            .HasColumnName("current_step")
            .IsRequired();

        builder.HasOne(x => x.Organisation)
            .WithMany()
            .HasForeignKey(x => x.OrganisationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Answers)
            .WithOne(x => x.ServiceCategory)
            .HasForeignKey(x => x.ServiceCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.OrganisationId, x.CategoryCode })
            .IsUnique();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedById).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("last_modified_at");
        builder.Property(x => x.LastModifiedById).HasColumnName("last_modified_by");
    }
}