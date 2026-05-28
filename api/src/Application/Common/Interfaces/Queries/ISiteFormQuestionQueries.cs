using Domain.SiteForms;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface ISiteFormQuestionQueries
{
    Task<IReadOnlyList<SiteFormQuestion>> GetAll(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SiteFormQuestion>> GetAllActive(CancellationToken cancellationToken = default);
    Task<Option<SiteFormQuestion>> GetById(SiteFormQuestionId id, CancellationToken cancellationToken = default);
    Task<Option<SiteFormQuestion>> GetByCode(string code, CancellationToken cancellationToken = default);
}