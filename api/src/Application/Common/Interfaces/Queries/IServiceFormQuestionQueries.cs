using Domain.ServiceForms;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IServiceFormQuestionQueries
{
    Task<IReadOnlyList<ServiceFormQuestion>> GetAll(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceFormQuestion>> GetAllActive(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceFormQuestion>> GetByStep(int step, CancellationToken cancellationToken = default);
    Task<Option<ServiceFormQuestion>> GetById(ServiceFormQuestionId id, CancellationToken cancellationToken = default);
    Task<Option<ServiceFormQuestion>> GetByCode(string code, CancellationToken cancellationToken = default);
}