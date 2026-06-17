using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record DeleteFormModuleCommand(Guid Id) : IRequest<Either<FormModuleException, LanguageExt.Unit>>;

public class DeleteFormModuleCommandHandler(
    IDataCollectionFormModuleRepository moduleRepository)
    : IRequestHandler<DeleteFormModuleCommand, Either<FormModuleException, LanguageExt.Unit>>
{
    public async Task<Either<FormModuleException, LanguageExt.Unit>> Handle(
        DeleteFormModuleCommand request,
        CancellationToken cancellationToken)
    {
        var moduleId = DataCollectionFormModuleId.From(request.Id);
        var module = await moduleRepository.GetById(moduleId, cancellationToken);

        return await module.MatchAsync(
            async m =>
            {
                if (m.Fields.Any())
                {
                    return (Either<FormModuleException, LanguageExt.Unit>)new FormModuleHasFieldsException(request.Id);
                }

                await moduleRepository.DeleteAsync(m, cancellationToken);
                return LanguageExt.Unit.Default;
            },
            () => new FormModuleNotFoundException(request.Id));
    }
}