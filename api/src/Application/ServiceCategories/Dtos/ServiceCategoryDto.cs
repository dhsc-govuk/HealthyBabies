using Application.ServiceCategoryForms.Dtos;
using Domain.ServiceCategories;

namespace Application.ServiceCategories.Dtos;

public record ServiceCategoryDto(
    Guid? Id,
    Guid OrganisationId,
    string CategoryCode,
    string CategoryName,
    int Status,
    int CurrentStep,
    IReadOnlyList<ServiceCategoryAnswerDto> Answers)
{
    public static ServiceCategoryDto FromDomainModel(ServiceCategory serviceCategory) =>
        new(
            serviceCategory.Id.Value,
            serviceCategory.OrganisationId.Value,
            serviceCategory.CategoryCode,
            serviceCategory.CategoryName,
            (int)serviceCategory.Status,
            serviceCategory.CurrentStep,
            serviceCategory.Answers.Select(ServiceCategoryAnswerDto.FromDomainModel).ToList());
}

public record ServiceCategoryListDto(
    Guid Id,
    string CategoryCode,
    string CategoryName,
    int Status,
    int CurrentStep)
{
    public static ServiceCategoryListDto FromDomainModel(ServiceCategory serviceCategory) =>
        new(
            serviceCategory.Id.Value,
            serviceCategory.CategoryCode,
            serviceCategory.CategoryName,
            (int)serviceCategory.Status,
            serviceCategory.CurrentStep);
}