using Domain.ServiceCategoryForms;

namespace Application.Common.Interfaces.Queries;

public interface IServiceCategoryFormQuestionQueries
{
    Task<IReadOnlyList<ServiceCategoryFormQuestion>> GetAll(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceCategoryFormQuestion>> GetByStep(int step, CancellationToken cancellationToken = default);
}