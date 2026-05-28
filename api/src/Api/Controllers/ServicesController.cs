using Api.Modules.Errors;
using Api.Services.Abstract;
using Application.Common;
using Application.Common.Permissions;
using Application.ServiceForms.Dtos;
using Application.Services.Commands;
using Application.Services.Dtos;
using Application.Services.Queries;
using Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("services")]
[Authorize(Roles = Role.OrganisationAdmin)]
public class ServicesController(
    PermissionsService permissionsService,
    ISender sender,
    IServicesControllerService controllerService,
    IServicesBulkUploadTemplateService bulkUploadTemplateService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceListDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<IReadOnlyList<ServiceListDto>>>(
            async p =>
            {
                var result = await controllerService.GetAll(p, cancellationToken);
                return result.ToList();
            },
            e => Unauthorized(e));
    }

    [HttpGet("{serviceId:guid}")]
    public async Task<ActionResult<ServiceDto>> Get(
        [FromRoute] Guid serviceId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync(
            async p =>
            {
                var result = await controllerService.Get(p, serviceId, cancellationToken);
                return result.Match<ActionResult<ServiceDto>>(
                    r => r,
                    NotFound());
            },
            e => Unauthorized(e));
    }

    [HttpPost("create")]
    public async Task<ActionResult<ServiceDto>> Create(
        [FromBody] CreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var input = new CreateServiceCommand
        {
            Name = Utility.HtmlDecodeNullableField(request.Name)
        };

        var result = await sender.Send(input, cancellationToken);
        return result.Match<ActionResult<ServiceDto>>(
            s => ServiceDto.FromDomainModel(s),
            e => e.ToObjectResult());
    }

    [HttpPut("{serviceId:guid}/step-one")]
    public async Task<ActionResult<ServiceDto>> UpdateStepOne(
        [FromRoute] Guid serviceId,
        [FromBody] UpdateServiceStepOneRequest request,
        CancellationToken cancellationToken)
    {
        var input = new UpdateServiceStepOneCommand
        {
            ServiceId = serviceId,
            Name = Utility.HtmlDecodeNullableField(request.Name),
            Answers = request.Answers.Select(a => new ServiceAnswerInputDto(a.QuestionCode, Utility.HtmlDecodeNullableField(a.Value))).ToList(),
            AdvanceStep = request.AdvanceStep
        };

        var result = await sender.Send(input, cancellationToken);
        return result.Match<ActionResult<ServiceDto>>(
            s => ServiceDto.FromDomainModel(s),
            e => e.ToObjectResult());
    }

    [HttpPut("{serviceId:guid}/step-two")]
    public async Task<ActionResult<ServiceDto>> UpdateStepTwo(
        [FromRoute] Guid serviceId,
        [FromBody] UpdateServiceStepTwoRequest request,
        CancellationToken cancellationToken)
    {
        var input = new UpdateServiceStepTwoCommand
        {
            ServiceId = serviceId,
            Answers = request.Answers.Select(a => new ServiceAnswerInputDto(a.QuestionCode, Utility.HtmlDecodeNullableField(a.Value))).ToList(),
            AdvanceStep = request.AdvanceStep
        };

        var result = await sender.Send(input, cancellationToken);
        return result.Match<ActionResult<ServiceDto>>(
            s => ServiceDto.FromDomainModel(s),
            e => e.ToObjectResult());
    }

    [HttpPost("{serviceId:guid}/complete")]
    public async Task<ActionResult<ServiceDto>> Complete(
        [FromRoute] Guid serviceId,
        CancellationToken cancellationToken)
    {
        var input = new CompleteServiceCommand
        {
            ServiceId = serviceId
        };

        var result = await sender.Send(input, cancellationToken);
        return result.Match<ActionResult<ServiceDto>>(
            s => ServiceDto.FromDomainModel(s),
            e => e.ToObjectResult());
    }

    [HttpDelete("{serviceId:guid}")]
    public async Task<ActionResult> Delete(
        [FromRoute] Guid serviceId,
        CancellationToken cancellationToken)
    {
        var input = new DeleteServiceCommand
        {
            ServiceId = serviceId
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
        if (format.Equals("xlsx", StringComparison.OrdinalIgnoreCase) ||
            format.Equals("excel", StringComparison.OrdinalIgnoreCase))
        {
            var excelBytes = await bulkUploadTemplateService.GenerateExcelTemplateAsync(cancellationToken);
            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "services_upload_template.xlsx");
        }

        var csvBytes = await bulkUploadTemplateService.GenerateCsvTemplateAsync(cancellationToken);
        return File(csvBytes, "text/csv", "services_upload_template.csv");
    }

    [HttpPost("bulk-upload/match")]
    public async Task<ActionResult<IReadOnlyList<ServiceMatchResult>>> MatchServices(
        [FromBody] MatchServicesRequest request,
        CancellationToken cancellationToken)
    {
        var input = new MatchServicesByNameQuery
        {
            ServiceNames = request.ServiceNames
        };

        var result = await sender.Send(input, cancellationToken);
        return Ok(result);
    }

    [HttpPost("bulk-upload")]
    public async Task<ActionResult<BulkUpdateServicesResult>> BulkUpdate(
        [FromBody] BulkUpdateServicesRequest request,
        CancellationToken cancellationToken)
    {
        var services = request.Services.Select(s => new ServiceBulkUpdateItem(
            s.ServiceId,
            Utility.HtmlDecodeField(s.Name),
            s.Answers.Select(a => new ServiceAnswerInputDto(a.QuestionCode, Utility.HtmlDecodeNullableField(a.Value))).ToList())).ToList();

        var input = new BulkUpdateServicesCommand
        {
            DataCollectionId = request.DataCollectionId,
            Services = services
        };

        var result = await sender.Send(input, cancellationToken);
        return result.Match<ActionResult<BulkUpdateServicesResult>>(
            r => r,
            e => e.ToObjectResult());
    }
}

public record CreateServiceRequest(string? Name);

public record AnswerInputRequest(string QuestionCode, string? Value);

public record UpdateServiceStepOneRequest(
    string? Name,
    IReadOnlyList<AnswerInputRequest> Answers,
    bool AdvanceStep = true);

public record UpdateServiceStepTwoRequest(
    IReadOnlyList<AnswerInputRequest> Answers,
    bool AdvanceStep = true);

public record MatchServicesRequest(IReadOnlyList<string> ServiceNames);

public record BulkUpdateServiceItemRequest(
    Guid? ServiceId,
    string Name,
    IReadOnlyList<AnswerInputRequest> Answers);

public record BulkUpdateServicesRequest(Guid? DataCollectionId, IReadOnlyList<BulkUpdateServiceItemRequest> Services);