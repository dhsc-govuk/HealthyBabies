using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record CreateFormSectionCommand : IRequest<Either<FormModuleException, FormSection>>
{
    public Guid FormModuleId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? HelpText { get; init; }
    public string? HelpUrl { get; init; }
}

public class CreateFormSectionCommandHandler(
    IDataCollectionFormModuleRepository moduleRepository)
    : IRequestHandler<CreateFormSectionCommand, Either<FormModuleException, FormSection>>
{
    public async Task<Either<FormModuleException, FormSection>> Handle(
        CreateFormSectionCommand request,
        CancellationToken cancellationToken)
    {
        var moduleId = DataCollectionFormModuleId.From(request.FormModuleId);
        var module = await moduleRepository.GetById(moduleId, cancellationToken);

        return await module.MatchAsync(
            async m =>
            {
                var nextSectionNumber = m.Sections.Count + 1;

                var section = m.AddSection(
                    nextSectionNumber,
                    request.Title,
                    request.Description,
                    request.HelpText,
                    request.HelpUrl);

                await moduleRepository.UpdateAsync(m, cancellationToken);
                return (Either<FormModuleException, FormSection>)section;
            },
            () => new FormModuleNotFoundException(request.FormModuleId));
    }
}