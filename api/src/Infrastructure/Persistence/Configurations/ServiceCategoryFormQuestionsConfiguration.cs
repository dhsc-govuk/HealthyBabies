using Domain.ServiceCategoryForms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ServiceCategoryFormQuestionsConfiguration : IEntityTypeConfiguration<ServiceCategoryFormQuestion>
{
    public void Configure(EntityTypeBuilder<ServiceCategoryFormQuestion> builder)
    {
        builder.ToTable("service_category_form_questions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => new ServiceCategoryFormQuestionId(value))
            .HasColumnName("id");

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Label)
            .HasColumnName("label")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Hint)
            .HasColumnName("hint")
            .HasMaxLength(1000);

        builder.Property(x => x.Placeholder)
            .HasColumnName("placeholder")
            .HasMaxLength(200);

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

        builder.Property(x => x.IsRequired)
            .HasColumnName("is_required")
            .IsRequired();

        builder.Property(x => x.IsPredefined)
            .HasColumnName("is_predefined")
            .IsRequired();

        builder.Property(x => x.HelpTextSummary)
            .HasColumnName("help_text_summary")
            .HasMaxLength(500);

        builder.Property(x => x.HelpText)
            .HasColumnName("help_text")
            .HasMaxLength(4000);

        builder.Property(x => x.ConditionalQuestionCode)
            .HasColumnName("conditional_question_code")
            .HasMaxLength(50);

        builder.Property(x => x.ConditionalValue)
            .HasColumnName("conditional_value")
            .HasMaxLength(200);

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.HasMany(x => x.Options)
            .WithOne(x => x.Question)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedById).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.LastModifiedById).HasColumnName("last_modified_by");
    }
}