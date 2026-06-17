using Application.Organisations.Dtos;
using LanguageExt;

namespace Api.Services.Abstract;

public interface IOrganisationContactsControllerService
{
    Task<IEnumerable<OrganisationContactDto>> GetByOrganisationId(Guid organisationId, CancellationToken cancellationToken);
    Task<Option<OrganisationContactDto>> GetById(Guid contactId, CancellationToken cancellationToken);
}