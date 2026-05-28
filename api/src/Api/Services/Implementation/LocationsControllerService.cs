using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Common.Permissions;
using Application.Organisations.Dtos;
using Application.SiteForms.Dtos;
using Domain.Locations;
using Domain.Organisations;
using LanguageExt;

namespace Api.Services.Implementation;

public class LocationsControllerService(
    ILocationQueries locationQueries) : ILocationsControllerService
{
    public async Task<IEnumerable<LocationDto>> GetGlobal(CancellationToken cancellationToken)
    {
        var locations = await locationQueries.GetGlobal(cancellationToken);

        return locations.Select(x => new LocationDto(
            x.Id.Value,
            x.OrganisationId.Value,
            x.Name,
            x.PostCode,
            x.ReferenceNumber,
            x.AddressLine1,
            x.AddressLine2,
            x.TownOrCity,
            x.County,
            x.IsActive,
            x.Answers.Select(SiteAnswerDto.FromDomainModel).ToList(),
            x.Organisation is not null
                ? new OrganisationDto(
                    x.Organisation.Id.Value,
                    x.Organisation.Name,
                    x.Organisation.ONSCode,
                    x.Organisation.IsActive)
                : null));
    }

    public async Task<IEnumerable<LocationDto>> GetAll(
      Permission permission,
      Guid organisationId,
      CancellationToken cancellationToken)
    {
        var locations = await locationQueries.GetAll(
            new OrganisationId(organisationId),
            permission,
            cancellationToken);

        return locations.Select(x => new LocationDto(
            x.Id.Value,
            x.OrganisationId.Value,
            x.Name,
            x.PostCode,
            x.ReferenceNumber,
            x.AddressLine1,
            x.AddressLine2,
            x.TownOrCity,
            x.County,
            x.IsActive,
            x.Answers.Select(SiteAnswerDto.FromDomainModel).ToList(),
            x.Organisation is not null
                ? new OrganisationDto(
                    x.Organisation.Id.Value,
                    x.Organisation.Name,
                    x.Organisation.ONSCode,
                    x.Organisation.IsActive)
                : null));
    }

    public async Task<Option<LocationDto>> Get(
       Permission permission,
       Guid locationId,
       CancellationToken cancellationToken)
    {
        var location = await locationQueries.GetById(
            new LocationId(locationId),
            permission,
            cancellationToken);

        return location.Match(
            l => new LocationDto(
                l.Id.Value,
                l.OrganisationId.Value,
                l.Name,
                l.PostCode,
                l.ReferenceNumber,
                l.AddressLine1,
                l.AddressLine2,
                l.TownOrCity,
                l.County,
                l.IsActive,
                l.Answers.Select(SiteAnswerDto.FromDomainModel).ToList(),
                l.Organisation is not null
                    ? new OrganisationDto(
                        l.Organisation.Id.Value,
                        l.Organisation.Name,
                        l.Organisation.ONSCode,
                        l.Organisation.IsActive)
                    : null),
            () => Option<LocationDto>.None);
    }

    public async Task<Option<LocationHomeDto>> Totals(
        Permission permission,
        Guid locationId,
        CancellationToken cancellationToken)
    {
        var locId = new LocationId(locationId);

        var location = await locationQueries.GetById(locId, permission, cancellationToken);

        return location.Match(
            l => new LocationHomeDto(
                LocationName: l.Name,
                OrganisationName: l.Organisation!.Name,
                Admins: 0),
            () => Option<LocationHomeDto>.None);
    }
}