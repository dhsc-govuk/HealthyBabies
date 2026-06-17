using Domain.SiteForms;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface ISiteFormQuestionRepository
{
    Task<Option<SiteFormQuestion>> GetById(SiteFormQuestionId id, CancellationToken cancellationToken = default);
    Task<Option<SiteFormQuestion>> FindByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<int> GetMaxDisplayOrder(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SiteFormQuestion>> GetAllTracking(CancellationToken cancellationToken = default);
    Task<SiteFormQuestion> AddAsync(SiteFormQuestion entity, CancellationToken cancellationToken = default);
    Task<SiteFormQuestion> UpdateAsync(SiteFormQuestion entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(SiteFormQuestion entity, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}