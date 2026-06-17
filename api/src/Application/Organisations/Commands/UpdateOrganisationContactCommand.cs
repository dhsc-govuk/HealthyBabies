using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Organisations.Exceptions;
using Domain.Organisations;
using Domain.Systems;
using LanguageExt;
using MediatR;

namespace Application.Organisations.Commands;

public record UpdateOrganisationContactCommand : IRequest<Either<OrganisationContactException, OrganisationContact>>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

public class UpdateOrganisationContactCommandHandler(
    IOrganisationContactRepository repository,
    IGlobalDataQueries globalDataQueries)
    : IRequestHandler<UpdateOrganisationContactCommand, Either<OrganisationContactException, OrganisationContact>>
{
    public async Task<Either<OrganisationContactException, OrganisationContact>> Handle(
        UpdateOrganisationContactCommand request,
        CancellationToken cancellationToken)
    {
        var contactId = new OrganisationContactId(request.Id);
        var contact = await repository.GetByIdAsync(contactId, cancellationToken);

        return await contact.MatchAsync(
            c => ValidateRoleAsync(request, c, cancellationToken)
                .BindAsync(validatedContact => UpdateContact(request, validatedContact, cancellationToken)),
            () => new OrganisationContactDoesNotExistException(contactId));
    }

    private async Task<Either<OrganisationContactException, OrganisationContact>> ValidateRoleAsync(
        UpdateOrganisationContactCommand request,
        OrganisationContact contact,
        CancellationToken cancellationToken)
    {
        var contactRoles = await globalDataQueries.GetByEntityAsync(GlobalDataEntityTypes.ContactRole, cancellationToken);
        var isValid = contactRoles.Any(r => r.Value == request.Role);

        return isValid
            ? contact
            : new OrganisationContactInvalidRoleException(contact.Id, request.Role);
    }

    private async Task<Either<OrganisationContactException, OrganisationContact>> UpdateContact(
        UpdateOrganisationContactCommand request,
        OrganisationContact contact,
        CancellationToken cancellationToken)
    {
        try
        {
            contact.UpdateDetails(request.Name, request.Email, request.Role);
            return await repository.UpdateAsync(contact, cancellationToken);
        }
        catch (Exception exception)
        {
            return new OrganisationContactUnknownException(contact.Id, exception);
        }
    }
}