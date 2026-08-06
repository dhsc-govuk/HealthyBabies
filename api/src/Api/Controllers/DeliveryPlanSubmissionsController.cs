using Api.Services.Abstract;
using Application.Common.Permissions;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("delivery-plan/submissions")]
[ApiController]
[Authorize(Roles = Role.DeliveryPlanLead)]
public class DeliveryPlanSubmissionsController(
    PermissionsService permissionsService,
    ISubmissionsControllerService controllerService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubmissionDto>>> GetAll(CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<IReadOnlyList<SubmissionDto>>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId.Value,
                    () => Guid.Empty);

                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.GetByOrganisationIdAndType(
                    organisationId,
                    DataCollectionTypeConstants.DeliveryPlan,
                    cancellationToken);
                return result.ToList();
            },
            e => Unauthorized(e.Message));
    }

    [HttpGet("{submissionId:guid}")]
    public async Task<ActionResult<SubmissionDetailDto>> Get(
        [FromRoute] Guid submissionId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<SubmissionDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId.Value,
                    () => Guid.Empty);

                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.GetById(organisationId, submissionId, cancellationToken);
                return result.Match<ActionResult<SubmissionDetailDto>>(
                    r => r,
                    NotFound());
            },
            e => Unauthorized(e.Message));
    }

    [HttpGet("{submissionId:guid}/modules/{moduleId:guid}")]
    public async Task<ActionResult<FormModuleDetailDto>> GetFormModule(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<FormModuleDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId.Value,
                    () => Guid.Empty);

                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.GetFormModuleById(organisationId, submissionId, moduleId, cancellationToken);
                return result.Match<ActionResult<FormModuleDetailDto>>(
                    r => r,
                    NotFound());
            },
            e => Unauthorized(e.Message));
    }

    [HttpPost("{submissionId:guid}/modules/{moduleId:guid}")]
    public async Task<ActionResult<FormModuleDetailDto>> SaveFormModule(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        [FromBody] SaveFormModuleRequest request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<FormModuleDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId.Value,
                    () => Guid.Empty);

                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.SaveFormModule(organisationId, submissionId, moduleId, request, cancellationToken);
                return result.Match<ActionResult<FormModuleDetailDto>>(
                    r => r,
                    error => BadRequest(error));
            },
            e => Unauthorized(e.Message));
    }

    [HttpGet("{submissionId:guid}/modules/{moduleId:guid}/activities")]
    public async Task<ActionResult<ActivitiesModuleDetailDto>> GetActivitiesModule(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<ActivitiesModuleDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(orgId => orgId.Value, () => Guid.Empty);
                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.GetActivitiesModule(organisationId, submissionId, moduleId, cancellationToken);
                return result.Match<ActionResult<ActivitiesModuleDetailDto>>(r => r, NotFound());
            },
            e => Unauthorized(e.Message));
    }

    [HttpPost("{submissionId:guid}/modules/{moduleId:guid}/activities")]
    public async Task<ActionResult<ActivityFormDetailDto>> CreateActivity(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<ActivityFormDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(orgId => orgId.Value, () => Guid.Empty);
                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.CreateActivity(organisationId, submissionId, moduleId, cancellationToken);
                return result.Match<ActionResult<ActivityFormDetailDto>>(r => r, error => BadRequest(error));
            },
            e => Unauthorized(e.Message));
    }

    [HttpGet("{submissionId:guid}/modules/{moduleId:guid}/activities/new")]
    public async Task<ActionResult<ActivityFormDetailDto>> GetActivityTemplate(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<ActivityFormDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(orgId => orgId.Value, () => Guid.Empty);
                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.GetActivityTemplate(organisationId, submissionId, moduleId, cancellationToken);
                return result.Match<ActionResult<ActivityFormDetailDto>>(r => r, NotFound());
            },
            e => Unauthorized(e.Message));
    }

    [HttpGet("{submissionId:guid}/modules/{moduleId:guid}/activities/{activityId:guid}")]
    public async Task<ActionResult<ActivityFormDetailDto>> GetActivityForm(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        [FromRoute] Guid activityId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<ActivityFormDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(orgId => orgId.Value, () => Guid.Empty);
                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.GetActivityForm(organisationId, submissionId, moduleId, activityId, cancellationToken);
                return result.Match<ActionResult<ActivityFormDetailDto>>(r => r, NotFound());
            },
            e => Unauthorized(e.Message));
    }

    [HttpPost("{submissionId:guid}/modules/{moduleId:guid}/activities/{activityId:guid}")]
    public async Task<ActionResult<ActivityFormDetailDto>> SaveActivityForm(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        [FromRoute] Guid activityId,
        [FromBody] SaveActivityFormRequest request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<ActivityFormDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(orgId => orgId.Value, () => Guid.Empty);
                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.SaveActivityForm(organisationId, submissionId, moduleId, activityId, request, cancellationToken);
                return result.Match<ActionResult<ActivityFormDetailDto>>(r => r, error => BadRequest(error));
            },
            e => Unauthorized(e.Message));
    }

    [HttpDelete("{submissionId:guid}/modules/{moduleId:guid}/activities/{activityId:guid}")]
    public async Task<ActionResult> DeleteActivity(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        [FromRoute] Guid activityId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(orgId => orgId.Value, () => Guid.Empty);
                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.DeleteActivity(organisationId, submissionId, moduleId, activityId, cancellationToken);
                return result.Match<ActionResult>(
                    _ => NoContent(),
                    error => BadRequest(error));
            },
            e => Unauthorized(e.Message));
    }

    [HttpPost("{submissionId:guid}/submit")]
    public async Task<ActionResult<SubmissionDetailDto>> SubmitSubmission(
        [FromRoute] Guid submissionId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<SubmissionDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId.Value,
                    () => Guid.Empty);

                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var userId = p.UserId.Match(
                    uid => uid.Value,
                    () => Guid.Empty);

                if (userId == Guid.Empty)
                {
                    return Unauthorized("User ID is required");
                }

                var result = await controllerService.SubmitSubmission(organisationId, submissionId, userId, cancellationToken);
                return result.Match<ActionResult<SubmissionDetailDto>>(
                    r => r,
                    error => BadRequest(error));
            },
            e => Unauthorized(e.Message));
    }
}