using Application.Users.Dtos;

namespace Api.Services.Abstract;

public interface IAdminHomeControllerService
{
    Task<AdminTotalsResponse> GetTotals(CancellationToken cancellationToken);
}