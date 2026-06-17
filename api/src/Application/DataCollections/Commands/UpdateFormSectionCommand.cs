using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record UpdateFormSectionCommand : IRequest<Either<FormModuleException, FormSection>>
{
    public Guid FormModuleId { get; init; }
    public Guid SectionId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? HelpText { get; init; }
    public string? HelpUrl { get; init; }
}

public class UpdateFormSectionCommandHandler(
    IDataCollectionFormModuleRepository moduleRepository)
    : IRequestHandler<UpdateFormSectionCommand, Either<FormModuleException, FormSection>>
{
    public async Task<Either<FormModuleException, FormSection>> Handle(
        UpdateFormSectionCommand request,
        CancellationToken cancellationToken)
    {
        var moduleId = DataCollectionFormModuleId.From(request.FormModuleId);
        var module = await moduleRepository.GetById(moduleId, cancellationToken);

        return await module.MatchAsync(
            async m =>
            {
                var sectionId = FormSectionId.From(request.SectionId);
                var section = m.Sections.FirstOrDefault(s => s.Id == sectionId);

                if (section == null)
                {
                    return (Either<FormModuleException, FormSection>)new FormSectionNotFoundException(request.SectionId);
                }

                section.UpdateDetails(request.Title, request.Description, request.HelpText, request.HelpUrl);
                await moduleRepository.UpdateAsync(m, cancellationToken);
                return section;
            },
            () => new FormModuleNotFoundException(request.FormModuleId));
    }
}