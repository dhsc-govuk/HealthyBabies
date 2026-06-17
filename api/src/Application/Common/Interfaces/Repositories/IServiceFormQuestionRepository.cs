using Domain.ServiceForms;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IServiceFormQuestionRepository
{
    Task<Option<ServiceFormQuestion>> GetById(ServiceFormQuestionId id, CancellationToken cancellationToken = default);
    Task<Option<ServiceFormQuestion>> FindByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<int> GetMaxDisplayOrderForStep(int step, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceFormQuestion>> GetByStepTracking(int step, CancellationToken cancellationToken = default);
    Task<ServiceFormQuestion> AddAsync(ServiceFormQuestion entity, CancellationToken cancellationToken = default);
    Task<ServiceFormQuestion> UpdateAsync(ServiceFormQuestion entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(ServiceFormQuestion entity, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}