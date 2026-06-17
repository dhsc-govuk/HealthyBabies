using Application.Common.Interfaces.Queries;
using Application.ServiceCategoryForms.Dtos;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("service-category-form-questions")]
[Authorize(Roles = Role.OrganisationAdmin)]
public class ServiceCategoryFormQuestionsController(
    IServiceCategoryFormQuestionQueries questionQueries)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceCategoryFormQuestionDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var questions = await questionQueries.GetAll(cancellationToken);
        return Ok(questions.Select(ServiceCategoryFormQuestionDto.FromDomainModel).ToList());
    }

    [HttpGet("step/{step:int}")]
    public async Task<ActionResult<IReadOnlyList<ServiceCategoryFormQuestionDto>>> GetByStep(
        [FromRoute] int step,
        CancellationToken cancellationToken)
    {
        var questions = await questionQueries.GetByStep(step, cancellationToken);
        return Ok(questions.Select(ServiceCategoryFormQuestionDto.FromDomainModel).ToList());
    }
}