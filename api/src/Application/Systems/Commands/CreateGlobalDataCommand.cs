using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Systems.Exceptions;
using Domain.Systems;
using LanguageExt;
using MediatR;

namespace Application.Systems.Commands;

public record CreateGlobalDataCommand : IRequest<Either<GlobalDataException, Domain.Systems.GlobalData>>
{
    public string Entity { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public class CreateGlobalDataCommandHandler(
    IGlobalDataRepository repository,
    IGlobalDataQueries globalDataQueries)
    : IRequestHandler<CreateGlobalDataCommand, Either<GlobalDataException, Domain.Systems.GlobalData>>
{
    public async Task<Either<GlobalDataException, Domain.Systems.GlobalData>> Handle(
        CreateGlobalDataCommand request,
        CancellationToken cancellationToken)
    {
        return await ValidateEntityAsync(request)
            .BindAsync(validatedRequest => ValidateDuplicateAsync(validatedRequest, cancellationToken))
            .BindAsync(validatedRequest => CreateGlobalData(validatedRequest, cancellationToken));
    }

    private Either<GlobalDataException, CreateGlobalDataCommand> ValidateEntityAsync(
        CreateGlobalDataCommand request)
    {
        return GlobalDataEntityTypes.IsValidEntity(request.Entity)
            ? request
            : new GlobalDataInvalidEntityException(request.Entity);
    }

    private async Task<Either<GlobalDataException, CreateGlobalDataCommand>> ValidateDuplicateAsync(
        CreateGlobalDataCommand request,
        CancellationToken cancellationToken)
    {
        var existingValues = await globalDataQueries.GetByEntityAsync(request.Entity, cancellationToken);
        var isDuplicate = existingValues.Any(v => v.Value.Equals(request.Value, StringComparison.OrdinalIgnoreCase));

        return isDuplicate
            ? new GlobalDataDuplicateValueException(request.Entity, request.Value)
            : request;
    }

    private async Task<Either<GlobalDataException, Domain.Systems.GlobalData>> CreateGlobalData(
        CreateGlobalDataCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var globalData = Domain.Systems.GlobalData.New(
                request.Entity,
                request.Value,
                request.Description);

            return await repository.AddAsync(globalData, cancellationToken);
        }
        catch (Exception exception)
        {
            return new GlobalDataUnknownException(GlobalDataId.Empty(), exception);
        }
    }
}