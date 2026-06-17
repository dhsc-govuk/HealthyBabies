using Api.Modules.Errors;
using Api.Services.Abstract;
using Application.Common;
using Application.Common.Permissions;
using Application.ServiceCategories.Commands;
using Application.ServiceCategories.Dtos;
using Application.ServiceCategoryForms.Dtos;
using Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("service-categories")]
[Authorize(Roles = Role.OrganisationAdmin)]
public class ServiceCategoriesController(
    PermissionsService permissionsService,
    ISender sender,
    IServiceCategoriesControllerService controllerService,
    IServiceCategoriesBulkUploadTemplateService bulkUploadTemplateService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceCategoryListDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<IReadOnlyList<ServiceCategoryListDto>>>(
            async p =>
            {
                var result = await controllerService.GetAll(p, cancellationToken);
                return result.ToList();
            },
            e => Unauthorized(e));
    }

    [HttpGet("{serviceCategoryId:guid}")]
    public async Task<ActionResult<ServiceCategoryDto>> Get(
        [FromRoute] Guid serviceCategoryId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync(
            async p =>
            {
                var result = await controllerService.Get(p, serviceCategoryId, cancellationToken);
                return result.Match<ActionResult<ServiceCategoryDto>>(
                    r => r,
                    NotFound());
            },
            e => Unauthorized(e));
    }

    [HttpPost("create")]
    public async Task<ActionResult<ServiceCategoryDto>> Create(
        [FromBody] CreateServiceCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var input = new CreateServiceCategoryCommand
        {
            CategoryCode = request.CategoryCode,
            CategoryName = Utility.HtmlDecodeNullableField(request.CategoryName)
        };

        var result = await sender.Send(input, cancellationToken);
        return result.Match<ActionResult<ServiceCategoryDto>>(
            s => ServiceCategoryDto.FromDomainModel(s),
            e => e.ToObjectResult());
    }

    [HttpPut("{serviceCategoryId:guid}/step-one")]
    public async Task<ActionResult<ServiceCategoryDto>> UpdateStepOne(
        [FromRoute] Guid serviceCategoryId,
        [FromBody] UpdateServiceCategoryStepOneRequest request,
        CancellationToken cancellationToken)
    {
        var input = new UpdateServiceCategoryStepOneCommand
        {
            ServiceCategoryId = serviceCategoryId,
            Answers = request.Answers.Select(a => new ServiceCategoryAnswerInputDto(a.QuestionCode, a.Value)).ToList(),
            AdvanceStep = request.AdvanceStep
        };

        var result = await sender.Send(input, cancellationToken);
        return result.Match<ActionResult<ServiceCategoryDto>>(
            s => ServiceCategoryDto.FromDomainModel(s),
            e => e.ToObjectResult());
    }

    [HttpPost("{serviceCategoryId:guid}/complete")]
    public async Task<ActionResult<ServiceCategoryDto>> Complete(
        [FromRoute] Guid serviceCategoryId,
        CancellationToken cancellationToken)
    {
        var input = new CompleteServiceCategoryCommand
        {
            ServiceCategoryId = serviceCategoryId
        };

        var result = await sender.Send(input, cancellationToken);
        return result.Match<ActionResult<ServiceCategoryDto>>(
            s => ServiceCategoryDto.FromDomainModel(s),
            e => e.ToObjectResult());
    }

    [HttpDelete("{serviceCategoryId:guid}")]
    public async Task<ActionResult> Delete(
        [FromRoute] Guid serviceCategoryId,
        CancellationToken cancellationToken)
    {
        var input = new DeleteServiceCategoryCommand
        {
            ServiceCategoryId = serviceCategoryId
        };

        var result = await sender.Send(input, cancellationToken);
        return result.Match<ActionResult>(
            _ => NoContent(),
            e => e.ToObjectResult());
    }

    [HttpGet("bulk-upload/template")]
    public async Task<ActionResult> DownloadTemplate(
        [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId.Value,
                    () => Guid.Empty);

                if (organisationId == Guid.Empty)
                {
                    return Unauthorized();
                }

                if (format.Equals("xlsx", StringComparison.OrdinalIgnoreCase) ||
                    format.Equals("excel", StringComparison.OrdinalIgnoreCase))
                {
                    var excelBytes = await bulkUploadTemplateService.GenerateExcelTemplateAsync(organisationId, cancellationToken);
                    return File(
                        excelBytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "wider_services_upload_template.xlsx");
                }

                var csvBytes = await bulkUploadTemplateService.GenerateCsvTemplateAsync(organisationId, cancellationToken);
                return File(csvBytes, "text/csv", "wider_services_upload_template.csv");
            },
            e => Task.FromResult<ActionResult>(Unauthorized(e)));
    }
}

public record CreateServiceCategoryRequest(string? CategoryCode, string? CategoryName);

public record ServiceCategoryAnswerInputRequest(string QuestionCode, string? Value);

public record UpdateServiceCategoryStepOneRequest(
    IReadOnlyList<ServiceCategoryAnswerInputRequest> Answers,
    bool AdvanceStep = true);