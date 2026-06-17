namespace Application.Common.Dtos;

public record OsPlacesAddressDto(
    string Uprn,
    string Address,
    string? BuildingName,
    string? BuildingNumber,
    string? ThoroughfareName,
    string? DependentLocality,
    string PostTown,
    string Postcode,
    string? OrganisationName);