using Domain.Locations;
using Domain.SiteForms;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SiteAnswersConfiguration : IEntityTypeConfiguration<SiteAnswer>
{
    public void Configure(EntityTypeBuilder<SiteAnswer> builder)
    {
        builder.ToTable("site_answers");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new SiteAnswerId(x));

        builder.Property(x => x.LocationId).HasConversion(x => x.Value, x => new LocationId(x));

        builder.Property(x => x.QuestionCode).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.QuestionLabel).HasColumnType("varchar(500)").IsRequired();
        builder.Property(x => x.QuestionHint).HasColumnType("varchar(1000)");
        builder.Property(x => x.QuestionType).HasConversion<int>();
        builder.Property(x => x.DisplayOrder);
        builder.Property(x => x.Value).HasColumnType("text");
        builder.Property(x => x.DisplayValue).HasColumnType("text");
        builder.Property(x => x.OptionsSnapshot).HasColumnType("text");

        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.CreatedById).HasConversion(
            x => x != null ? x.Value : (Guid?)null,
            x => x.HasValue ? new UserId(x.Value) : null);
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.LastModifiedById).HasConversion(
            x => x != null ? x.Value : (Guid?)null,
            x => x.HasValue ? new UserId(x.Value) : null);

        builder.HasOne(x => x.Location)
            .WithMany(x => x.Answers)
            .HasForeignKey(x => x.LocationId)
            .HasConstraintName("fk_site_answers_locations_location_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .HasConstraintName("fk_site_answers_users_created_by_id");

        builder.HasOne(x => x.LastModifiedBy)
            .WithMany()
            .HasForeignKey(x => x.LastModifiedById)
            .HasConstraintName("fk_site_answers_users_last_modified_by_id");

        builder.HasIndex(x => new { x.LocationId, x.QuestionCode })
            .HasDatabaseName("ix_site_answers_location_id_question_code");
    }
}