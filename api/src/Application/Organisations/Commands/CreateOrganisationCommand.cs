using Application.Common.Interfaces.Repositories;
using Application.Organisations.Exceptions;
using Domain.Organisations;
using LanguageExt;
using MediatR;

namespace Application.Organisations.Commands;

public record CreateContactCommand(string FullName, string Role, string? RoleTitle, string Email);

public record CreateOrganisationCommand : IRequest<Either<OrganisationException, Organisation>>
{
    public required string Name { get; init; }
    public required string ONSCode { get; init; }
    public bool IsActive { get; init; }
    public List<CreateContactCommand>? Contacts { get; init; }
}

public class CreateOrganisationCommandHandler(
    IOrganisationRepository repository,
    IOrganisationContactRepository contactRepository)
    : IRequestHandler<CreateOrganisationCommand, Either<OrganisationException, Organisation>>
{
    public async Task<Either<OrganisationException, Organisation>> Handle(CreateOrganisationCommand request, CancellationToken cancellationToken)
    {
        var organisationWithName = await repository.FindByNameAsync(
            request.Name,
            cancellationToken);

        return await organisationWithName.MatchAsync(
            o => new OrganisationAlreadyExistsException(o.Id, request.Name!),
            () => CreateOrganisation(request, cancellationToken));
    }

    private async Task<Either<OrganisationException, Organisation>> CreateOrganisation(CreateOrganisationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var organisation = Organisation.New(
                OrganisationId.New(),
                request.Name,
                request.ONSCode,
                request.IsActive);
            var result = await repository.AddAsync(organisation, cancellationToken);

            if (request.Contacts is { Count: > 0 })
            {
                foreach (var contact in request.Contacts)
                {
                    var organisationContact = OrganisationContact.New(
                        OrganisationContactId.New(),
                        result.Id,
                        contact.FullName,
                        contact.Email,
                        contact.Role,
                        contact.RoleTitle);
                    await contactRepository.AddAsync(organisationContact, cancellationToken);
                }
            }

            return result;
        }
        catch (Exception exception)
        {
            return new OrganisationUnknownException(OrganisationId.Empty(), exception);
        }
    }
}