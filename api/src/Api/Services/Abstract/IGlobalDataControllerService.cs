using Application.Systems.Dtos;
using LanguageExt;

namespace Api.Services.Abstract;

public interface IGlobalDataControllerService
{
    Task<IEnumerable<GlobalDataDto>> GetAll(CancellationToken cancellationToken);
    Task<IEnumerable<GlobalDataDto>> GetByEntity(string entity, CancellationToken cancellationToken);
    Task<Option<GlobalDataDto>> GetById(Guid id, CancellationToken cancellationToken);
}