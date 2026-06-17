using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Organisations.Dtos;
using Domain.Organisations;
using LanguageExt;

namespace Api.Services.Implementation;

public class OrganisationContactsControllerService(IOrganisationContactQueries contactQueries)
    : IOrganisationContactsControllerService
{
    public async Task<IEnumerable<OrganisationContactDto>> GetByOrganisationId(
        Guid organisationId,
        CancellationToken cancellationToken)
    {
        var contacts = await contactQueries.GetByOrganisationIdAsync(
            new OrganisationId(organisationId),
            cancellationToken);

        return contacts.Select(x => new OrganisationContactDto(
            x.Id.Value,
            x.OrganisationId.Value,
            x.Name,
            x.Email,
            x.Role));
    }

    public async Task<Option<OrganisationContactDto>> GetById(
        Guid contactId,
        CancellationToken cancellationToken)
    {
        var contact = await contactQueries.GetByIdAsync(
            new OrganisationContactId(contactId),
            cancellationToken);

        return contact.Match(
            c => new OrganisationContactDto(
                c.Id.Value,
                c.OrganisationId.Value,
                c.Name,
                c.Email,
                c.Role),
            () => Option<OrganisationContactDto>.None);
    }
}