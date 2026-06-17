using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using Domain.Organisations;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class FormSubmissionRepository(ApplicationDbContext dbContext)
    : RepositoryBase<FormSubmission, FormSubmissionId>(dbContext), IFormSubmissionRepository, IFormSubmissionQueries
{
    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return Context.FormSubmissions.CountAsync(cancellationToken);
    }

    public async Task<Option<FormSubmission>> GetByIdAsync(FormSubmissionId id, CancellationToken cancellationToken = default)
    {
        var entity = await Context.FormSubmissions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<FormSubmission>.None;
    }

    public async Task<Option<FormSubmission>> GetByIdWithValuesAsync(FormSubmissionId id, CancellationToken cancellationToken = default)
    {
        var entity = await Context.FormSubmissions
            .Include(x => x.FieldValues)
            .Include(x => x.History)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<FormSubmission>.None;
    }

    public async Task<IReadOnlyList<FormSubmission>> GetByFormModuleIdAsync(DataCollectionFormModuleId formModuleId, CancellationToken cancellationToken = default)
    {
        return await Context.FormSubmissions
            .AsNoTracking()
            .Where(x => x.FormModuleId == formModuleId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FormSubmission>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        return await Context.FormSubmissions
            .AsTracking()
            .Include(x => x.FieldValues)
            .Where(x => x.EntityType == entityType && x.EntityId == entityId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FormSubmission>> GetByStatusAsync(SubmissionStatus status, CancellationToken cancellationToken = default)
    {
        return await Context.FormSubmissions
            .AsTracking()
            .Include(x => x.FieldValues)
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FormSubmission>> GetByFormModuleAndEntityTypeAsync(
        DataCollectionFormModuleId formModuleId,
        string entityType,
        CancellationToken cancellationToken = default)
    {
        return await Context.FormSubmissions
            .AsNoTracking()
            .Include(x => x.FieldValues)
            .Where(x => x.FormModuleId == formModuleId && x.EntityType == entityType)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Option<FormSubmission>> GetByFormModuleAndServiceAsync(
        DataCollectionFormModuleId formModuleId,
        Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.FormSubmissions
            .AsTracking()
            .Include(x => x.FieldValues)
            .FirstOrDefaultAsync(x => x.FormModuleId == formModuleId && x.EntityType == "Service" && x.EntityId == serviceId, cancellationToken);
        return entity ?? Option<FormSubmission>.None;
    }

    public async Task<Option<FormSubmission>> GetByFormModuleAndServiceCategoryAsync(
        DataCollectionFormModuleId formModuleId,
        Guid serviceCategoryId,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.FormSubmissions
            .AsTracking()
            .Include(x => x.FieldValues)
            .FirstOrDefaultAsync(x => x.FormModuleId == formModuleId && x.EntityType == "WiderServiceCategory" && x.EntityId == serviceCategoryId, cancellationToken);
        return entity ?? Option<FormSubmission>.None;
    }

    public async Task<Option<FormSubmission>> GetByFormModuleDataCollectionAndServiceAsync(
        DataCollectionFormModuleId formModuleId,
        DataCollectionId dataCollectionId,
        Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.FormSubmissions
            .AsTracking()
            .Include(x => x.FieldValues)
            .FirstOrDefaultAsync(x => x.FormModuleId == formModuleId && x.DataCollectionId == dataCollectionId && x.EntityType == "Service" && x.EntityId == serviceId, cancellationToken);
        return entity ?? Option<FormSubmission>.None;
    }

    public async Task<Option<FormSubmission>> GetByFormModuleDataCollectionAndServiceCategoryAsync(
        DataCollectionFormModuleId formModuleId,
        DataCollectionId dataCollectionId,
        Guid serviceCategoryId,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.FormSubmissions
            .AsTracking()
            .Include(x => x.FieldValues)
            .FirstOrDefaultAsync(x => x.FormModuleId == formModuleId && x.DataCollectionId == dataCollectionId && x.EntityType == "WiderServiceCategory" && x.EntityId == serviceCategoryId, cancellationToken);
        return entity ?? Option<FormSubmission>.None;
    }

    public async Task<IReadOnlyList<FormSubmission>> GetByFormModuleAndDataCollectionAsync(
        DataCollectionFormModuleId formModuleId,
        DataCollectionId dataCollectionId,
        CancellationToken cancellationToken = default)
    {
        return await Context.FormSubmissions
            .AsTracking()
            .Include(x => x.FieldValues)
            .Where(x => x.FormModuleId == formModuleId && x.EntityType == "DataCollection" && x.EntityId == dataCollectionId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FormSubmission>> GetByFormModuleOrganisationAndDataCollectionAsync(
        DataCollectionFormModuleId formModuleId,
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        CancellationToken cancellationToken = default)
    {
        return await Context.FormSubmissions
            .AsTracking()
            .Include(x => x.FieldValues)
            .Where(x => x.FormModuleId == formModuleId && x.OrganisationId == organisationId && x.DataCollectionId == dataCollectionId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FormSubmission>> GetByFormModuleOrganisationAndEntityTypeAsync(
        DataCollectionFormModuleId formModuleId,
        OrganisationId organisationId,
        string entityType,
        CancellationToken cancellationToken = default)
    {
        return await Context.FormSubmissions
            .AsNoTracking()
            .Include(x => x.FieldValues)
            .Where(x => x.FormModuleId == formModuleId && x.OrganisationId == organisationId && x.EntityType == entityType)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FormSubmission>> GetByFormModuleOrganisationDataCollectionAndEntityTypeAsync(
        DataCollectionFormModuleId formModuleId,
        OrganisationId organisationId,
        DataCollectionId dataCollectionId,
        string entityType,
        CancellationToken cancellationToken = default)
    {
        return await Context.FormSubmissions
            .AsNoTracking()
            .Include(x => x.FieldValues)
            .Where(x => x.FormModuleId == formModuleId && x.OrganisationId == organisationId && x.DataCollectionId == dataCollectionId && x.EntityType == entityType)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FormSubmission>> GetDraftsAsync(CancellationToken cancellationToken = default)
    {
        return await Context.FormSubmissions
            .AsNoTracking()
            .Where(x => x.Status == SubmissionStatus.Draft)
            .OrderByDescending(x => x.DraftSavedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FormSubmission>> GetPendingReviewAsync(CancellationToken cancellationToken = default)
    {
        return await Context.FormSubmissions
            .AsNoTracking()
            .Where(x => x.Status == SubmissionStatus.Submitted || x.Status == SubmissionStatus.UnderReview)
            .OrderBy(x => x.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FormSubmission>> GetByOrganisationIdAsync(
        OrganisationId organisationId,
        CancellationToken cancellationToken = default)
    {
        return await Context.FormSubmissions
            .AsTracking()
            .Include(x => x.FieldValues)
            .Where(x => x.OrganisationId == organisationId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FormSubmission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Context.FormSubmissions
            .AsTracking()
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        var count = await Context.FormSubmissions.IgnoreQueryFilters().CountAsync(cancellationToken);
        await Context.FormSubmissions.IgnoreQueryFilters().ExecuteDeleteAsync(cancellationToken);
        return count;
    }

    public async Task DeleteAsync(FormSubmissionId id, CancellationToken cancellationToken = default)
    {
        var entity = await Context.FormSubmissions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity != null)
        {
            Context.FormSubmissions.Remove(entity);
            await Context.SaveChangesAsync(cancellationToken);
        }
    }
}