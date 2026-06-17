using Domain.ServiceForms;
using Domain.Services;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ServiceAnswersConfiguration : IEntityTypeConfiguration<ServiceAnswer>
{
    public void Configure(EntityTypeBuilder<ServiceAnswer> builder)
    {
        builder.ToTable("service_answers");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new ServiceAnswerId(x));

        builder.Property(x => x.ServiceId).HasConversion(x => x.Value, x => new ServiceId(x));

        builder.Property(x => x.QuestionCode).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.QuestionLabel).HasColumnType("varchar(500)").IsRequired();
        builder.Property(x => x.QuestionHint).HasColumnType("varchar(1000)");
        builder.Property(x => x.QuestionType).HasConversion<int>();
        builder.Property(x => x.Step);
        builder.Property(x => x.DisplayOrder);
        builder.Property(x => x.Value).HasColumnType("text");
        builder.Property(x => x.DisplayValue).HasColumnType("text");
        builder.Property(x => x.OptionsSnapshot).HasColumnType("text");

        // Index for efficient querying
        builder.HasIndex(x => new { x.ServiceId, x.QuestionCode }).IsUnique();

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
        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .HasConstraintName("fk_service_answers_users_created_by_id");

        builder.HasOne(x => x.LastModifiedBy)
            .WithMany()
            .HasForeignKey(x => x.LastModifiedById)
            .HasConstraintName("fk_service_answers_users_last_modified_by_id");
    }
}