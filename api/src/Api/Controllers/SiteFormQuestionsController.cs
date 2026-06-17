using Api.Modules.Errors;
using Application.Common;
using Application.Common.Interfaces.Queries;
using Application.SiteForms.Commands;
using Application.SiteForms.Dtos;
using Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("site-form-questions")]
public class SiteFormQuestionsController(
    ISiteFormQuestionQueries questionQueries,
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{Role.OrganisationAdmin},{Role.Admin}")]
    public async Task<ActionResult<IReadOnlyList<SiteFormQuestionDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var questions = await questionQueries.GetAllActive(cancellationToken);
        return questions.Select(SiteFormQuestionDto.FromDomainModel).ToList();
    }

    [HttpGet("all")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<IReadOnlyList<SiteFormQuestionDto>>> GetAllIncludingInactive(
        CancellationToken cancellationToken)
    {
        var questions = await questionQueries.GetAll(cancellationToken);
        return questions.Select(SiteFormQuestionDto.FromDomainModel).ToList();
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<SiteFormQuestionDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var question = await questionQueries.GetById(
            new Domain.SiteForms.SiteFormQuestionId(id),
            cancellationToken);

        return question.Match<ActionResult<SiteFormQuestionDto>>(
            q => SiteFormQuestionDto.FromDomainModel(q),
            NotFound());
    }

    [HttpPost]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<SiteFormQuestionDto>> Create(
        [FromBody] CreateSiteFormQuestionDto dto,
        CancellationToken cancellationToken)
    {
        var command = new CreateSiteFormQuestionCommand
        {
            Code = dto.Code,
            Label = Utility.HtmlDecodeField(dto.Label),
            Hint = Utility.HtmlDecodeNullableField(dto.Hint),
            Placeholder = Utility.HtmlDecodeNullableField(dto.Placeholder),
            QuestionType = dto.QuestionType,
            IsRequired = dto.IsRequired,
            HelpTextSummary = Utility.HtmlDecodeNullableField(dto.HelpTextSummary),
            HelpText = Utility.HtmlDecodeNullableField(dto.HelpText),
            ConditionalQuestionCode = dto.ConditionalQuestionCode,
            ConditionalValue = Utility.HtmlDecodeNullableField(dto.ConditionalValue),
            Options = dto.Options.Select(o => new SiteFormQuestionOptionInputDto(
                Utility.HtmlDecodeField(o.Value),
                Utility.HtmlDecodeField(o.Label),
                o.DisplayOrder)).ToList()
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<SiteFormQuestionDto>>(
            question => CreatedAtAction(
                nameof(GetById),
                new { id = question.Id.Value },
                SiteFormQuestionDto.FromDomainModel(question)),
            error => error.ToObjectResult());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<SiteFormQuestionDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateSiteFormQuestionDto dto,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSiteFormQuestionCommand
        {
            Id = id,
            Label = Utility.HtmlDecodeField(dto.Label),
            Hint = Utility.HtmlDecodeNullableField(dto.Hint),
            Placeholder = Utility.HtmlDecodeNullableField(dto.Placeholder),
            QuestionType = dto.QuestionType,
            DisplayOrder = dto.DisplayOrder,
            IsRequired = dto.IsRequired,
            IsActive = dto.IsActive,
            HelpTextSummary = Utility.HtmlDecodeNullableField(dto.HelpTextSummary),
            HelpText = Utility.HtmlDecodeNullableField(dto.HelpText),
            ConditionalQuestionCode = dto.ConditionalQuestionCode,
            ConditionalValue = Utility.HtmlDecodeNullableField(dto.ConditionalValue),
            Options = dto.Options.Select(o => new SiteFormQuestionOptionInputDto(
                Utility.HtmlDecodeField(o.Value),
                Utility.HtmlDecodeField(o.Label),
                o.DisplayOrder)).ToList()
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<SiteFormQuestionDto>>(
            question => SiteFormQuestionDto.FromDomainModel(question),
            error => error.ToObjectResult());
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteSiteFormQuestionCommand { Id = id };
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult>(
            _ => NoContent(),
            error => error.ToObjectResult());
    }

    [HttpPost("reorder")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult> Reorder(
        [FromBody] ReorderSiteFormQuestionsDto dto,
        CancellationToken cancellationToken)
    {
        var command = new ReorderSiteFormQuestionsCommand
        {
            Questions = dto.Questions
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult>(
            _ => NoContent(),
            error => error.ToObjectResult());
    }
}