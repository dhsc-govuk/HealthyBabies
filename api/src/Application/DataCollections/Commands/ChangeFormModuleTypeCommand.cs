using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record ChangeFormModuleTypeCommand : IRequest<Either<FormModuleException, DataCollectionFormModule>>
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
}

public class ChangeFormModuleTypeCommandHandler(
    IDataCollectionFormModuleRepository moduleRepository)
    : IRequestHandler<ChangeFormModuleTypeCommand, Either<FormModuleException, DataCollectionFormModule>>
{
    public async Task<Either<FormModuleException, DataCollectionFormModule>> Handle(
        ChangeFormModuleTypeCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Type != DataCollectionTypeConstants.DeliveryPlan
            && request.Type != DataCollectionTypeConstants.ManagementInformation)
        {
            return new FormModuleInvalidTypeException(request.Type);
        }

        var moduleId = DataCollectionFormModuleId.From(request.Id);
        var module = await moduleRepository.GetById(moduleId, cancellationToken);

        return await module.MatchAsync(
            async m =>
            {
                m.UpdateType(DataCollectionType.From(request.Type));
                await moduleRepository.UpdateAsync(m, cancellationToken);
                return (Either<FormModuleException, DataCollectionFormModule>)m;
            },
            () => new FormModuleNotFoundException(request.Id));
    }
}