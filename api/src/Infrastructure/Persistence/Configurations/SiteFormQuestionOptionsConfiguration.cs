using Domain.SiteForms;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SiteFormQuestionOptionsConfiguration : IEntityTypeConfiguration<SiteFormQuestionOption>
{
    public void Configure(EntityTypeBuilder<SiteFormQuestionOption> builder)
    {
        builder.ToTable("site_form_question_options");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new SiteFormQuestionOptionId(x));

        builder.Property(x => x.QuestionId).HasConversion(x => x.Value, x => new SiteFormQuestionId(x));

        builder.Property(x => x.Value).HasColumnType("varchar(255)").IsRequired();
        builder.Property(x => x.Label).HasColumnType("varchar(500)").IsRequired();
        builder.Property(x => x.DisplayOrder);

        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.CreatedById).HasConversion(
            x => x != null ? x.Value : (Guid?)null,
            x => x.HasValue ? new UserId(x.Value) : null);
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.LastModifiedById).HasConversion(
            x => x != null ? x.Value : (Guid?)null,
            x => x.HasValue ? new UserId(x.Value) : null);

        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .HasConstraintName("fk_site_form_question_options_users_created_by_id");

        builder.HasOne(x => x.LastModifiedBy)
            .WithMany()
            .HasForeignKey(x => x.LastModifiedById)
            .HasConstraintName("fk_site_form_question_options_users_last_modified_by_id");
    }
}