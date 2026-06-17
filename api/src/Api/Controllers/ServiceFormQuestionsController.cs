using Api.Modules.Errors;
using Application.Common;
using Application.Common.Interfaces.Queries;
using Application.ServiceForms.Commands;
using Application.ServiceForms.Dtos;
using Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("service-form-questions")]
public class ServiceFormQuestionsController(
    IServiceFormQuestionQueries questionQueries,
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{Role.OrganisationAdmin},{Role.Admin}")]
    public async Task<ActionResult<IReadOnlyList<ServiceFormQuestionDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var questions = await questionQueries.GetAllActive(cancellationToken);
        return questions.Select(ServiceFormQuestionDto.FromDomainModel).ToList();
    }

    [HttpGet("all")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<IReadOnlyList<ServiceFormQuestionDto>>> GetAllIncludingInactive(
        CancellationToken cancellationToken)
    {
        var questions = await questionQueries.GetAll(cancellationToken);
        return questions.Select(ServiceFormQuestionDto.FromDomainModel).ToList();
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<ServiceFormQuestionDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var question = await questionQueries.GetById(
            new Domain.ServiceForms.ServiceFormQuestionId(id),
            cancellationToken);

        return question.Match<ActionResult<ServiceFormQuestionDto>>(
            q => ServiceFormQuestionDto.FromDomainModel(q),
            NotFound());
    }

    [HttpGet("step/{step:int}")]
    [Authorize(Roles = $"{Role.OrganisationAdmin},{Role.Admin}")]
    public async Task<ActionResult<IReadOnlyList<ServiceFormQuestionDto>>> GetByStep(
        [FromRoute] int step,
        CancellationToken cancellationToken)
    {
        if (step < 1 || step > 2)
        {
            return BadRequest("Step must be 1 or 2");
        }

        var questions = await questionQueries.GetByStep(step, cancellationToken);
        return questions.Select(ServiceFormQuestionDto.FromDomainModel).ToList();
    }

    [HttpPost]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<ServiceFormQuestionDto>> Create(
        [FromBody] CreateServiceFormQuestionDto dto,
        CancellationToken cancellationToken)
    {
        var command = new CreateServiceFormQuestionCommand
        {
            Code = dto.Code,
            Label = Utility.HtmlDecodeField(dto.Label),
            Hint = Utility.HtmlDecodeNullableField(dto.Hint),
            Placeholder = Utility.HtmlDecodeNullableField(dto.Placeholder),
            QuestionType = dto.QuestionType,
            Step = dto.Step,
            IsRequired = dto.IsRequired,
            IsPredefined = dto.IsPredefined,
            HelpText = Utility.HtmlDecodeNullableField(dto.HelpText),
            ConditionalQuestionCode = dto.ConditionalQuestionCode,
            ConditionalValue = Utility.HtmlDecodeNullableField(dto.ConditionalValue),
            Options = dto.Options.Select(o => new ServiceFormQuestionOptionInputDto(
                Utility.HtmlDecodeField(o.Value),
                Utility.HtmlDecodeField(o.Label),
                o.DisplayOrder)).ToList()
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<ServiceFormQuestionDto>>(
            question => CreatedAtAction(
                nameof(GetById),
                new { id = question.Id.Value },
                ServiceFormQuestionDto.FromDomainModel(question)),
            error => error.ToObjectResult());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<ServiceFormQuestionDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateServiceFormQuestionDto dto,
        CancellationToken cancellationToken)
    {
        var command = new UpdateServiceFormQuestionCommand
        {
            Id = id,
            Label = Utility.HtmlDecodeField(dto.Label),
            Hint = Utility.HtmlDecodeNullableField(dto.Hint),
            Placeholder = Utility.HtmlDecodeNullableField(dto.Placeholder),
            QuestionType = dto.QuestionType,
            Step = dto.Step,
            DisplayOrder = dto.DisplayOrder,
            IsRequired = dto.IsRequired,
            IsPredefined = dto.IsPredefined,
            IsActive = dto.IsActive,
            HelpText = Utility.HtmlDecodeNullableField(dto.HelpText),
            ConditionalQuestionCode = dto.ConditionalQuestionCode,
            ConditionalValue = Utility.HtmlDecodeNullableField(dto.ConditionalValue),
            Options = dto.Options.Select(o => new ServiceFormQuestionOptionInputDto(
                Utility.HtmlDecodeField(o.Value),
                Utility.HtmlDecodeField(o.Label),
                o.DisplayOrder)).ToList()
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<ServiceFormQuestionDto>>(
            question => ServiceFormQuestionDto.FromDomainModel(question),
            error => error.ToObjectResult());
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteServiceFormQuestionCommand { Id = id };
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult>(
            _ => NoContent(),
            error => error.ToObjectResult());
    }

    [HttpPost("reorder")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult> Reorder(
        [FromBody] ReorderQuestionsDto dto,
        CancellationToken cancellationToken)
    {
        var command = new ReorderQuestionsCommand
        {
            Step = dto.Step,
            Questions = dto.Questions
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult>(
            _ => NoContent(),
            error => error.ToObjectResult());
    }
}