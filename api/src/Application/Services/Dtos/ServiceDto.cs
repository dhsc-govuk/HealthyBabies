using Application.ServiceForms.Dtos;
using Domain.Services;

namespace Application.Services.Dtos;

public record ServiceDto(
    Guid? Id,
    Guid OrganisationId,
    string Name,
    int Status,
    int CurrentStep,
    IReadOnlyList<ServiceAnswerDto> Answers)
{
    public static ServiceDto FromDomainModel(Service service) =>
        new(
            service.Id.Value,
            service.OrganisationId.Value,
            service.Name,
            (int)service.Status,
            service.CurrentStep,
            service.Answers.Select(ServiceAnswerDto.FromDomainModel).ToList());
}

public record ServiceListDto(
    Guid Id,
    string Name,
    int Status,
    int CurrentStep)
{
    public static ServiceListDto FromDomainModel(Service service) =>
        new(
            service.Id.Value,
            service.Name,
            (int)service.Status,
            service.CurrentStep);
}