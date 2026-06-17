using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Common.Permissions;
using Application.Organisations.Dtos;
using Domain.Organisations;
using LanguageExt;

namespace Api.Services.Implementation;

public class OrganisationsControllerService(
    IOrganisationQueries organisationQueries,
    IOrganisationUserQueries organisationUserQueries,
    ILocationQueries locationQueries,
    IOrganisationContactQueries organisationContactQueries) : IOrganisationsControllerService
{
    public async Task<IEnumerable<OrganisationDto>> GetAll(CancellationToken cancellationToken)
    {
        var organisations = await organisationQueries.GetOrganisations(cancellationToken);

        return organisations
            .Select(x => new OrganisationDto(x.Id.Value, x.Name, x.ONSCode, x.IsActive));
    }

    public async Task<Option<OrganisationDto>> Get(Guid organisationId, CancellationToken cancellationToken)
    {
        var entityId = new OrganisationId(organisationId);
        var organisation = await organisationQueries.GetOrganisationById(entityId, cancellationToken);
        return organisation.Match(
            org => new OrganisationDto(
                org.Id.Value,
                org.Name,
                org.ONSCode,
                org.IsActive),
            () => Option<OrganisationDto>.None);
    }

    public async Task<Option<OrganisationHomeDto>> Totals(
        Permission permission,
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var entityId = new OrganisationId(organisationId);

        return await organisationQueries.GetOrganisationById(entityId, cancellationToken)
            .MatchAsync<Organisation, Option<OrganisationHomeDto>>(
                async o =>
                {
                    var contacts = await organisationContactQueries.GetByOrganisationIdAsync(entityId, cancellationToken);
                    var contactDetails = contacts.Select(c => new OrganisationContactDetailDto(
                        c.Name,
                        c.Role,
                        c.RoleTitle,
                        c.Email)).ToList();

                    return new OrganisationHomeDto(
                        Name: o.Name,
                        ONSCode: o.ONSCode,
                        IsActive: o.IsActive,
                        Admins: await organisationUserQueries.Count(entityId, permission, cancellationToken),
                        Locations: await locationQueries.Count(entityId, permission, cancellationToken),
                        ContactDetails: contactDetails,
                        CreatedBy: o.CreatedBy?.Name.ToString(),
                        CreatedAt: o.CreatedAt,
                        LastChangedBy: o.LastModifiedBy?.Name.ToString(),
                        LastChangedAt: o.UpdatedAt);
                },
                () => Option<OrganisationHomeDto>.None);
    }
}