using Domain.ServiceCategories;
using Domain.ServiceCategoryForms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ServiceCategoryAnswersConfiguration : IEntityTypeConfiguration<ServiceCategoryAnswer>
{
    public void Configure(EntityTypeBuilder<ServiceCategoryAnswer> builder)
    {
        builder.ToTable("service_category_answers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => new ServiceCategoryAnswerId(value))
            .HasColumnName("id");

        builder.Property(x => x.ServiceCategoryId)
            .HasConversion(
                id => id.Value,
                value => new ServiceCategoryId(value))
            .HasColumnName("service_category_id")
            .IsRequired();

        builder.Property(x => x.QuestionCode)
            .HasColumnName("question_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.QuestionLabel)
            .HasColumnName("question_label")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.QuestionHint)
            .HasColumnName("question_hint")
            .HasMaxLength(1000);

        builder.Property(x => x.QuestionType)
            .HasColumnName("question_type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Step)
            .HasColumnName("step")
            .IsRequired();

        builder.Property(x => x.DisplayOrder)
            .HasColumnName("display_order")
            .IsRequired();

        builder.Property(x => x.Value)
            .HasColumnName("value")
            .HasMaxLength(2000);

        builder.Property(x => x.DisplayValue)
            .HasColumnName("display_value")
            .HasMaxLength(2000);

        builder.Property(x => x.OptionsSnapshot)
            .HasColumnName("options_snapshot")
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedById).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.LastModifiedById).HasColumnName("last_modified_by");
    }
}