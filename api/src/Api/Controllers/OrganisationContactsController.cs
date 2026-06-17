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

[Route("organisations/{organisationId:guid}/contacts")]
[ApiController]
[Authorize(Roles = $"{Role.Admin},{Role.OrganisationAdmin}")]
public class OrganisationContactsController(
    PermissionsService permissionsService,
    ISender sender,
    IOrganisationContactsControllerService controllerService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrganisationContactDto>>> GetAll(
        [FromRoute] Guid organisationId,
        CancellationToken cancellationToken)
    {
        var result = await controllerService.GetByOrganisationId(organisationId, cancellationToken);
        return result.ToList();
    }

    [HttpGet("{contactId:guid}")]
    public async Task<ActionResult<OrganisationContactDto>> Get(
        [FromRoute] Guid organisationId,
        [FromRoute] Guid contactId,
        CancellationToken cancellationToken)
    {
        var result = await controllerService.GetById(contactId, cancellationToken);
        return result.Match<ActionResult<OrganisationContactDto>>(
            r => r,
            NotFound());
    }

    [HttpPost]
    public async Task<ActionResult<OrganisationContactDto>> Create(
        [FromRoute] Guid organisationId,
        [FromBody] CreateOrganisationContactDto contact,
        CancellationToken cancellationToken)
    {
        var input = new CreateOrganisationContactCommand
        {
            OrganisationId = organisationId,
            Name = Utility.HtmlDecodeField(contact.Name),
            Email = contact.Email,
            Role = Utility.HtmlDecodeField(contact.Role)
        };

        var result = await sender.Send(input, cancellationToken);

        return result.Match<ActionResult<OrganisationContactDto>>(
            c => new OrganisationContactDto(
                c.Id.Value,
                c.OrganisationId.Value,
                c.Name,
                c.Email,
                c.Role),
            error => error.ToObjectResult());
    }

    [HttpPut("{contactId:guid}")]
    public async Task<ActionResult<OrganisationContactDto>> Update(
        [FromRoute] Guid organisationId,
        [FromRoute] Guid contactId,
        [FromBody] UpdateOrganisationContactDto contact,
        CancellationToken cancellationToken)
    {
        var input = new UpdateOrganisationContactCommand
        {
            Id = contactId,
            Name = contact.Name,
            Email = contact.Email,
            Role = contact.Role
        };

        var result = await sender.Send(input, cancellationToken);

        return result.Match<ActionResult<OrganisationContactDto>>(
            c => new OrganisationContactDto(
                c.Id.Value,
                c.OrganisationId.Value,
                c.Name,
                c.Email,
                c.Role),
            error => error.ToObjectResult());
    }

    [HttpDelete("{contactId:guid}")]
    public async Task<ActionResult> Delete(
        [FromRoute] Guid organisationId,
        [FromRoute] Guid contactId,
        CancellationToken cancellationToken)
    {
        var userId = permissionsService.GetUserId();
        return await userId.MatchAsync(
            async uId =>
            {
                var input = new DeleteOrganisationContactCommand
                {
                    Id = contactId,
                    DeletedByUserId = uId
                };

                var result = await sender.Send(input, cancellationToken);

                return result.Match<ActionResult>(
                    _ => NoContent(),
                    error => error.ToObjectResult());
            },
            e => Unauthorized(e.Message));
    }
}