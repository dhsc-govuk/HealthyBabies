using Application.Common.Interfaces.Repositories;
using Application.Common.Permissions;
using LanguageExt.UnsafeValueAccess;
using MediatR;

namespace Application.Organisations.Queries;

public record MatchLocationsByNameQuery : IRequest<IReadOnlyList<LocationMatchResult>>
{
    public IReadOnlyList<string> SiteNames { get; init; } = [];
}

public record LocationMatchResult(
    string SearchName,
    Guid? LocationId,
    string? MatchedName,
    string? MatchedUprn,
    bool IsActive,
    bool HasExistingData);

public class MatchLocationsByNameQueryHandler(
    PermissionsService permissionsService,
    ILocationRepository locationRepository)
    : IRequestHandler<MatchLocationsByNameQuery, IReadOnlyList<LocationMatchResult>>
{
    public async Task<IReadOnlyList<LocationMatchResult>> Handle(
        MatchLocationsByNameQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = permissionsService.GetOrganisationPermissions();

        return await permissions.MatchAsync(
            async p =>
            {
                var organisationId = p.OrganisationId.ValueUnsafe();
                var locations = await locationRepository.FindByNamesOrUprnAsync(
                    request.SiteNames,
                    organisationId,
                    cancellationToken);

                var results = new List<LocationMatchResult>();

                foreach (var searchName in request.SiteNames)
                {
                    var matchedLocation = locations.FirstOrDefault(l =>
                        l.Name.Equals(searchName, StringComparison.OrdinalIgnoreCase) ||
                        l.ReferenceNumber.Equals(searchName, StringComparison.OrdinalIgnoreCase));

                    if (matchedLocation != null)
                    {
                        results.Add(new LocationMatchResult(
                            searchName,
                            matchedLocation.Id.Value,
                            matchedLocation.Name,
                            matchedLocation.ReferenceNumber,
                            matchedLocation.IsActive,
                            matchedLocation.Answers.Count > 0));
                    }
                    else
                    {
                        results.Add(new LocationMatchResult(
                            searchName,
                            null,
                            null,
                            null,
                            false,
                            false));
                    }
                }

                return (IReadOnlyList<LocationMatchResult>)results;
            },
            _ => Task.FromResult<IReadOnlyList<LocationMatchResult>>([]));
    }
}