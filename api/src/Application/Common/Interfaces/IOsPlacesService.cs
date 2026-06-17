using Application.Common.Dtos;
using LanguageExt;

namespace Application.Common.Interfaces;

public interface IOsPlacesService
{
    Task<Either<Exception, IReadOnlyList<OsPlacesAddressDto>>> SearchByPostcodeAsync(
        string postcode,
        CancellationToken cancellationToken = default);
}