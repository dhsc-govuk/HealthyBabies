using Api.Modules.Errors;
using Api.Services.Abstract;
using Application.Common;
using Application.Common.Permissions;
using Application.Users.Commands.OrganisationUsers;
using Application.Users.Dtos;
using Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("organisations/{organisationId:guid}/users")]
[ApiController]
[Authorize(Roles = $"{Role.Admin},{Role.OrganisationAdmin}")]
public class OrganisationUsersController(
    PermissionsService permissionsService,
    ISender sender,
    IOrganisationUsersControllerService controllerService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrganisationUserDto>>> GetAll(
        [FromRoute] Guid organisationId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<IReadOnlyList<OrganisationUserDto>>>(
            async p =>
            {
                var result = await controllerService.GetAll(p, organisationId, cancellationToken);

                return result.ToList();
            },
            e => Unauthorized(e.Message));
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<OrganisationUserDto>> Get(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync(
            async p =>
            {
                var result = await controllerService.Get(p, userId, cancellationToken);

                return result.Match<ActionResult<OrganisationUserDto>>(
                    r => r,
                    NotFound());
            },
            e => Unauthorized(e.Message));
    }

    [HttpPost("create")]
    public async Task<ActionResult<OrganisationUserDto>> Create(
        [FromBody] OrganisationUserDto user,
        [FromRoute] Guid organisationId,
        CancellationToken cancellationToken)
    {
        var input = new CreateOrganisationUserCommand
        {
            FirstName = Utility.HtmlDecodeField(user.FirstName),
            LastName = Utility.HtmlDecodeField(user.LastName),
            Email = user.Email,
            IsActive = user.IsActive,
            OrganisationId = organisationId,
            Role = UserRole.From(user.Role)
        };

        var result = await sender.Send(input, cancellationToken);
        return result.Match<ActionResult<OrganisationUserDto>>(
            r => new OrganisationUserDto(
                r.OrganisationUser.Id.ToString(),
                r.OrganisationUser.User!.Name.FirstName,
                r.OrganisationUser.User.Name.LastName,
                r.OrganisationUser.User.Email,
                r.OrganisationUser.User.IsActive,
                r.OrganisationUser.User.Role,
                TemporaryPassword: r.TemporaryPassword),
            error => error.ToObjectResult());
    }

    [HttpPut("edit")]
    public async Task<ActionResult<OrganisationUserDto>> Edit(
        [FromBody] OrganisationUserDto user,
        CancellationToken cancellationToken)
    {
        var input = new UpdateOrganisationUserCommand
        {
            Id = Guid.Parse(user.Id!),
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            IsActive = user.IsActive,
            Role = UserRole.From(user.Role)
        };

        var result = await sender.Send(input, cancellationToken);
        return result.Match<ActionResult<OrganisationUserDto>>(
            u => new OrganisationUserDto(
                u.Id.ToString(),
                u.User!.Name.FirstName,
                u.User.Name.LastName,
                u.User.Email,
                u.User.IsActive,
                u.User.Role),
            error => error.ToObjectResult());
    }

    private string GetOriginFromRequest() => $"{Request.Scheme}://{Request.Host.Value}{Request.PathBase.Value}";
}