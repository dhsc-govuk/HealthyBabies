using Application.Common.Dtos;
using Application.Common.Interfaces;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class AddressLookupController(IOsPlacesService osPlacesService) : ControllerBase
{
    [HttpGet("address-lookup")]
    [Authorize(Roles = $"{Role.Admin},{Role.OrganisationAdmin}")]
    public async Task<ActionResult<IReadOnlyList<OsPlacesAddressDto>>> SearchByPostcode(
        [FromQuery] string postcode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(postcode))
        {
            return BadRequest("Postcode is required");
        }

        var result = await osPlacesService.SearchByPostcodeAsync(postcode, cancellationToken);

        return result.Match<ActionResult<IReadOnlyList<OsPlacesAddressDto>>>(
            addresses => Ok(addresses),
            error => BadRequest(error.Message));
    }
}