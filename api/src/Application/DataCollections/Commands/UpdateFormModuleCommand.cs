using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record UpdateFormModuleCommand : IRequest<Either<FormModuleException, DataCollectionFormModule>>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
}

public class UpdateFormModuleCommandHandler(
    IDataCollectionFormModuleRepository moduleRepository)
    : IRequestHandler<UpdateFormModuleCommand, Either<FormModuleException, DataCollectionFormModule>>
{
    public async Task<Either<FormModuleException, DataCollectionFormModule>> Handle(
        UpdateFormModuleCommand request,
        CancellationToken cancellationToken)
    {
        var moduleId = DataCollectionFormModuleId.From(request.Id);
        var module = await moduleRepository.GetById(moduleId, cancellationToken);

        return await module.MatchAsync(
            async m =>
            {
                m.UpdateDetails(request.Name, request.Description);

                if (request.IsActive)
                {
                    m.Activate();
                }
                else
                {
                    m.Deactivate();
                }

                await moduleRepository.UpdateAsync(m, cancellationToken);
                return (Either<FormModuleException, DataCollectionFormModule>)m;
            },
            () => new FormModuleNotFoundException(request.Id));
    }
}