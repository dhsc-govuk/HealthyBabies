using Domain.Systems;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IGlobalDataRepository
{
    Task<Option<GlobalData>> GetByIdAsync(GlobalDataId id, CancellationToken cancellationToken = default);
    Task<GlobalData> AddAsync(GlobalData entity, CancellationToken cancellationToken = default);
    Task<GlobalData> UpdateAsync(GlobalData entity, CancellationToken cancellationToken = default);
    Task<GlobalData> RemoveAsync(GlobalData entity, CancellationToken cancellationToken = default);
}