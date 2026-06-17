using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using LanguageExt.UnsafeValueAccess;
using MediatR;

namespace Application.Services.Queries;

public record MatchServicesByNameQuery : IRequest<IReadOnlyList<ServiceMatchResult>>
{
    public IReadOnlyList<string> ServiceNames { get; init; } = [];
}

public record ServiceMatchResult(
    string SearchName,
    Guid? ServiceId,
    string? MatchedName,
    int? Status,
    DateTime? LastUpdated,
    bool HasExistingData);

public class MatchServicesByNameQueryHandler(
    PermissionsService permissionsService,
    IServiceRepository serviceRepository)
    : IRequestHandler<MatchServicesByNameQuery, IReadOnlyList<ServiceMatchResult>>
{
    public async Task<IReadOnlyList<ServiceMatchResult>> Handle(
        MatchServicesByNameQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();

        return await permissions.MatchAsync(
            async p =>
            {
                var organisationId = p.OrganisationId.ValueUnsafe();
                var services = await serviceRepository.FindByNamesAsync(
                    request.ServiceNames,
                    organisationId,
                    cancellationToken);

                var results = new List<ServiceMatchResult>();

                foreach (var searchName in request.ServiceNames)
                {
                    var matchedService = services.FirstOrDefault(s =>
                        s.Name.Equals(searchName, StringComparison.OrdinalIgnoreCase));

                    if (matchedService != null)
                    {
                        results.Add(new ServiceMatchResult(
                            searchName,
                            matchedService.Id.Value,
                            matchedService.Name,
                            (int)matchedService.Status,
                            matchedService.UpdatedAt,
                            matchedService.Answers.Count > 0));
                    }
                    else
                    {
                        results.Add(new ServiceMatchResult(
                            searchName,
                            null,
                            null,
                            null,
                            null,
                            false));
                    }
                }

                return (IReadOnlyList<ServiceMatchResult>)results;
            },
            _ => (IReadOnlyList<ServiceMatchResult>)[]);
    }
}