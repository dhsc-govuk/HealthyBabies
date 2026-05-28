using Domain.DataCollections;
using Domain.Organisations;
using Domain.Users;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class DataCollectionSubmissionsConfiguration : IEntityTypeConfiguration<DataCollectionSubmission>
{
    public void Configure(EntityTypeBuilder<DataCollectionSubmission> builder)
    {
        builder.ToTable("data_collection_submissions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new DataCollectionSubmissionId(x));

        builder.Property(x => x.DataCollectionId)
            .HasConversion(x => x.Value, x => new DataCollectionId(x))
            .IsRequired();

        builder.Property(x => x.OrganisationId)
            .HasConversion(x => x.Value, x => new OrganisationId(x))
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.SubmittedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);
        builder.Property(x => x.SubmittedById)
            .HasConversion(
                x => x != null ? x.Value : (Guid?)null,
                x => x.HasValue ? new UserId(x.Value) : null);
        builder.HasOne(x => x.SubmittedBy)
            .WithMany()
            .HasForeignKey(x => x.SubmittedById)
            .HasConstraintName("fk_data_collection_submissions_users_submitted_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(x => x.ReviewedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);
        builder.Property(x => x.ReviewedById)
            .HasConversion(
                x => x != null ? x.Value : (Guid?)null,
                x => x.HasValue ? new UserId(x.Value) : null);
        builder.HasOne(x => x.ReviewedBy)
            .WithMany()
            .HasForeignKey(x => x.ReviewedById)
            .HasConstraintName("fk_data_collection_submissions_users_reviewed_by")
            .OnDelete(DeleteBehavior.SetNull);
        builder.Property(x => x.ReviewNotes).HasColumnType("varchar(2000)");

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
            .HasConstraintName("fk_data_collection_submissions_users_created_by_id")
            .OnDelete(DeleteBehavior.SetNull);
        builder.Property(x => x.LastModifiedById)
            .HasConversion(x => x!.Value, x => new UserId(x));
        builder.HasOne(x => x.LastModifiedBy)
            .WithMany()
            .HasForeignKey(x => x.LastModifiedById)
            .HasConstraintName("fk_data_collection_submissions_users_last_modified_by_id")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.DataCollection)
            .WithMany()
            .HasForeignKey(x => x.DataCollectionId)
            .HasConstraintName("fk_data_collection_submissions_data_collections")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Organisation)
            .WithMany()
            .HasForeignKey(x => x.OrganisationId)
            .HasConstraintName("fk_data_collection_submissions_organisations")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.DataCollectionId, x.OrganisationId })
            .IsUnique()
            .HasDatabaseName("ix_data_collection_submissions_data_collection_organisation");
    }
}