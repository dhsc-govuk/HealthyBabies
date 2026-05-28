using Api.Modules.Errors;
using Application.Common;
using Application.Common.Interfaces.Queries;
using Application.DataCollections.Commands;
using Application.DataCollections.Dtos;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("data-collection-form-questions")]
public class DataCollectionFormQuestionsController(
    IFormFieldQueries fieldQueries,
    IDataCollectionFormModuleQueries moduleQueries,
    ISender sender)
    : ControllerBase
{
    [HttpGet("modules")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<IReadOnlyList<DataCollectionFormModuleDto>>> GetAllModules(
        CancellationToken cancellationToken)
    {
        var modules = await moduleQueries.GetAllActiveAsync(cancellationToken);
        return modules.Select(DataCollectionFormModuleDto.FromDomainModel).ToList();
    }

    [HttpGet("modules/{moduleId:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<DataCollectionFormModuleWithFieldsDto>> GetModuleWithFields(
        [FromRoute] Guid moduleId,
        CancellationToken cancellationToken)
    {
        var module = await moduleQueries.GetByIdWithFieldsAsync(
            DataCollectionFormModuleId.From(moduleId),
            cancellationToken);

        return module.Match<ActionResult<DataCollectionFormModuleWithFieldsDto>>(
            m => DataCollectionFormModuleWithFieldsDto.FromDomainModel(m),
            NotFound());
    }

    [HttpGet("modules/code/{moduleCode}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<DataCollectionFormModuleWithFieldsDto>> GetModuleByCodeWithFields(
        [FromRoute] string moduleCode,
        CancellationToken cancellationToken)
    {
        var module = await moduleQueries.GetByCodeWithFieldsAsync(moduleCode, cancellationToken);

        return module.Match<ActionResult<DataCollectionFormModuleWithFieldsDto>>(
            m => DataCollectionFormModuleWithFieldsDto.FromDomainModel(m),
            NotFound());
    }

    [HttpGet]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<IReadOnlyList<FormFieldDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var fields = await fieldQueries.GetAll(cancellationToken);
        return fields.Select(FormFieldDto.FromDomainModel).ToList();
    }

    [HttpGet("module/{moduleId:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<IReadOnlyList<FormFieldDto>>> GetByModuleId(
        [FromRoute] Guid moduleId,
        CancellationToken cancellationToken)
    {
        var fields = await fieldQueries.GetByModuleId(
            DataCollectionFormModuleId.From(moduleId),
            cancellationToken);
        return fields.Select(FormFieldDto.FromDomainModel).ToList();
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<FormFieldDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var field = await fieldQueries.GetById(FormFieldId.From(id), cancellationToken);

        return field.Match<ActionResult<FormFieldDto>>(
            f => FormFieldDto.FromDomainModel(f),
            NotFound());
    }

    [HttpPost]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<FormFieldDto>> Create(
        [FromBody] CreateFormFieldDto dto,
        CancellationToken cancellationToken)
    {
        var command = new CreateFormFieldCommand
        {
            FormModuleId = dto.FormModuleId,
            FormSectionId = dto.FormSectionId,
            FieldKey = dto.FieldKey,
            Label = Utility.HtmlDecodeField(dto.Label),
            FieldType = dto.FieldType,
            IsRequired = dto.IsRequired,
            Placeholder = Utility.HtmlDecodeNullableField(dto.Placeholder),
            HelpText = Utility.HtmlDecodeNullableField(dto.HelpText),
            DefaultValue = Utility.HtmlDecodeNullableField(dto.DefaultValue),
            ValidationRules = dto.ValidationRules,
            ConditionalRules = dto.ConditionalRules,
            Configuration = dto.Configuration,
            Options = dto.Options.Select(o => new FormFieldOptionInputDto(
                Utility.HtmlDecodeField(o.Value),
                Utility.HtmlDecodeField(o.Label),
                o.DisplayOrder,
                o.IsDefault)).ToList()
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<FormFieldDto>>(
            field => CreatedAtAction(
                nameof(GetById),
                new { id = field.Id.Value },
                FormFieldDto.FromDomainModel(field)),
            error => error.ToObjectResult());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<FormFieldDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateFormFieldDto dto,
        CancellationToken cancellationToken)
    {
        var command = new UpdateFormFieldCommand
        {
            Id = id,
            FormSectionId = dto.FormSectionId,
            Label = Utility.HtmlDecodeField(dto.Label),
            FieldType = dto.FieldType,
            DisplayOrder = dto.DisplayOrder,
            IsRequired = dto.IsRequired,
            IsActive = dto.IsActive,
            Placeholder = Utility.HtmlDecodeNullableField(dto.Placeholder),
            HelpText = Utility.HtmlDecodeNullableField(dto.HelpText),
            DefaultValue = Utility.HtmlDecodeNullableField(dto.DefaultValue),
            ValidationRules = dto.ValidationRules,
            ConditionalRules = dto.ConditionalRules,
            Configuration = dto.Configuration,
            Options = dto.Options.Select(o => new FormFieldOptionInputDto(
                Utility.HtmlDecodeField(o.Value),
                Utility.HtmlDecodeField(o.Label),
                o.DisplayOrder,
                o.IsDefault)).ToList()
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<FormFieldDto>>(
            field => FormFieldDto.FromDomainModel(field),
            error => error.ToObjectResult());
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteFormFieldCommand { Id = id };
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult>(
            _ => NoContent(),
            error => error.ToObjectResult());
    }

    [HttpPost("reorder")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult> Reorder(
        [FromBody] ReorderFormFieldsDto dto,
        CancellationToken cancellationToken)
    {
        var command = new ReorderFormFieldsCommand
        {
            FormModuleId = dto.FormModuleId,
            Fields = dto.Fields
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult>(
            _ => NoContent(),
            error => error.ToObjectResult());
    }

    // ==================== Module CRUD ====================

    [HttpPost("modules")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<DataCollectionFormModuleDto>> CreateModule(
        [FromBody] CreateFormModuleDto dto,
        CancellationToken cancellationToken)
    {
        var command = new CreateFormModuleCommand
        {
            Code = Utility.HtmlDecodeField(dto.Code),
            Name = Utility.HtmlDecodeField(dto.Name),
            Description = Utility.HtmlDecodeNullableField(dto.Description)
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<DataCollectionFormModuleDto>>(
            module => CreatedAtAction(
                nameof(GetModuleWithFields),
                new { moduleId = module.Id.Value },
                DataCollectionFormModuleDto.FromDomainModel(module)),
            error => error.ToObjectResult());
    }

    [HttpPut("modules/{moduleId:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<DataCollectionFormModuleDto>> UpdateModule(
        [FromRoute] Guid moduleId,
        [FromBody] UpdateFormModuleDto dto,
        CancellationToken cancellationToken)
    {
        var command = new UpdateFormModuleCommand
        {
            Id = moduleId,
            Name = Utility.HtmlDecodeField(dto.Name),
            Description = Utility.HtmlDecodeNullableField(dto.Description),
            IsActive = dto.IsActive
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<DataCollectionFormModuleDto>>(
            module => DataCollectionFormModuleDto.FromDomainModel(module),
            error => error.ToObjectResult());
    }

    [HttpDelete("modules/{moduleId:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult> DeleteModule(
        [FromRoute] Guid moduleId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteFormModuleCommand(moduleId);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult>(
            _ => NoContent(),
            error => error.ToObjectResult());
    }

    // ==================== Section CRUD ====================

    [HttpPost("modules/{moduleId:guid}/sections")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<FormSectionDto>> CreateSection(
        [FromRoute] Guid moduleId,
        [FromBody] CreateFormSectionDto dto,
        CancellationToken cancellationToken)
    {
        var command = new CreateFormSectionCommand
        {
            FormModuleId = moduleId,
            Title = Utility.HtmlDecodeField(dto.Title),
            Description = Utility.HtmlDecodeNullableField(dto.Description),
            HelpText = Utility.HtmlDecodeNullableField(dto.HelpText),
            HelpUrl = Utility.HtmlDecodeNullableField(dto.HelpUrl)
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<FormSectionDto>>(
            section => CreatedAtAction(
                nameof(GetModuleWithFields),
                new { moduleId },
                FormSectionDto.FromDomainModel(section)),
            error => error.ToObjectResult());
    }

    [HttpPut("modules/{moduleId:guid}/sections/{sectionId:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<FormSectionDto>> UpdateSection(
        [FromRoute] Guid moduleId,
        [FromRoute] Guid sectionId,
        [FromBody] UpdateFormSectionDto dto,
        CancellationToken cancellationToken)
    {
        var command = new UpdateFormSectionCommand
        {
            FormModuleId = moduleId,
            SectionId = sectionId,
            Title = Utility.HtmlDecodeField(dto.Title),
            Description = Utility.HtmlDecodeNullableField(dto.Description),
            HelpText = Utility.HtmlDecodeNullableField(dto.HelpText),
            HelpUrl = Utility.HtmlDecodeNullableField(dto.HelpUrl)
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<FormSectionDto>>(
            section => FormSectionDto.FromDomainModel(section),
            error => error.ToObjectResult());
    }

    [HttpDelete("modules/{moduleId:guid}/sections/{sectionId:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult> DeleteSection(
        [FromRoute] Guid moduleId,
        [FromRoute] Guid sectionId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteFormSectionCommand
        {
            FormModuleId = moduleId,
            SectionId = sectionId
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult>(
            _ => NoContent(),
            error => error.ToObjectResult());
    }
}