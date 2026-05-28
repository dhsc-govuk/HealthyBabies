using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record DeleteFormSectionCommand : IRequest<Either<FormModuleException, LanguageExt.Unit>>
{
    public Guid FormModuleId { get; init; }
    public Guid SectionId { get; init; }
}

public class DeleteFormSectionCommandHandler(
    IDataCollectionFormModuleRepository moduleRepository)
    : IRequestHandler<DeleteFormSectionCommand, Either<FormModuleException, LanguageExt.Unit>>
{
    public async Task<Either<FormModuleException, LanguageExt.Unit>> Handle(
        DeleteFormSectionCommand request,
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
                    return (Either<FormModuleException, LanguageExt.Unit>)new FormSectionNotFoundException(request.SectionId);
                }

                var fieldsInSection = m.Fields.Any(f => f.FormSectionId == sectionId);
                if (fieldsInSection)
                {
                    return new FormSectionHasFieldsException(request.SectionId);
                }

                m.RemoveSection(sectionId);
                await moduleRepository.UpdateAsync(m, cancellationToken);
                return LanguageExt.Unit.Default;
            },
            () => new FormModuleNotFoundException(request.FormModuleId));
    }
}