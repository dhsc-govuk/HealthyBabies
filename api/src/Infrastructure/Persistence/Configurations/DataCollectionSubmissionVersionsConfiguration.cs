using System.Text.Json;
using Domain.DataCollections;
using Domain.Users;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DataCollectionSubmissionVersionsConfiguration : IEntityTypeConfiguration<DataCollectionSubmissionVersion>
{
    public void Configure(EntityTypeBuilder<DataCollectionSubmissionVersion> builder)
    {
        builder.ToTable("data_collection_submission_versions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new DataCollectionSubmissionVersionId(x));

        builder.Property(x => x.DataCollectionSubmissionId)
            .HasConversion(x => x.Value, x => new DataCollectionSubmissionId(x))
            .IsRequired();

        builder.HasOne(x => x.DataCollectionSubmission)
            .WithMany()
            .HasForeignKey(x => x.DataCollectionSubmissionId)
            .HasConstraintName("fk_data_collection_submission_versions_data_collection_submissions")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.VersionNumber).IsRequired();

        builder.Property(x => x.ReviewNotes).HasColumnType("varchar(2000)");

        var sectionsToReviewComparer = new ValueComparer<IReadOnlyList<Guid>>(
            (a, b) => (a ?? new List<Guid>()).SequenceEqual(b ?? new List<Guid>()),
            v => v.Aggregate(0, (hash, x) => HashCode.Combine(hash, x)),
            v => v.ToList());
        builder.Property(x => x.SectionsToReview)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>())
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(sectionsToReviewComparer);

        builder.Property(x => x.ResubmitBy)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);

        builder.Property(x => x.ReviewedById)
            .HasConversion(x => x.Value, x => new UserId(x))
            .IsRequired();
        builder.HasOne(x => x.ReviewedBy)
            .WithMany()
            .HasForeignKey(x => x.ReviewedById)
            .HasConstraintName("fk_data_collection_submission_versions_users_reviewed_by")
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.ReviewedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired();

        builder.Property(x => x.SubmittedById)
            .HasConversion(
                x => x != null ? x.Value : (Guid?)null,
                x => x.HasValue ? new UserId(x.Value) : null);
        builder.HasOne(x => x.SubmittedBy)
            .WithMany()
            .HasForeignKey(x => x.SubmittedById)
            .HasConstraintName("fk_data_collection_submission_versions_users_submitted_by")
            .OnDelete(DeleteBehavior.SetNull);
        builder.Property(x => x.SubmittedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);

        builder.Property(x => x.Content).HasColumnType("jsonb").IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .HasDefaultValueSql("timezone('utc', now())");
        builder.Property(x => x.UpdatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);
        builder.Property(x => x.CreatedById)
            .HasConversion(x => x!.Value, x => new UserId(x));
        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .HasConstraintName("fk_data_collection_submission_versions_users_created_by_id")
            .OnDelete(DeleteBehavior.SetNull);
        builder.Property(x => x.LastModifiedById)
            .HasConversion(x => x!.Value, x => new UserId(x));
        builder.HasOne(x => x.LastModifiedBy)
            .WithMany()
            .HasForeignKey(x => x.LastModifiedById)
            .HasConstraintName("fk_data_collection_submission_versions_users_last_modified_by_id")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.DataCollectionSubmissionId, x.VersionNumber })
            .IsUnique()
            .HasDatabaseName("ux_data_collection_submission_versions_submission_version");
    }
}