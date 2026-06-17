using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Application.Organisations.Exceptions;
using Domain.Organisations;
using Domain.Systems;
using LanguageExt;
using MediatR;

namespace Application.Organisations.Commands;

public record CreateOrganisationContactCommand : IRequest<Either<OrganisationContactException, OrganisationContact>>
{
    public Guid OrganisationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

public class CreateOrganisationContactCommandHandler(
    IOrganisationContactRepository repository,
    IOrganisationRepository organisationRepository,
    IGlobalDataQueries globalDataQueries)
    : IRequestHandler<CreateOrganisationContactCommand, Either<OrganisationContactException, OrganisationContact>>
{
    public async Task<Either<OrganisationContactException, OrganisationContact>> Handle(
        CreateOrganisationContactCommand request,
        CancellationToken cancellationToken)
    {
        var organisationId = new OrganisationId(request.OrganisationId);
        var organisation = await organisationRepository.GetOrganisationById(organisationId, cancellationToken);

        return await organisation.MatchAsync(
            _ => ValidateRoleAsync(request, cancellationToken)
                .BindAsync(validatedRequest => CreateContact(validatedRequest, organisationId, cancellationToken)),
            () => new OrganisationContactUnknownException(
                OrganisationContactId.Empty(),
                new Exception($"Local Authority with id {organisationId} does not exist")));
    }

    private async Task<Either<OrganisationContactException, CreateOrganisationContactCommand>> ValidateRoleAsync(
        CreateOrganisationContactCommand request,
        CancellationToken cancellationToken)
    {
        var contactRoles = await globalDataQueries.GetByEntityAsync(GlobalDataEntityTypes.ContactRole, cancellationToken);
        var isValid = contactRoles.Any(r => r.Value == request.Role);

        return isValid
            ? request
            : new OrganisationContactInvalidRoleException(OrganisationContactId.Empty(), request.Role);
    }

    private async Task<Either<OrganisationContactException, OrganisationContact>> CreateContact(
        CreateOrganisationContactCommand request,
        OrganisationId organisationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var contact = OrganisationContact.New(
                OrganisationContactId.New(),
                organisationId,
                request.Name,
                request.Email,
                request.Role);

            var result = await repository.AddAsync(contact, cancellationToken);
            return result;
        }
        catch (Exception exception)
        {
            return new OrganisationContactUnknownException(OrganisationContactId.Empty(), exception);
        }
    }
}