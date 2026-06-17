using Api.Services.Abstract;
using Application.Common.Interfaces.Queries;
using Application.Users.Dtos;

namespace Api.Services.Implementation;

public class AdminHomeControllerService(
    IUserQueries userQueries,
    IOrganisationQueries organisationQueries) : IAdminHomeControllerService
{
    public async Task<AdminTotalsResponse> GetTotals(CancellationToken cancellationToken)
    {
        var adminsCount = await userQueries.Count(cancellationToken);
        var organisationsCount = await organisationQueries.Count(cancellationToken);

        return new AdminTotalsResponse(
            Admins: adminsCount,
            Organisations: organisationsCount);
    }
}