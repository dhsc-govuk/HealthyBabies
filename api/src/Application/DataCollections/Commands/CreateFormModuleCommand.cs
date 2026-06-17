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
}

public class CreateFormModuleCommandHandler(
    IDataCollectionFormModuleRepository moduleRepository)
    : IRequestHandler<CreateFormModuleCommand, Either<FormModuleException, DataCollectionFormModule>>
{
    public async Task<Either<FormModuleException, DataCollectionFormModule>> Handle(
        CreateFormModuleCommand request,
        CancellationToken cancellationToken)
    {
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
            request.Description);

        await moduleRepository.AddAsync(module, cancellationToken);

        return module;
    }
}