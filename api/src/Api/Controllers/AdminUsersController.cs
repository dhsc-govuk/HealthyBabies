using Api.Modules.Errors;
using Api.Services.Abstract;
using Application.Common;
using Application.Mfa.Commands;
using Application.Mfa.Dtos;
using Application.Users.Commands.Admins;
using Application.Users.Commands.OrganisationUsers;
using Application.Users.Dtos;
using Domain.Users;
using Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("admin/users")]
[ApiController]
[Authorize(Roles = Role.Admin)]
public class AdminUsersController(
    ISender sender,
    IAdminUsersControllerService controllerService,
    ISubmissionsControllerService submissionsController) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await controllerService.GetAll(cancellationToken);

        return result.ToList();
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<AdminUserDto>> Get(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await controllerService.Get(userId, cancellationToken);

        return result.Match<ActionResult<AdminUserDto>>(
            r => r,
            NotFound());
    }

    [HttpPut("edit")]
    public async Task<ActionResult<AdminUserDto>> Edit(
        [FromBody] AdminUserDto userDto,
        CancellationToken cancellationToken)
    {
        var input = new UpdateAdminCommand
        {
            Id = Guid.Parse(userDto.Id!),
            FirstName = userDto.FirstName,
            LastName = userDto.LastName,
            Email = userDto.Email,
            IsActive = userDto.IsActive
        };

        var result = await sender.Send(input, cancellationToken);
        return result.Match<ActionResult<AdminUserDto>>(
            user => new AdminUserDto(
                user.Id.ToString(),
                user.Name.FirstName,
                user.Name.LastName,
                user.Email,
                user.IsActive),
            error => error.ToObjectResult());
    }

    [HttpPost("create")]
    public async Task<ActionResult<AdminUserDto>> Create(
        [FromBody] AdminUserDto userDto,
        CancellationToken cancellationToken)
    {
        var input = new CreateAdminCommand
        {
            FirstName = userDto.FirstName,
            LastName = userDto.LastName,
            Email = userDto.Email,
            IsActive = userDto.IsActive
        };

        var result = await sender.Send(input, cancellationToken);
        return result.Match<ActionResult<AdminUserDto>>(
            r => new AdminUserDto(
                r.User.Id.ToString(),
                r.User.Name.FirstName,
                r.User.Name.LastName,
                r.User.Email,
                r.User.IsActive,
                TemporaryPassword: r.TemporaryPassword),
            error => error.ToObjectResult());
    }

    [HttpGet("organisation-users")]
    public async Task<ActionResult<PaginatedResult<OrganisationUserDto>>> GetAll(
        [FromQuery] OrganisationUserQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await controllerService.GetOrganisationUsers(query, cancellationToken);

        return result;
    }

    [HttpGet("organisation-users/{userId:guid}")]
    public async Task<ActionResult<OrganisationUserDto>> GetOrganisationUser(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await controllerService.GetOrganisationUser(userId, cancellationToken);

        return result.Match<ActionResult<OrganisationUserDto>>(
            r => r,
            NotFound());
    }

    [HttpPost("organisation-users")]
    public async Task<ActionResult<OrganisationUserDto>> CreateOrganisationUser(
        [FromBody] OrganisationUserDto user,
        CancellationToken cancellationToken)
    {
        var input = new CreateOrganisationUserCommand
        {
            FirstName = Utility.HtmlDecodeField(user.FirstName),
            LastName = Utility.HtmlDecodeField(user.LastName),
            Email = user.Email,
            IsActive = user.IsActive,
            OrganisationId = Guid.Parse(user.OrganisationId!),
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

    [HttpPost("activate-or-deactivate-user")]
    public async Task<ActionResult<AdminUserDto>> ActivateOrDeactivateUser(
        [FromBody] ActivateOrDeactivateUserDto request,
        CancellationToken cancellationToken)
    {
        var input = new ActivateOrDeactivateUserCommand
        {
            Id = request.Id,
            IsActive = request.IsActive,
        };

        var result = await sender.Send(input, cancellationToken);

        return result.Match<ActionResult<AdminUserDto>>(
        user => new AdminUserDto(
                user.Id.ToString(),
                user.Name.FirstName,
                user.Name.LastName,
                user.Email,
                user.IsActive),
        error => error.ToObjectResult());
    }

    [HttpPost("{userId:guid}/require-mfa-setup")]
    public async Task<ActionResult> RequireMfaSetup(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var command = new AdminRequireMfaSetupCommand(new UserId(userId));
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult>(
            _ => NoContent(),
            error => error.ToObjectResult());
    }

    [HttpPost("{userId:guid}/enable-mfa")]
    public async Task<ActionResult> EnableMfa(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var command = new AdminEnableMfaCommand(new UserId(userId));
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult>(
            _ => NoContent(),
            error => error.ToObjectResult());
    }

    [HttpPost("{userId:guid}/disable-mfa")]
    public async Task<ActionResult> DisableMfa(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var command = new AdminDisableMfaCommand(new UserId(userId));
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult>(
            _ => NoContent(),
            error => error.ToObjectResult());
    }

    [HttpPost("{userId:guid}/reset-mfa")]
    public async Task<ActionResult> ResetMfa(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var command = new AdminResetMfaCommand(new UserId(userId));
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult>(
            _ => NoContent(),
            error => error.ToObjectResult());
    }

    [HttpGet("{userId:guid}/mfa-status")]
    public async Task<ActionResult<MfaStatusDto>> GetMfaStatus(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await controllerService.GetMfaStatus(userId, cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{userId:guid}")]
    public async Task<ActionResult> Delete(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteAdminCommand { Id = userId };
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult>(
            _ => NoContent(),
            error => error.ToObjectResult());
    }

    [HttpDelete("purge-submissions")]
    public async Task<IActionResult> FlushAllSubmissions(CancellationToken cancellationToken)
    {
        var result = await submissionsController.PurgeAllSubmissions(Guid.Empty, cancellationToken);

        return Ok(result);
    }
}