using Api.Services.Abstract;
using Application.Common;
using Application.Common.FileValidation;
using Application.Common.Permissions;
using Application.DataCollections.Dtos;
using Application.Submissions.Commands;
using Application.Submissions.Dtos;
using Domain.DataCollections;
using Domain.ValueObjects;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("delivery-plan/submissions")]
[ApiController]
[Authorize(Roles = Role.DeliveryPlanLead)]
public class DeliveryPlanSubmissionsController(
    PermissionsService permissionsService,
    ISubmissionsControllerService controllerService,
    ISubmissionsBulkUploadService bulkUploadService,
    ISubmissionExportService submissionExportService,
    IDeliveryPlanReviewService reviewService,
    IMediator mediator,
    IFileValidationService fileValidationService)
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

    [HttpGet("{submissionId:guid}/versions")]
    public async Task<ActionResult<IReadOnlyList<SubmissionVersionSummaryDto>>> GetSubmissionVersions(
        [FromRoute] Guid submissionId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<IReadOnlyList<SubmissionVersionSummaryDto>>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(orgId => orgId.Value, () => Guid.Empty);
                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.GetSubmissionVersions(organisationId, submissionId, cancellationToken);
                return result.ToList();
            },
            e => Unauthorized(e.Message));
    }

    [HttpGet("{submissionId:guid}/versions/{versionNumber:int}")]
    public async Task<ActionResult<SubmissionVersionDetailDto>> GetSubmissionVersionDetail(
        [FromRoute] Guid submissionId,
        [FromRoute] int versionNumber,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<SubmissionVersionDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(orgId => orgId.Value, () => Guid.Empty);
                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.GetSubmissionVersionDetail(organisationId, submissionId, versionNumber, cancellationToken);
                return result.Match<ActionResult<SubmissionVersionDetailDto>>(r => r, NotFound());
            },
            e => Unauthorized(e.Message));
    }

    [HttpGet("{submissionId:guid}/versions/{versionNumber:int}/sections/{formModuleId:guid}")]
    public async Task<ActionResult<SubmissionVersionSectionDetailDto>> GetSubmissionVersionSection(
        [FromRoute] Guid submissionId,
        [FromRoute] int versionNumber,
        [FromRoute] Guid formModuleId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<SubmissionVersionSectionDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(orgId => orgId.Value, () => Guid.Empty);
                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.GetSubmissionVersionSection(organisationId, submissionId, versionNumber, formModuleId, cancellationToken);
                return result.Match<ActionResult<SubmissionVersionSectionDetailDto>>(r => r, NotFound());
            },
            e => Unauthorized(e.Message));
    }

    [HttpGet("{submissionId:guid}/versions/{versionNumber:int}/sections/{formModuleId:guid}/activities/{activityId:guid}")]
    public async Task<ActionResult<SubmissionVersionActivityDetailDto>> GetSubmissionVersionActivity(
        [FromRoute] Guid submissionId,
        [FromRoute] int versionNumber,
        [FromRoute] Guid formModuleId,
        [FromRoute] Guid activityId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<SubmissionVersionActivityDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(orgId => orgId.Value, () => Guid.Empty);
                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.GetSubmissionVersionActivity(organisationId, submissionId, versionNumber, formModuleId, activityId, cancellationToken);
                return result.Match<ActionResult<SubmissionVersionActivityDetailDto>>(r => r, NotFound());
            },
            e => Unauthorized(e.Message));
    }

    [HttpGet("{submissionId:guid}/versions/{versionNumber:int}/download")]
    public async Task<ActionResult> DownloadSubmissionVersion(
        [FromRoute] Guid submissionId,
        [FromRoute] int versionNumber,
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

                var zipResult = await controllerService.GetSubmissionVersionZip(organisationId, submissionId, versionNumber, cancellationToken);
                return zipResult.Match<ActionResult>(
                    bytes => File(bytes, "application/zip", $"submission-version-{versionNumber}.zip"),
                    NotFound());
            },
            e => Task.FromResult<ActionResult>(Unauthorized(e.Message)));
    }

    // Read-only view of the caller's own submitted Delivery Plan, once accepted - the same
    // AnswerGroups/Activities shape DeliveryPlanReviewController gives admins, scoped to the
    // caller's own organisation rather than an admin-supplied local authority id.
    [HttpGet("{submissionId:guid}/review/sections/{formModuleId:guid}")]
    public async Task<ActionResult<ReviewSectionDetailDto>> GetReviewSection(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid formModuleId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<ReviewSectionDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(orgId => orgId.Value, () => Guid.Empty);
                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await reviewService.GetSection(submissionId, organisationId, formModuleId, cancellationToken);
                return result.Match<ActionResult<ReviewSectionDetailDto>>(r => r, NotFound());
            },
            e => Unauthorized(e.Message));
    }

    [HttpGet("{submissionId:guid}/review/sections/{formModuleId:guid}/activities/{activityId:guid}")]
    public async Task<ActionResult<ReviewActivityDetailDto>> GetReviewActivity(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid formModuleId,
        [FromRoute] Guid activityId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<ReviewActivityDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(orgId => orgId.Value, () => Guid.Empty);
                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await reviewService.GetActivity(submissionId, organisationId, formModuleId, activityId, cancellationToken);
                return result.Match<ActionResult<ReviewActivityDetailDto>>(r => r, NotFound());
            },
            e => Unauthorized(e.Message));
    }

    // Download of the caller's own current (live) submission - separate from the version
    // download above, which serves a frozen historical snapshot.
    [HttpGet("{submissionId:guid}/download")]
    public async Task<ActionResult> DownloadSubmission(
        [FromRoute] Guid submissionId,
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

                var zipResult = await submissionExportService.ExportSubmissionAsZipAsync(submissionId, organisationId, cancellationToken);
                return zipResult.Match<ActionResult>(
                    bytes => File(bytes, "application/zip", $"submission_{submissionId}.zip"),
                    error => BadRequest(error));
            },
            e => Task.FromResult<ActionResult>(Unauthorized(e.Message)));
    }

    [HttpGet("{submissionId:guid}/modules/{moduleId:guid}/bulk-upload/template")]
    public async Task<ActionResult> DownloadBulkUploadTemplate(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
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
                    return Unauthorized("Organisation ID is required");
                }

                // Note: submissionId in the route is actually the DataCollection.Id (dataCollectionId)
                // This matches the pattern used by SubmissionsController
                var dataCollectionId = submissionId;

                if (format.ToLowerInvariant() == "xlsx")
                {
                    var excelBytes = await bulkUploadService.GenerateExcelTemplateAsync(organisationId, moduleId, submissionId, dataCollectionId, cancellationToken);
                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "bulk_upload_template.xlsx");
                }

                var csvBytes = await bulkUploadService.GenerateCsvTemplateAsync(organisationId, moduleId, submissionId, dataCollectionId, cancellationToken);
                return File(csvBytes, "text/csv", "bulk_upload_template.csv");
            },
            e => Task.FromResult<ActionResult>(Unauthorized(e.Message)));
    }

    [HttpPost("{submissionId:guid}/modules/{moduleId:guid}/bulk-upload/validate")]
    public async Task<ActionResult<BulkUploadValidationResultDto>> ValidateBulkUpload(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateBulkUploadFile(file, cancellationToken);
        return await validation.MatchAsync<ActionResult<BulkUploadValidationResultDto>>(
            async validated =>
            {
                await using var content = validated.Content;
                var command = new ValidateBulkUploadCommand
                {
                    SubmissionId = submissionId,
                    ModuleId = moduleId,
                    FileStream = content,
                    FileName = validated.OriginalFileName
                };

                var result = await mediator.Send(command, cancellationToken);
                return result.Match<ActionResult<BulkUploadValidationResultDto>>(
                    success => Ok(success),
                    error => BadRequest(error));
            },
            error => BadRequest(error.Message));
    }

    private Task<Either<FileValidationError, ValidatedFile>> ValidateBulkUploadFile(
        IFormFile file,
        CancellationToken cancellationToken) =>
        fileValidationService.ValidateBulkUploadFileAsync(file, FileUploadProfile.BulkUploadCsv, cancellationToken);

    [HttpPost("{submissionId:guid}/modules/{moduleId:guid}/bulk-upload/check-activity-duplicates")]
    public async Task<ActionResult<List<ActivityDuplicateCheckDto>>> CheckActivityDuplicates(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        [FromBody] CheckActivityDuplicatesRequestDto request,
        CancellationToken cancellationToken)
    {
        var query = new CheckActivityDuplicatesQuery
        {
            SubmissionId = submissionId,
            ModuleId = moduleId,
            Rows = request.Rows,
        };

        var result = await mediator.Send(query, cancellationToken);
        return result.Match<ActionResult<List<ActivityDuplicateCheckDto>>>(
            success => Ok(success),
            error => BadRequest(error));
    }

    [HttpPost("{submissionId:guid}/modules/{moduleId:guid}/bulk-upload/staged")]
    public async Task<ActionResult<BulkUploadResultDto>> ProcessStagedBulkUpload(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        [FromBody] ProcessStagedBulkUploadRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new ProcessStagedBulkUploadCommand
        {
            SubmissionId = submissionId,
            ModuleId = moduleId,
            StagingId = request.StagingId,
            SelectedRowIndices = request.SelectedRowIndices,
            CellEdits = request.CellEdits.Select(e => new BulkUploadCellEdit(e.RowIndex, e.ColumnIndex, Utility.HtmlDecodeField(e.Value))).ToList(),
        };

        var result = await mediator.Send(command, cancellationToken);
        return result.Match<ActionResult<BulkUploadResultDto>>(
            success => Ok(success),
            error => BadRequest(error));
    }
}