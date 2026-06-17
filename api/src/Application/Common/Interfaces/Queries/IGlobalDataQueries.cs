using Domain.Systems;
using LanguageExt;
using GlobalDataEntity = Domain.Systems.GlobalData;

namespace Application.Common.Interfaces.Queries;

public interface IGlobalDataQueries
{
    Task<IReadOnlyList<GlobalDataEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GlobalDataEntity>> GetByEntityAsync(string entity, CancellationToken cancellationToken = default);
    Task<Option<GlobalDataEntity>> GetByIdAsync(GlobalDataId id, CancellationToken cancellationToken = default);
}