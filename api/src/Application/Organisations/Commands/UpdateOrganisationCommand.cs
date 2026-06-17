using Application.Common.Interfaces.Repositories;
using Application.Organisations.Exceptions;
using Domain.Organisations;
using LanguageExt;
using MediatR;

namespace Application.Organisations.Commands;

public record UpdateOrganisationCommand : IRequest<Either<OrganisationException, Organisation>>
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string ONSCode { get; init; }
    public bool IsActive { get; init; }
}

public class UpdateOrganisationCommandHandler(IOrganisationRepository repository)
    : IRequestHandler<UpdateOrganisationCommand, Either<OrganisationException, Organisation>>
{
    public async Task<Either<OrganisationException, Organisation>> Handle(UpdateOrganisationCommand request, CancellationToken cancellationToken)
    {
        var organisationId = new OrganisationId(request.Id);
        var organisationResult = await GetOrganisation(organisationId, cancellationToken);
        return await organisationResult
            .BindAsync(o => FindDuplicate(request, organisationId, o, cancellationToken));
    }

    private async Task<Either<OrganisationException, Organisation>> FindDuplicate(
        UpdateOrganisationCommand request,
        OrganisationId organisationId,
        Organisation organisation,
        CancellationToken cancellationToken)
    {
        var organisationWithName = await repository.FindDuplicateAsync(request.Name!, organisationId, cancellationToken);
        return await organisationWithName.MatchAsync(
            o => new OrganisationAlreadyExistsException(o.Id, request.Name!),
            () => UpdateOrganisation(request, organisation, cancellationToken));
    }

    private async Task<Either<OrganisationException, Organisation>> UpdateOrganisation(
        UpdateOrganisationCommand request,
        Organisation organisation,
        CancellationToken cancellationToken)
    {
        try
        {
            organisation.UpdateDetails(request.Name!, request.ONSCode ?? string.Empty, request.IsActive);
            return await repository.UpdateAsync(organisation, cancellationToken);
        }
        catch (Exception exception)
        {
            return new OrganisationUnknownException(organisation.Id, exception);
        }
    }

    private async Task<Either<OrganisationException, Organisation>> GetOrganisation(
        OrganisationId id,
        CancellationToken cancellationToken)
    {
        var organisation = await repository.GetOrganisationById(id, cancellationToken);
        return organisation.Match<Either<OrganisationException, Organisation>>(
            o => o,
            new OrganisationDoesNotExistException(id));
    }
}