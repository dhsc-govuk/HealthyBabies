using Domain.DataCollections.Forms;

namespace Application.DataCollections.Dtos;

public record FormSectionDto(
    Guid Id,
    Guid FormModuleId,
    int SectionNumber,
    string Title,
    string? Description,
    string? HelpText,
    string? HelpUrl,
    bool IsActive)
{
    public static FormSectionDto FromDomainModel(FormSection section) =>
        new(
            section.Id.Value,
            section.FormModuleId.Value,
            section.SectionNumber,
            section.Title,
            section.Description,
            section.HelpText,
            section.HelpUrl,
            section.IsActive);
}