using Api.Services.Abstract;
using Application.Common;
using Application.Common.FileValidation;
using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using Application.Submissions.Commands;
using Application.Submissions.Dtos;
using Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("organisation-admin/submissions")]
[ApiController]
[Authorize(Roles = Role.OrganisationAdmin)]
public class SubmissionsController(
    PermissionsService permissionsService,
    ISubmissionsControllerService controllerService,
    ISubmissionsBulkUploadService bulkUploadService,
    IDataCollectionSubmissionRepository dataCollectionSubmissionRepository,
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

                var result = await controllerService.GetByOrganisationId(organisationId, cancellationToken);
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

    [HttpGet("{submissionId:guid}/sections/{sectionId:guid}")]
    public async Task<ActionResult<SectionDetailDto>> GetSection(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid sectionId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<SectionDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId.Value,
                    () => Guid.Empty);

                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.GetSectionById(organisationId, submissionId, sectionId, cancellationToken);
                return result.Match<ActionResult<SectionDetailDto>>(
                    r => r,
                    NotFound());
            },
            e => Unauthorized(e.Message));
    }

    [HttpPost("{submissionId:guid}/sections/{sectionId:guid}")]
    public async Task<ActionResult<SectionDetailDto>> SaveSection(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid sectionId,
        [FromBody] SaveSectionRequest request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<SectionDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId.Value,
                    () => Guid.Empty);

                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.SaveSection(organisationId, submissionId, sectionId, request, cancellationToken);
                return result.Match<ActionResult<SectionDetailDto>>(
                    r => r,
                    error => BadRequest(error));
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

    [HttpDelete("{submissionId:guid}/modules/{moduleId:guid}")]
    public async Task<ActionResult> DeleteFormModule(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        CancellationToken cancellationToken)
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

                var result = await controllerService.DeleteFormModule(organisationId, submissionId, moduleId, cancellationToken);
                return result.Match<ActionResult>(
                    _ => NoContent(),
                    error => BadRequest(error));
            },
            e => Unauthorized(e.Message));
    }

    [HttpPost("{submissionId:guid}/modules/{moduleId:guid}/upload")]
    public async Task<ActionResult<FileUploadResultDto>> UploadFile(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required");
        }

        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<FileUploadResultDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId.Value,
                    () => Guid.Empty);

                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.UploadFile(organisationId, submissionId, moduleId, file, cancellationToken);
                return result.Match<ActionResult<FileUploadResultDto>>(
                    r => r,
                    error => BadRequest(error));
            },
            e => Unauthorized(e.Message));
    }

    [HttpGet("{submissionId:guid}/modules/{moduleId:guid}/services")]
    public async Task<ActionResult<ServiceUsersModuleDetailDto>> GetServiceUsersModule(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<ServiceUsersModuleDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId.Value,
                    () => Guid.Empty);

                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.GetServiceUsersModule(organisationId, submissionId, moduleId, cancellationToken);
                return result.Match<ActionResult<ServiceUsersModuleDetailDto>>(
                    r => r,
                    NotFound());
            },
            e => Unauthorized(e.Message));
    }

    [HttpGet("{submissionId:guid}/modules/{moduleId:guid}/services/{serviceId:guid}")]
    public async Task<ActionResult<ServiceFormDetailDto>> GetServiceForm(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        [FromRoute] Guid serviceId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<ServiceFormDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId.Value,
                    () => Guid.Empty);

                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.GetServiceForm(organisationId, submissionId, moduleId, serviceId, cancellationToken);
                return result.Match<ActionResult<ServiceFormDetailDto>>(
                    r => r,
                    NotFound());
            },
            e => Unauthorized(e.Message));
    }

    [HttpPost("{submissionId:guid}/modules/{moduleId:guid}/services/{serviceId:guid}")]
    public async Task<ActionResult<ServiceFormDetailDto>> SaveServiceForm(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        [FromRoute] Guid serviceId,
        [FromBody] SaveServiceFormRequest request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<ServiceFormDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId.Value,
                    () => Guid.Empty);

                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.SaveServiceForm(organisationId, submissionId, moduleId, serviceId, request, cancellationToken);
                return result.Match<ActionResult<ServiceFormDetailDto>>(
                    r => r,
                    error => BadRequest(error));
            },
            e => Task.FromResult<ActionResult<ServiceFormDetailDto>>(Unauthorized(e.Message)));
    }

    [HttpGet("{submissionId:guid}/modules/{moduleId:guid}/wider-services")]
    public async Task<ActionResult<WiderServiceUsersModuleDetailDto>> GetWiderServiceUsersModule(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<WiderServiceUsersModuleDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId.Value,
                    () => Guid.Empty);

                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.GetWiderServiceUsersModule(organisationId, submissionId, moduleId, cancellationToken);
                return result.Match<ActionResult<WiderServiceUsersModuleDetailDto>>(
                    r => r,
                    NotFound());
            },
            e => Unauthorized(e.Message));
    }

    [HttpGet("{submissionId:guid}/modules/{moduleId:guid}/wider-services/{categoryId:guid}")]
    public async Task<ActionResult<WiderServiceCategoryFormDto>> GetWiderServiceCategoryForm(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        [FromRoute] Guid categoryId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<WiderServiceCategoryFormDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId.Value,
                    () => Guid.Empty);

                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.GetWiderServiceCategoryForm(organisationId, submissionId, moduleId, categoryId, cancellationToken);
                return result.Match<ActionResult<WiderServiceCategoryFormDto>>(
                    r => r,
                    NotFound());
            },
            e => Unauthorized(e.Message));
    }

    [HttpPost("{submissionId:guid}/modules/{moduleId:guid}/wider-services/{categoryId:guid}")]
    public async Task<ActionResult<WiderServiceCategoryFormDto>> SaveWiderServiceCategoryForm(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        [FromRoute] Guid categoryId,
        [FromBody] SaveWiderServiceCategoryFormRequest request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<WiderServiceCategoryFormDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId.Value,
                    () => Guid.Empty);

                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.SaveWiderServiceCategoryForm(organisationId, submissionId, moduleId, categoryId, request, cancellationToken);
                return result.Match<ActionResult<WiderServiceCategoryFormDto>>(
                    r => r,
                    error => BadRequest(error));
            },
            e => Unauthorized(e.Message));
    }

    [HttpGet("{submissionId:guid}/modules/{moduleId:guid}/outcome-scores")]
    public async Task<ActionResult<OutcomeScoresModuleDetailDto>> GetOutcomeScoresModule(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<OutcomeScoresModuleDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId.Value,
                    () => Guid.Empty);

                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.GetOutcomeScoresModule(organisationId, submissionId, moduleId, cancellationToken);
                return result.Match<ActionResult<OutcomeScoresModuleDetailDto>>(
                    r => r,
                    NotFound());
            },
            e => Unauthorized(e.Message));
    }

    [HttpPost("{submissionId:guid}/modules/{moduleId:guid}/outcome-scores")]
    public async Task<ActionResult<OutcomeScoreFormDetailDto>> CreateOutcomeScoreRecord(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<OutcomeScoreFormDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId.Value,
                    () => Guid.Empty);

                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.CreateOutcomeScoreRecord(organisationId, submissionId, moduleId, cancellationToken);
                return result.Match<ActionResult<OutcomeScoreFormDetailDto>>(
                    r => r,
                    error => BadRequest(error));
            },
            e => Unauthorized(e.Message));
    }

    [HttpGet("{submissionId:guid}/modules/{moduleId:guid}/outcome-scores/{recordId:guid}")]
    public async Task<ActionResult<OutcomeScoreFormDetailDto>> GetOutcomeScoreRecord(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        [FromRoute] Guid recordId,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<OutcomeScoreFormDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId.Value,
                    () => Guid.Empty);

                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.GetOutcomeScoreRecord(organisationId, submissionId, moduleId, recordId, cancellationToken);
                return result.Match<ActionResult<OutcomeScoreFormDetailDto>>(
                    r => r,
                    NotFound());
            },
            e => Unauthorized(e.Message));
    }

    [HttpPost("{submissionId:guid}/modules/{moduleId:guid}/outcome-scores/{recordId:guid}")]
    public async Task<ActionResult<OutcomeScoreFormDetailDto>> SaveOutcomeScoreRecord(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        [FromRoute] Guid recordId,
        [FromBody] SaveOutcomeScoreFormRequest request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();
        return await permissions.MatchAsync<ActionResult<OutcomeScoreFormDetailDto>>(
            async p =>
            {
                var organisationId = p.OrganisationId.Match(
                    orgId => orgId.Value,
                    () => Guid.Empty);

                if (organisationId == Guid.Empty)
                {
                    return Unauthorized("Organisation ID is required");
                }

                var result = await controllerService.SaveOutcomeScoreRecord(organisationId, submissionId, moduleId, recordId, request, cancellationToken);
                return result.Match<ActionResult<OutcomeScoreFormDetailDto>>(
                    r => r,
                    error => BadRequest(error));
            },
            e => Unauthorized(e.Message));
    }

    [HttpDelete("{submissionId:guid}/modules/{moduleId:guid}/outcome-scores/{recordId:guid}")]
    public async Task<ActionResult> DeleteOutcomeScoreRecord(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        [FromRoute] Guid recordId,
        CancellationToken cancellationToken)
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

                var result = await controllerService.DeleteOutcomeScoreRecord(organisationId, submissionId, moduleId, recordId, cancellationToken);
                return result.Match<ActionResult>(
                    _ => Ok(new { message = "Record deleted successfully" }),
                    error => BadRequest(error));
            },
            e => Unauthorized(e.Message));
    }

    [HttpDelete("purge-all")]
    public async Task<ActionResult> PurgeAllSubmissions(CancellationToken cancellationToken)
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

                var result = await controllerService.PurgeAllSubmissions(organisationId, cancellationToken);
                return result.Match<ActionResult>(
                    count => Ok(new { message = $"Deleted {count} form submissions" }),
                    error => BadRequest(error));
            },
            e => Task.FromResult<ActionResult>(Unauthorized(e.Message)));
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
                // This matches the pattern used by other endpoints in this controller
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
        var validation = await fileValidationService.ValidateAsync(file, FileUploadProfile.BulkUploadCsv, cancellationToken);
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

    [HttpPost("{submissionId:guid}/modules/{moduleId:guid}/bulk-upload")]
    [Obsolete("Use /bulk-upload/staged with the stagingId returned from /bulk-upload/validate. This endpoint is kept for backward compatibility only.")]
    public async Task<ActionResult<BulkUploadResultDto>> ProcessBulkUpload(
        [FromRoute] Guid submissionId,
        [FromRoute] Guid moduleId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        var validation = await fileValidationService.ValidateAsync(file, FileUploadProfile.BulkUploadCsv, cancellationToken);
        return await validation.MatchAsync<ActionResult<BulkUploadResultDto>>(
            async validated =>
            {
                await using var content = validated.Content;
                var command = new ProcessBulkUploadCommand
                {
                    SubmissionId = submissionId,
                    ModuleId = moduleId,
                    FileStream = content,
                    FileName = validated.OriginalFileName
                };

                var result = await mediator.Send(command, cancellationToken);
                return result.Match<ActionResult<BulkUploadResultDto>>(
                    success => Ok(success),
                    error => BadRequest(error));
            },
            error => BadRequest(error.Message));
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
            SelectedServiceNames = request.SelectedServiceNames,
            CellEdits = request.CellEdits.Select(e => new BulkUploadCellEdit(e.RowIndex, e.ColumnIndex, Utility.HtmlDecodeField(e.Value))).ToList(),
        };

        var result = await mediator.Send(command, cancellationToken);
        return result.Match<ActionResult<BulkUploadResultDto>>(
            success => Ok(success),
            error => BadRequest(error));
    }
}