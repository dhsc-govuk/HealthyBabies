using Domain.Organisations;
using Domain.Users;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class OrganisationFinancialDataConfiguration : IEntityTypeConfiguration<OrganisationFinancialData>
{
    public void Configure(EntityTypeBuilder<OrganisationFinancialData> builder)
    {
        builder.ToTable("organisation_financial_data");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new OrganisationFinancialDataId(x));

        builder.Property(x => x.OrganisationId)
            .HasConversion(x => x.Value, x => new OrganisationId(x))
            .IsRequired();

        builder.Property(x => x.Module)
            .HasConversion(x => x.Value, x => FinancialDataModule.From(x))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.YearRange)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasIndex(x => new { x.OrganisationId, x.Module, x.YearRange })
            .IsUnique()
            .HasDatabaseName("ix_organisation_financial_data_org_module_year");

        builder.Property(x => x.CreatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .HasDefaultValueSql("timezone('utc', now())");

        builder.Property(x => x.UpdatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);

        builder.Property(x => x.CreatedById)
            .HasConversion(x => x!.Value, x => new UserId(x))
            .IsRequired(false);

        builder.HasOne<User>(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .HasConstraintName("fk_organisation_financial_data_users_created_by_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.LastModifiedById)
            .HasConversion(x => x!.Value, x => new UserId(x))
            .IsRequired(false);

        builder.HasOne<User>(x => x.LastModifiedBy)
            .WithMany()
            .HasForeignKey(x => x.LastModifiedById)
            .HasConstraintName("fk_organisation_financial_data_users_last_modified_by_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Organisation>(x => x.Organisation)
            .WithMany()
            .HasForeignKey(x => x.OrganisationId)
            .HasConstraintName("fk_organisation_financial_data_organisations_organisation_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}