using Application.Common.Interfaces.Repositories;
using Application.Organisations.Exceptions;
using Domain.Organisations;
using Domain.Users;
using LanguageExt;
using MediatR;

namespace Application.Organisations.Commands;

public record DeleteOrganisationContactCommand : IRequest<Either<OrganisationContactException, OrganisationContact>>
{
    public required Guid Id { get; init; }
    public required UserId DeletedByUserId { get; init; }
}

public class DeleteOrganisationContactCommandHandler(IOrganisationContactRepository repository)
    : IRequestHandler<DeleteOrganisationContactCommand, Either<OrganisationContactException, OrganisationContact>>
{
    public async Task<Either<OrganisationContactException, OrganisationContact>> Handle(
        DeleteOrganisationContactCommand request,
        CancellationToken cancellationToken)
    {
        var contactId = new OrganisationContactId(request.Id);
        var contact = await repository.GetByIdAsync(contactId, cancellationToken);

        return await contact.MatchAsync(
            c => DeleteContact(request, c, cancellationToken),
            () => new OrganisationContactDoesNotExistException(contactId));
    }

    private async Task<Either<OrganisationContactException, OrganisationContact>> DeleteContact(
        DeleteOrganisationContactCommand request,
        OrganisationContact contact,
        CancellationToken cancellationToken)
    {
        try
        {
            contact.Delete(request.DeletedByUserId);
            return await repository.UpdateAsync(contact, cancellationToken);
        }
        catch (Exception exception)
        {
            return new OrganisationContactUnknownException(contact.Id, exception);
        }
    }
}