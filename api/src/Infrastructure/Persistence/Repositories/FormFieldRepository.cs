using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class FormFieldRepository(ApplicationDbContext dbContext)
    : RepositoryBase<FormField, FormFieldId>(dbContext), IFormFieldRepository, IFormFieldQueries
{
    public async Task<IReadOnlyList<FormField>> GetAll(CancellationToken cancellationToken = default)
    {
        return await Context.FormFields
            .AsNoTracking()
            .Include(x => x.Options.OrderBy(o => o.DisplayOrder))
            .Include(x => x.FormSection)
            .OrderBy(x => x.FormModuleId)
            .ThenBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FormField>> GetAllActive(CancellationToken cancellationToken = default)
    {
        return await Context.FormFields
            .AsNoTracking()
            .Include(x => x.Options.OrderBy(o => o.DisplayOrder))
            .Include(x => x.FormSection)
            .Where(x => x.IsActive)
            .OrderBy(x => x.FormModuleId)
            .ThenBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FormField>> GetByModuleId(
        DataCollectionFormModuleId moduleId,
        CancellationToken cancellationToken = default)
    {
        return await Context.FormFields
            .AsNoTracking()
            .Include(x => x.Options.OrderBy(o => o.DisplayOrder))
            .Include(x => x.FormSection)
            .Where(x => x.FormModuleId == moduleId && x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FormField>> GetByModuleCode(
        string moduleCode,
        CancellationToken cancellationToken = default)
    {
        return await Context.FormFields
            .AsNoTracking()
            .Include(x => x.Options.OrderBy(o => o.DisplayOrder))
            .Include(x => x.FormSection)
            .Include(x => x.FormModule)
            .Where(x => x.FormModule != null && x.FormModule.Code == moduleCode && x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<Option<FormField>> GetById(FormFieldId id, CancellationToken cancellationToken = default)
    {
        var entity = await Context.FormFields
            .Include(x => x.Options.OrderBy(o => o.DisplayOrder))
            .Include(x => x.FormSection)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity ?? Option<FormField>.None;
    }

    public async Task<Option<FormField>> GetByFieldKey(
        DataCollectionFormModuleId moduleId,
        string fieldKey,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.FormFields
            .Include(x => x.Options.OrderBy(o => o.DisplayOrder))
            .Include(x => x.FormSection)
            .FirstOrDefaultAsync(x => x.FormModuleId == moduleId && x.FieldKey == fieldKey, cancellationToken);
        return entity ?? Option<FormField>.None;
    }

    public async Task<Option<FormField>> FindByFieldKeyAsync(
        DataCollectionFormModuleId moduleId,
        string fieldKey,
        CancellationToken cancellationToken = default)
    {
        var entity = await Context.FormFields
            .FirstOrDefaultAsync(x => x.FormModuleId == moduleId && x.FieldKey == fieldKey, cancellationToken);
        return entity ?? Option<FormField>.None;
    }

    public async Task<int> GetMaxDisplayOrderForModule(
        DataCollectionFormModuleId moduleId,
        CancellationToken cancellationToken = default)
    {
        var maxOrder = await Context.FormFields
            .Where(x => x.FormModuleId == moduleId)
            .MaxAsync(x => (int?)x.DisplayOrder, cancellationToken);
        return maxOrder ?? 0;
    }

    public async Task<IReadOnlyList<FormField>> GetByModuleIdTracking(
        DataCollectionFormModuleId moduleId,
        CancellationToken cancellationToken = default)
    {
        return await Context.FormFields
            .Where(x => x.FormModuleId == moduleId)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(FormField entity, CancellationToken cancellationToken = default)
    {
        Context.FormFields.Remove(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasRelatedValuesAsync(FormFieldId fieldId, CancellationToken cancellationToken = default)
    {
        return await Context.FormFieldValues
            .AnyAsync(x => x.FormFieldId == fieldId, cancellationToken);
    }

    public async Task ClearOptionsAsync(FormFieldId fieldId, CancellationToken cancellationToken = default)
    {
        var options = await Context.FormFieldOptions
            .Where(x => x.FormFieldId == fieldId)
            .ToListAsync(cancellationToken);

        Context.FormFieldOptions.RemoveRange(options);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public new async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await Context.SaveChangesAsync(cancellationToken);
    }
}