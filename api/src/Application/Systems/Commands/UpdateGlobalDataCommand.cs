using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Systems.Exceptions;
using Domain.Systems;
using LanguageExt;
using MediatR;

namespace Application.Systems.Commands;

public record UpdateGlobalDataCommand : IRequest<Either<GlobalDataException, Domain.Systems.GlobalData>>
{
    public Guid Id { get; init; }
    public string Entity { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public class UpdateGlobalDataCommandHandler(
    IGlobalDataRepository repository,
    IGlobalDataQueries globalDataQueries)
    : IRequestHandler<UpdateGlobalDataCommand, Either<GlobalDataException, Domain.Systems.GlobalData>>
{
    public async Task<Either<GlobalDataException, Domain.Systems.GlobalData>> Handle(
        UpdateGlobalDataCommand request,
        CancellationToken cancellationToken)
    {
        var globalDataId = new GlobalDataId(request.Id);
        var globalData = await repository.GetByIdAsync(globalDataId, cancellationToken);

        return await globalData.MatchAsync(
            g => ValidateDuplicateAsync(request, g, cancellationToken)
                .BindAsync(validatedData => UpdateGlobalData(request, validatedData, cancellationToken)),
            () => new GlobalDataDoesNotExistException(globalDataId));
    }

    private async Task<Either<GlobalDataException, Domain.Systems.GlobalData>> ValidateDuplicateAsync(
        UpdateGlobalDataCommand request,
        Domain.Systems.GlobalData globalData,
        CancellationToken cancellationToken)
    {
        var existingValues = await globalDataQueries.GetByEntityAsync(globalData.Entity, cancellationToken);
        var isDuplicate = existingValues.Any(v =>
            v.Id != globalData.Id &&
            v.Value.Equals(request.Value, StringComparison.OrdinalIgnoreCase));

        return isDuplicate
            ? new GlobalDataDuplicateValueException(globalData.Entity, request.Value)
            : globalData;
    }

    private async Task<Either<GlobalDataException, Domain.Systems.GlobalData>> UpdateGlobalData(
        UpdateGlobalDataCommand request,
        Domain.Systems.GlobalData globalData,
        CancellationToken cancellationToken)
    {
        try
        {
            globalData.UpdateDetails(request.Entity, request.Value, request.Description);
            return await repository.UpdateAsync(globalData, cancellationToken);
        }
        catch (Exception exception)
        {
            return new GlobalDataUnknownException(globalData.Id, exception);
        }
    }
}