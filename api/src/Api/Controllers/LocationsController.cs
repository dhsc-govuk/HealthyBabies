using Amazon.Runtime.Internal;
using Api.Modules.Errors;
using Api.Services.Abstract;
using Application.Common;
using Application.Common.FileValidation;
using Application.Common.Permissions;
using Application.Locations.Commands;
using Application.Organisations.Commands;
using Application.Organisations.Dtos;
using Application.Organisations.Queries;
using Application.SiteForms.Dtos;
using Domain.Organisations;
using Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class LocationsController(
    PermissionsService permissionsService,
    ISender sender,
    ILocationsControllerService controllerService,
    IBulkUploadTemplateService bulkUploadTemplateService,
    IFileValidationService fileValidationService)
    : ControllerBase
{
    [HttpGet("organisations/{organisationId:guid}/locations")]
    [Authorize(Roles = $"{Role.Admin},{Role.OrganisationAdmin}")]
    public async Task<ActionResult<IReadOnlyList<LocationDto>>> GetAll(
        [FromRoute] Guid organisationId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<IReadOnlyList<LocationDto>>>(
            async p =>
            {
                var result = await controllerService.GetAll(p, organisationId, cancellationToken);

                return result.ToList();
            },
            e => Unauthorized(e));
    }

    [HttpGet("locations")]
    [Authorize(Roles = $"{Role.Admin},{Role.OrganisationAdmin}")]
    public async Task<ActionResult<IReadOnlyList<LocationDto>>> GetGlobal(CancellationToken cancellationToken)
    {
        var result = await controllerService.GetGlobal(cancellationToken);

        return result.ToList();
    }

    [HttpGet("organisations/{organisationId:guid}/locations/{locationId:guid}")]
    [Authorize(Roles = $"{Role.Admin},{Role.OrganisationAdmin}")]
    public async Task<ActionResult<LocationDto>> Get(
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync(
            async p =>
            {
                var result = await controllerService.Get(p, locationId, cancellationToken);

                return result.Match<ActionResult<LocationDto>>(
                    r => r,
                    NotFound());
            },
            e => Unauthorized(e));
    }

    [HttpPost("organisations/{organisationId:guid}/locations/create")]
    [Authorize(Roles = $"{Role.Admin},{Role.OrganisationAdmin}")]
    public async Task<ActionResult<LocationDto>> Create(
     [FromRoute] Guid organisationId,
     [FromBody] CreateLocationInputDto location,
     CancellationToken cancellationToken)
    {
        var input = new CreateLocationCommand
        {
            OrganisationId = organisationId,
            IsActive = true,
            Answers = location.Answers.Select(a => new SiteAnswerInputDto(a.QuestionCode, Utility.HtmlDecodeNullableField(a.Value))).ToList()
        };

        var result = await sender.Send(input, cancellationToken);

        return result.Match<ActionResult<LocationDto>>(
            l => new LocationDto(
                l.Id.Value,
                l.OrganisationId.Value,
                l.Name,
                l.PostCode,
                l.ReferenceNumber,
                l.AddressLine1,
                l.AddressLine2,
                l.TownOrCity,
                l.County,
                l.IsActive,
                l.Answers.Select(SiteAnswerDto.FromDomainModel).ToList()),
            e => e.ToObjectResult());
    }

    [HttpPut("organisations/{organisationId:guid}/locations/edit")]
    [Authorize(Roles = $"{Role.Admin},{Role.OrganisationAdmin}")]
    public async Task<ActionResult<LocationDto>> Edit(
        [FromBody] UpdateLocationInputDto location,
        CancellationToken cancellationToken)
    {
        var input = new UpdateLocationCommand
        {
            Id = location.Id,
            IsActive = location.IsActive,
            Answers = location.Answers.Select(a => new SiteAnswerInputDto(a.QuestionCode, Utility.HtmlDecodeNullableField(a.Value))).ToList()
        };

        var result = await sender.Send(input, cancellationToken);

        return result.Match<ActionResult<LocationDto>>(
            l => new LocationDto(
                l.Id.Value,
                l.OrganisationId.Value,
                l.Name,
                l.PostCode,
                l.ReferenceNumber,
                l.AddressLine1,
                l.AddressLine2,
                l.TownOrCity,
                l.County,
                l.IsActive,
                l.Answers.Select(SiteAnswerDto.FromDomainModel).ToList()),
            e => e.ToObjectResult());
    }

    [HttpGet("organisations/{organisationId:guid}/locations/{locationId:guid}/totals")]
    [Authorize(Roles = $"{Role.Admin},{Role.OrganisationAdmin}")]
    public async Task<ActionResult<LocationHomeDto>> Totals(
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync(
            async p =>
            {
                var result = await controllerService.Totals(p, locationId, cancellationToken);

                return result.Match<ActionResult<LocationHomeDto>>(
                    r => r,
                    NotFound());
            },
            e => Unauthorized(e));
    }

    [HttpPost("organisations/{organisationId:guid}/locations/bulk-upload/validate")]
    [Authorize(Roles = $"{Role.Admin},{Role.OrganisationAdmin}")]
    public async Task<ActionResult<BulkUploadLocationsResult>> ValidateBulkUpload(
        [FromRoute] Guid organisationId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        var validation = await fileValidationService.ValidateAsync(file, FileUploadProfile.BulkUploadCsvOrExcel, cancellationToken);
        var validationError = validation.Match<string?>(_ => null, e => e.Message);
        if (validationError is not null)
        {
            return BadRequest(new ErrorResponse { Message = validationError });
        }

        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync(
            async p =>
            {
                var command = new ValidateBulkUploadLocationsCommand
                {
                    File = file,
                    OrganisationId = new OrganisationId(organisationId)
                };

                var result = await sender.Send(command, cancellationToken);
                return result.Match<ActionResult<BulkUploadLocationsResult>>(
                    success => Ok(success),
                    error => BadRequest(new ErrorResponse { Message = error.Message }));
            },
            e => Unauthorized(new ErrorResponse { Message = e.Message }));
    }

    [HttpPost("organisations/{organisationId:guid}/locations/bulk-upload/confirm")]
    [Authorize(Roles = $"{Role.Admin},{Role.OrganisationAdmin}")]
    public async Task<ActionResult<ConfirmBulkUploadLocationsResult>> ConfirmBulkUpload(
        [FromRoute] Guid organisationId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        var validation = await fileValidationService.ValidateAsync(file, FileUploadProfile.BulkUploadCsvOrExcel, cancellationToken);
        var validationError = validation.Match<string?>(_ => null, e => e.Message);
        if (validationError is not null)
        {
            return BadRequest(new ErrorResponse { Message = validationError });
        }

        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync(
            async p =>
            {
                var command = new ConfirmBulkUploadLocationsCommand
                {
                    File = file,
                    OrganisationId = new OrganisationId(organisationId)
                };

                var result = await sender.Send(command, cancellationToken);
                return result.Match<ActionResult<ConfirmBulkUploadLocationsResult>>(
                    success => Ok(success),
                    error => BadRequest(new ErrorResponse { Message = error.Message }));
            },
            e => Unauthorized(new ErrorResponse { Message = e.Message }));
    }

    [HttpGet("organisations/{organisationId:guid}/locations/bulk-upload/template")]
    [Authorize(Roles = $"{Role.Admin},{Role.OrganisationAdmin}")]
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
                "locations_upload_template.xlsx");
        }

        var csvBytes = await bulkUploadTemplateService.GenerateCsvTemplateAsync(cancellationToken);
        return File(csvBytes, "text/csv", "locations_upload_template.csv");
    }

    [HttpPost("organisations/{organisationId:guid}/locations/bulk-upload/match")]
    [Authorize(Roles = $"{Role.Admin},{Role.OrganisationAdmin}")]
    public async Task<ActionResult<IReadOnlyList<LocationMatchResult>>> MatchLocations(
        [FromBody] MatchLocationsRequest request,
        CancellationToken cancellationToken)
    {
        var input = new MatchLocationsByNameQuery
        {
            SiteNames = request.SiteNames
        };

        var result = await sender.Send(input, cancellationToken);
        return Ok(result);
    }

    [HttpPost("organisations/{organisationId:guid}/locations/bulk-upload/update")]
    [Authorize(Roles = $"{Role.Admin},{Role.OrganisationAdmin}")]
    public async Task<ActionResult<BulkUpdateLocationsResult>> BulkUpdate(
        [FromRoute] Guid organisationId,
        [FromBody] BulkUpdateLocationsRequest request,
        CancellationToken cancellationToken)
    {
        var locations = request.Locations.Select(l => new LocationBulkUpdateItem(
            l.LocationId,
            Utility.HtmlDecodeField(l.Name),
            l.Answers.Select(a => new SiteAnswerInputDto(a.QuestionCode, Utility.HtmlDecodeNullableField(a.Value))).ToList())).ToList();

        var input = new BulkUpdateLocationsCommand
        {
            OrganisationId = organisationId,
            Locations = locations
        };

        var result = await sender.Send(input, cancellationToken);
        return result.Match<ActionResult<BulkUpdateLocationsResult>>(
            r => r,
            e => e.ToObjectResult());
    }
}

public record MatchLocationsRequest(IReadOnlyList<string> SiteNames);

// public record SiteAnswerInputRequest(string QuestionCode, string? Value);
public record BulkUpdateLocationItemRequest(
    Guid? LocationId,
    string Name,
    IReadOnlyList<AnswerInputRequest> Answers);

public record BulkUpdateLocationsRequest(IReadOnlyList<BulkUpdateLocationItemRequest> Locations);