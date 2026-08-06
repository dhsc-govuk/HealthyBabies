using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("delivery-plan")]
[ApiController]
[Authorize(Roles = Role.DeliveryPlanLead)]
public class DeliveryPlanHomeController : ControllerBase
{
    [HttpGet("home")]
    public ActionResult GetHome()
    {
        return Ok();
    }
}