using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Systems.Dtos;
using Domain.Systems;
using LanguageExt;

namespace Api.Services.Implementation;

public class GlobalDataControllerService(IGlobalDataQueries globalDataQueries) : IGlobalDataControllerService
{
    public async Task<IEnumerable<GlobalDataDto>> GetAll(CancellationToken cancellationToken)
    {
        var globalData = await globalDataQueries.GetAllAsync(cancellationToken);
        return globalData.Select(x => new GlobalDataDto(
            x.Id.Value,
            x.Entity,
            x.Value,
            x.Description));
    }

    public async Task<IEnumerable<GlobalDataDto>> GetByEntity(string entity, CancellationToken cancellationToken)
    {
        var globalData = await globalDataQueries.GetByEntityAsync(entity, cancellationToken);
        return globalData.Select(x => new GlobalDataDto(
            x.Id.Value,
            x.Entity,
            x.Value,
            x.Description));
    }

    public async Task<Option<GlobalDataDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var globalData = await globalDataQueries.GetByIdAsync(new GlobalDataId(id), cancellationToken);
        return globalData.Match(
            g => new GlobalDataDto(
                g.Id.Value,
                g.Entity,
                g.Value,
                g.Description),
            () => Option<GlobalDataDto>.None);
    }
}