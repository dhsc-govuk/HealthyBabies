using Application.Common.Interfaces.Repositories;
using Application.Systems.Exceptions;
using Domain.Systems;
using LanguageExt;
using MediatR;

namespace Application.Systems.Commands;

public record DeleteGlobalDataCommand : IRequest<Either<GlobalDataException, GlobalData>>
{
    public Guid Id { get; init; }
}

public class DeleteGlobalDataCommandHandler(IGlobalDataRepository repository)
    : IRequestHandler<DeleteGlobalDataCommand, Either<GlobalDataException, GlobalData>>
{
    public async Task<Either<GlobalDataException, GlobalData>> Handle(
        DeleteGlobalDataCommand request,
        CancellationToken cancellationToken)
    {
        var globalDataId = new GlobalDataId(request.Id);
        var globalData = await repository.GetByIdAsync(globalDataId, cancellationToken);

        return await globalData.MatchAsync(
            g => DeleteGlobalData(g, cancellationToken),
            () => new GlobalDataDoesNotExistException(globalDataId));
    }

    private async Task<Either<GlobalDataException, GlobalData>> DeleteGlobalData(
        GlobalData globalData,
        CancellationToken cancellationToken)
    {
        try
        {
            return await repository.RemoveAsync(globalData, cancellationToken);
        }
        catch (Exception exception)
        {
            return new GlobalDataUnknownException(GlobalDataId.Empty(), exception);
        }
    }
}