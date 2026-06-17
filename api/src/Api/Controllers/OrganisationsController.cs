using Api.Modules.Errors;
using Api.Services.Abstract;
using Application.Common;
using Application.Common.Permissions;
using Application.Organisations.Commands;
using Application.Organisations.Dtos;
using Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("organisations")]
[ApiController]
[Authorize(Roles = Role.Admin)]
public class OrganisationsController(
    PermissionsService permissionsService,
    ISender sender,
    IOrganisationsControllerService controllerService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrganisationDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await controllerService.GetAll(cancellationToken);

        return result.ToList();
    }

    [HttpGet("{organisationId:guid}")]
    public async Task<ActionResult<OrganisationDto>> Get(
        [FromRoute] Guid organisationId,
        CancellationToken cancellationToken)
    {
        var result = await controllerService.Get(organisationId, cancellationToken);

        return result.Match<ActionResult<OrganisationDto>>(
            r => r,
            NotFound());
    }

    [HttpPost("create")]
    public async Task<ActionResult<OrganisationDto>> Create(
        [FromBody] CreateOrganisationDto organisation,
        CancellationToken cancellationToken)
    {
        var input = new CreateOrganisationCommand
        {
            Name = Utility.HtmlDecodeField(organisation.Name),
            ONSCode = organisation.ONSCode,
            IsActive = organisation.IsActive,
            Contacts = organisation.Contacts?
                .Select(c => new CreateContactCommand(
                    Utility.HtmlDecodeField(c.FullName),
                    Utility.HtmlDecodeField(c.Role),
                    Utility.HtmlDecodeNullableField(c.RoleTitle),
                    c.Email))
                .ToList()
        };

        var result = await sender.Send(input, cancellationToken);

        return result.Match<ActionResult<OrganisationDto>>(
            org => new OrganisationDto(org.Id.Value, org.Name, org.ONSCode, org.IsActive),
            error => error.ToObjectResult());
    }

    [HttpPut("edit")]
    public async Task<ActionResult<OrganisationDto>> Edit(
        [FromBody] OrganisationDto organisation,
        CancellationToken cancellationToken)
    {
        var input = new UpdateOrganisationCommand
        {
            Id = organisation.Id!.Value,
            Name = organisation.Name,
            ONSCode = organisation.ONSCode,
            IsActive = organisation.IsActive
        };

        var result = await sender.Send(input, cancellationToken);

        return result.Match<ActionResult<OrganisationDto>>(
            org => new OrganisationDto(
                org.Id.Value,
                org.Name,
                org.ONSCode,
                org.IsActive),
            error => error.ToObjectResult());
    }

    [HttpGet("{organisationId:guid}/totals")]
    public async Task<ActionResult<OrganisationHomeDto>> Totals(
        [FromRoute] Guid organisationId,
        CancellationToken cancellationToken)
    {
        return await permissionsService.GetOrganisationPermissions()
            .MatchAsync<ActionResult<OrganisationHomeDto>>(
                p => controllerService.Totals(p, organisationId, cancellationToken)
                    .Match<OrganisationHomeDto, ActionResult<OrganisationHomeDto>>(
                        r => r,
                        () => NotFound()),
                e => NotFound(e.Message));
    }
}