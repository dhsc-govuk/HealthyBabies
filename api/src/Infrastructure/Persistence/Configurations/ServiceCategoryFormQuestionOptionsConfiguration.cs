using Domain.ServiceCategoryForms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ServiceCategoryFormQuestionOptionsConfiguration : IEntityTypeConfiguration<ServiceCategoryFormQuestionOption>
{
    public void Configure(EntityTypeBuilder<ServiceCategoryFormQuestionOption> builder)
    {
        builder.ToTable("service_category_form_question_options");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => new ServiceCategoryFormQuestionOptionId(value))
            .HasColumnName("id");

        builder.Property(x => x.QuestionId)
            .HasConversion(
                id => id.Value,
                value => new ServiceCategoryFormQuestionId(value))
            .HasColumnName("question_id")
            .IsRequired();

        builder.Property(x => x.Value)
            .HasColumnName("value")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Label)
            .HasColumnName("label")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.DisplayOrder)
            .HasColumnName("display_order")
            .IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedById).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.LastModifiedById).HasColumnName("last_modified_by");
    }
}