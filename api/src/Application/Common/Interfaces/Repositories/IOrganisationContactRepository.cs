using Domain.Organisations;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IOrganisationContactRepository
{
    Task<Option<OrganisationContact>> GetByIdAsync(OrganisationContactId id, CancellationToken cancellationToken = default);
    Task<OrganisationContact> AddAsync(OrganisationContact entity, CancellationToken cancellationToken = default);
    Task<OrganisationContact> UpdateAsync(OrganisationContact entity, CancellationToken cancellationToken = default);
}