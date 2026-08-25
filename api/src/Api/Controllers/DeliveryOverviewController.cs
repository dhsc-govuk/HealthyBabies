using Api.Services.Abstract;
using Application.Organisations.Dtos;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>Department-wide (cross-organisation) Delivery overview dashboard — BLUM-7524.</summary>
[Route("admin/trends/delivery-overview")]
[ApiController]
[Authorize(Roles = Role.Admin)]
public class DeliveryOverviewController(IDeliveryOverviewControllerService controllerService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DeliveryOverviewSummaryDto>> GetSummary(
        [FromQuery] string yearRange,
        [FromQuery] string? strand,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(yearRange))
        {
            return BadRequest("yearRange is required.");
        }

        return await controllerService.GetSummary(yearRange, strand, cancellationToken);
    }

    [HttpGet("delivery-status")]
    public async Task<ActionResult<DeliveryOverviewDeliveryStatusDto>> GetDeliveryStatus(
        [FromQuery] string yearRange,
        [FromQuery] string? strand,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(yearRange))
        {
            return BadRequest("yearRange is required.");
        }

        return await controllerService.GetDeliveryStatus(yearRange, strand, cancellationToken);
    }

    [HttpGet("activity-status")]
    public async Task<ActionResult<DeliveryOverviewActivityStatusDto>> GetActivityStatus(
        [FromQuery] string yearRange,
        [FromQuery] string? strand,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(yearRange))
        {
            return BadRequest("yearRange is required.");
        }

        return await controllerService.GetActivityStatus(yearRange, strand, cancellationToken);
    }
}