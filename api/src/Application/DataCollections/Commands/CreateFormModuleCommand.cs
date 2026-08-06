using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record CreateFormModuleCommand : IRequest<Either<FormModuleException, DataCollectionFormModule>>
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Type { get; init; } = string.Empty;
}

public class CreateFormModuleCommandHandler(
    IDataCollectionFormModuleRepository moduleRepository)
    : IRequestHandler<CreateFormModuleCommand, Either<FormModuleException, DataCollectionFormModule>>
{
    public async Task<Either<FormModuleException, DataCollectionFormModule>> Handle(
        CreateFormModuleCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Type != DataCollectionTypeConstants.DeliveryPlan
            && request.Type != DataCollectionTypeConstants.ManagementInformation)
        {
            return new FormModuleInvalidTypeException(request.Type);
        }

        var codeExists = await moduleRepository.ExistsByCode(request.Code, cancellationToken);
        if (codeExists)
        {
            return new FormModuleDuplicateCodeException(request.Code);
        }

        var sectionNumber = await moduleRepository.GetNextSectionNumber(cancellationToken);

        var module = DataCollectionFormModule.Create(
            DataCollectionFormModuleId.New(),
            request.Code,
            sectionNumber,
            request.Name,
            request.Description,
            type: DataCollectionType.From(request.Type));

        await moduleRepository.AddAsync(module, cancellationToken);

        return module;
    }
}