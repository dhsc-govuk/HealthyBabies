using Api.Modules.Errors;
using Api.Services.Abstract;
using Application.Common;
using Application.Systems.Commands;
using Application.Systems.Dtos;
using Domain.Systems;
using Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("global-data")]
[ApiController]
[Authorize]
public class GlobalDataController(
    ISender sender,
    IGlobalDataControllerService controllerService)
    : ControllerBase
{
    [HttpGet("entity-types")]
    [Authorize(Roles = Role.Admin)]
    public ActionResult<IReadOnlyList<GlobalDataEntityTypeDto>> GetEntityTypes()
    {
        var entityTypes = GlobalDataEntityTypes.Entities
            .Select(e => new GlobalDataEntityTypeDto(e.Key, e.Value))
            .ToList();
        return entityTypes;
    }

    [HttpGet]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<IReadOnlyList<GlobalDataDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await controllerService.GetAll(cancellationToken);
        return result.ToList();
    }

    [HttpGet("entity/{entity}")]
    [Authorize(Roles = $"{Role.Admin},{Role.OrganisationAdmin}")]
    public async Task<ActionResult<IReadOnlyList<GlobalDataDto>>> GetByEntity(
        [FromRoute] string entity,
        CancellationToken cancellationToken)
    {
        var result = await controllerService.GetByEntity(entity, cancellationToken);
        return result.ToList();
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<GlobalDataDto>> Get(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await controllerService.GetById(id, cancellationToken);
        return result.Match<ActionResult<GlobalDataDto>>(
            r => r,
            NotFound());
    }

    [HttpPost]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<GlobalDataDto>> Create(
        [FromBody] CreateGlobalDataDto globalData,
        CancellationToken cancellationToken)
    {
        var input = new CreateGlobalDataCommand
        {
            Entity = Utility.HtmlDecodeField(globalData.Entity),
            Value = Utility.HtmlDecodeField(globalData.Value),
            Description = Utility.HtmlDecodeNullableField(globalData.Description)
        };

        var result = await sender.Send(input, cancellationToken);

        return result.Match<ActionResult<GlobalDataDto>>(
            g => new GlobalDataDto(
                g.Id.Value,
                g.Entity,
                g.Value,
                g.Description),
            error => error.ToObjectResult());
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult<GlobalDataDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateGlobalDataDto globalData,
        CancellationToken cancellationToken)
    {
        var input = new UpdateGlobalDataCommand
        {
            Id = id,
            Entity = globalData.Entity,
            Value = globalData.Value,
            Description = globalData.Description
        };

        var result = await sender.Send(input, cancellationToken);

        return result.Match<ActionResult<GlobalDataDto>>(
            g => new GlobalDataDto(
                g.Id.Value,
                g.Entity,
                g.Value,
                g.Description),
            error => error.ToObjectResult());
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Role.Admin)]
    public async Task<ActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var input = new DeleteGlobalDataCommand { Id = id };
        var result = await sender.Send(input, cancellationToken);

        return result.Match<ActionResult>(
            _ => NoContent(),
            error => error.ToObjectResult());
    }
}