using Api.Modules.Errors;
using Api.Services.Abstract;
using Application.Common;
using Application.DataCollections.Commands;
using Application.DataCollections.Dtos;
using Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("admin/data-collections")]
[ApiController]
[Authorize(Roles = Role.Admin)]
public class DataCollectionsController(
    ISender sender,
    IDataCollectionsControllerService controllerService,
    ISubmissionExportService submissionExportService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DataCollectionDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await controllerService.GetAll(cancellationToken);

        return result.ToList();
    }

    [HttpGet("form-modules")]
    public async Task<ActionResult<IReadOnlyList<DataCollectionFormModuleDto>>> GetFormModules(CancellationToken cancellationToken)
    {
        var result = await controllerService.GetAllFormModules(cancellationToken);

        return result.ToList();
    }

    [HttpGet("{dataCollectionId:guid}")]
    public async Task<ActionResult<DataCollectionDto>> Get(
        [FromRoute] Guid dataCollectionId,
        CancellationToken cancellationToken)
    {
        var result = await controllerService.GetWithSubmissions(dataCollectionId, cancellationToken);

        return result.Match<ActionResult<DataCollectionDto>>(
            r => r,
            NotFound());
    }

    [HttpGet("{dataCollectionId:guid}/full")]
    public async Task<ActionResult<DataCollectionDto>> GetFull(
        [FromRoute] Guid dataCollectionId,
        CancellationToken cancellationToken)
    {
        var result = await controllerService.GetWithLocalAuthorities(dataCollectionId, cancellationToken);

        return result.Match<ActionResult<DataCollectionDto>>(
            r => r,
            NotFound());
    }

    [HttpPost("create")]
    public async Task<ActionResult<DataCollectionDto>> Create(
        [FromBody] CreateDataCollectionDto dto,
        CancellationToken cancellationToken)
    {
        var command = new CreateDataCollectionCommand
        {
            Name = Utility.HtmlDecodeField(dto.Name),
            Description = Utility.HtmlDecodeNullableField(dto.Description),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            SaveAsDraft = dto.SaveAsDraft,
            IsSubmittedByAllLocalAuthorities = dto.IsSubmittedByAllLocalAuthorities,
            LocalAuthorityIds = dto.LocalAuthorityIds ?? [],
            FormModuleIds = dto.FormModuleIds ?? []
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<DataCollectionDto>>(
            dc => DataCollectionDto.FromDomainModel(dc),
            error => error.ToObjectResult());
    }

    [HttpPut("{dataCollectionId:guid}/edit")]
    public async Task<ActionResult<DataCollectionDto>> Edit(
        [FromRoute] Guid dataCollectionId,
        [FromBody] UpdateDataCollectionDto dto,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDataCollectionCommand
        {
            Id = dataCollectionId,
            Name = Utility.HtmlDecodeField(dto.Name),
            Description = Utility.HtmlDecodeNullableField(dto.Description),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            SaveAsDraft = dto.SaveAsDraft,
            IsSubmittedByAllLocalAuthorities = dto.IsSubmittedByAllLocalAuthorities,
            LocalAuthorityIds = dto.LocalAuthorityIds ?? [],
            FormModuleIds = dto.FormModuleIds ?? []
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<DataCollectionDto>>(
            dc => DataCollectionDto.FromDomainModelWithLocalAuthorities(dc),
            error => error.ToObjectResult());
    }

    [HttpDelete("{dataCollectionId:guid}")]
    public async Task<ActionResult<DataCollectionDto>> Delete(
        [FromRoute] Guid dataCollectionId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteDataCollectionCommand { Id = dataCollectionId };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<DataCollectionDto>>(
            dc => DataCollectionDto.FromDomainModel(dc),
            error => error.ToObjectResult());
    }

    [HttpGet("{dataCollectionId:guid}/local-authorities")]
    public async Task<ActionResult<DataCollectionDto>> GetWithLocalAuthorities(
        [FromRoute] Guid dataCollectionId,
        CancellationToken cancellationToken)
    {
        var result = await controllerService.GetWithLocalAuthorities(dataCollectionId, cancellationToken);

        return result.Match<ActionResult<DataCollectionDto>>(
            r => r,
            NotFound());
    }

    [HttpPut("{dataCollectionId:guid}/local-authorities")]
    public async Task<ActionResult<DataCollectionDto>> UpdateLocalAuthorities(
        [FromRoute] Guid dataCollectionId,
        [FromBody] UpdateDataCollectionLocalAuthoritiesDto dto,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDataCollectionLocalAuthoritiesCommand
        {
            DataCollectionId = dataCollectionId,
            LocalAuthorityIds = dto.LocalAuthorityIds
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<DataCollectionDto>>(
            dc => DataCollectionDto.FromDomainModelWithLocalAuthorities(dc),
            error => error.ToObjectResult());
    }

    [HttpDelete("{dataCollectionId:guid}/local-authorities/{localAuthorityId:guid}")]
    public async Task<ActionResult<DataCollectionDto>> RemoveLocalAuthority(
        [FromRoute] Guid dataCollectionId,
        [FromRoute] Guid localAuthorityId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveLocalAuthorityFromDataCollectionCommand
        {
            DataCollectionId = dataCollectionId,
            LocalAuthorityId = localAuthorityId
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<DataCollectionDto>>(
            dc => DataCollectionDto.FromDomainModelWithLocalAuthorities(dc),
            error => error.ToObjectResult());
    }

    [HttpPatch("{dataCollectionId:guid}/local-authorities/{localAuthorityId:guid}/end-date")]
    public async Task<ActionResult<DataCollectionDto>> UpdateLocalAuthorityEndDate(
        [FromRoute] Guid dataCollectionId,
        [FromRoute] Guid localAuthorityId,
        [FromBody] UpdateLocalAuthorityEndDateDto dto,
        CancellationToken cancellationToken)
    {
        var command = new UpdateLocalAuthorityEndDateCommand
        {
            DataCollectionId = dataCollectionId,
            LocalAuthorityId = localAuthorityId,
            EndDate = dto.EndDate
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<DataCollectionDto>>(
            dc => DataCollectionDto.FromDomainModelWithLocalAuthorities(dc),
            error => error.ToObjectResult());
    }

    [HttpPost("{dataCollectionId:guid}/close")]
    public async Task<ActionResult<DataCollectionDto>> Close(
        [FromRoute] Guid dataCollectionId,
        CancellationToken cancellationToken)
    {
        var command = new CloseDataCollectionCommand { Id = dataCollectionId };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<DataCollectionDto>>(
            dc => DataCollectionDto.FromDomainModel(dc),
            error => error.ToObjectResult());
    }

    [HttpPost("{dataCollectionId:guid}/revert-to-draft")]
    public async Task<ActionResult<DataCollectionDto>> RevertToDraft(
        [FromRoute] Guid dataCollectionId,
        CancellationToken cancellationToken)
    {
        var command = new RevertDataCollectionToDraftCommand { Id = dataCollectionId };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<DataCollectionDto>>(
            dc => DataCollectionDto.FromDomainModel(dc),
            error => error.ToObjectResult());
    }

    [HttpPost("{dataCollectionId:guid}/duplicate")]
    public async Task<ActionResult<DataCollectionDto>> Duplicate(
        [FromRoute] Guid dataCollectionId,
        [FromBody] DuplicateDataCollectionDto? dto,
        CancellationToken cancellationToken)
    {
        var command = new DuplicateDataCollectionCommand
        {
            SourceId = dataCollectionId,
            NewName = Utility.HtmlDecodeNullableField(dto?.NewName)
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<DataCollectionDto>>(
            dc => DataCollectionDto.FromDomainModel(dc),
            error => error.ToObjectResult());
    }

    [HttpGet("{dataCollectionId:guid}/local-authorities/{localAuthorityId:guid}")]
    public async Task<ActionResult<LocalAuthoritySubmissionDetailDto>> GetLocalAuthoritySubmission(
        [FromRoute] Guid dataCollectionId,
        [FromRoute] Guid localAuthorityId,
        CancellationToken cancellationToken)
    {
        var result = await controllerService.GetLocalAuthoritySubmission(dataCollectionId, localAuthorityId, cancellationToken);

        return result.Match<ActionResult<LocalAuthoritySubmissionDetailDto>>(
            r => r,
            NotFound());
    }

    [HttpGet("{dataCollectionId:guid}/local-authorities/{localAuthorityId:guid}/download")]
    public async Task<ActionResult> DownloadSubmission(
        [FromRoute] Guid dataCollectionId,
        [FromRoute] Guid localAuthorityId,
        [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var jsonResult = await submissionExportService.ExportSubmissionAsJsonAsync(
                dataCollectionId, localAuthorityId, cancellationToken);

            return jsonResult.Match<ActionResult>(
                bytes => File(bytes, "application/json", $"submission_{dataCollectionId}_{localAuthorityId}.json"),
                error => NotFound(error));
        }

        var csvResult = await submissionExportService.ExportSubmissionAsCsvZipAsync(
            dataCollectionId, localAuthorityId, cancellationToken);

        return csvResult.Match<ActionResult>(
            bytes => File(bytes, "application/zip", $"submission_{dataCollectionId}_{localAuthorityId}.zip"),
            error => NotFound(error));
    }

    [HttpGet("{dataCollectionId:guid}/download/{format}")]
    public async Task<ActionResult> DownloadConsolidatedSubmission(
        [FromRoute] Guid dataCollectionId,
        [FromRoute] string format,
        CancellationToken cancellationToken = default)
    {
        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var jsonResult = await submissionExportService.ExportConsolidatedJsonAsync(
                dataCollectionId, cancellationToken);

            return jsonResult.Match<ActionResult>(
                bytes => File(bytes, "application/json", $"data_collection_{dataCollectionId}.json"),
                error => NotFound(error));
        }

        var csvResult = await submissionExportService.ExportConsolidatedCsvZipAsync(
            dataCollectionId, cancellationToken);

        return csvResult.Match<ActionResult>(
            bytes => File(bytes, "application/zip", $"data_collection_{dataCollectionId}.zip"),
            error => NotFound(error));
    }
}