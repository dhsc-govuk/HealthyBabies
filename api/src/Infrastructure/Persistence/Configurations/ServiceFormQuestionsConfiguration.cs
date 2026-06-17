using Domain.ServiceForms;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ServiceFormQuestionsConfiguration : IEntityTypeConfiguration<ServiceFormQuestion>
{
    public void Configure(EntityTypeBuilder<ServiceFormQuestion> builder)
    {
        builder.ToTable("service_form_questions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new ServiceFormQuestionId(x));

        builder.Property(x => x.Code).HasColumnType("varchar(50)").IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.Label).HasColumnType("varchar(500)").IsRequired();
        builder.Property(x => x.Hint).HasColumnType("varchar(1000)");
        builder.Property(x => x.Placeholder).HasColumnType("varchar(255)");
        builder.Property(x => x.QuestionType).HasConversion<int>();
        builder.Property(x => x.Step);
        builder.Property(x => x.DisplayOrder);
        builder.Property(x => x.IsRequired);
        builder.Property(x => x.IsPredefined);
        builder.Property(x => x.HelpTextSummary).HasColumnType("varchar(500)");
        builder.Property(x => x.HelpText).HasColumnType("text");
        builder.Property(x => x.ConditionalQuestionCode).HasColumnType("varchar(50)");
        builder.Property(x => x.ConditionalValue).HasColumnType("varchar(255)");
        builder.Property(x => x.IsActive);

        // Audit fields
        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.CreatedById).HasConversion(
            x => x != null ? x.Value : (Guid?)null,
            x => x.HasValue ? new UserId(x.Value) : null);
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.LastModifiedById).HasConversion(
            x => x != null ? x.Value : (Guid?)null,
            x => x.HasValue ? new UserId(x.Value) : null);

        // Relationships
        builder.HasMany(x => x.Options)
            .WithOne(x => x.Question)
            .HasForeignKey(x => x.QuestionId)
            .HasConstraintName("fk_service_form_question_options_questions_question_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .HasConstraintName("fk_service_form_questions_users_created_by_id");

        builder.HasOne(x => x.LastModifiedBy)
            .WithMany()
            .HasForeignKey(x => x.LastModifiedById)
            .HasConstraintName("fk_service_form_questions_users_last_modified_by_id");
    }
}