using Domain.DataCollections;

namespace Application.DataCollections.Dtos;

public record DataCollectionFormModuleDto(
    Guid Id,
    string Code,
    int SectionNumber,
    string Name,
    string? Description,
    DateTime LastChangedOn,
    bool IsActive)
{
    public static DataCollectionFormModuleDto FromDomainModel(DataCollectionFormModule formModule)
    {
        return new DataCollectionFormModuleDto(
            formModule.Id.Value,
            formModule.Code,
            formModule.SectionNumber,
            formModule.Name,
            formModule.Description,
            formModule.LastChangedOn,
            formModule.IsActive);
    }
}

public record DataCollectionFormModuleWithFieldsDto(
    Guid Id,
    string Code,
    int SectionNumber,
    string Name,
    string? Description,
    DateTime LastChangedOn,
    bool IsActive,
    IReadOnlyList<FormSectionDto> Sections,
    IReadOnlyList<FormFieldDto> Fields)
{
    public static DataCollectionFormModuleWithFieldsDto FromDomainModel(DataCollectionFormModule formModule)
    {
        return new DataCollectionFormModuleWithFieldsDto(
            formModule.Id.Value,
            formModule.Code,
            formModule.SectionNumber,
            formModule.Name,
            formModule.Description,
            formModule.LastChangedOn,
            formModule.IsActive,
            formModule.Sections.Select(FormSectionDto.FromDomainModel).ToList(),
            formModule.Fields.OrderBy(f => f.DisplayOrder).Select(FormFieldDto.FromDomainModel).ToList());
    }
}