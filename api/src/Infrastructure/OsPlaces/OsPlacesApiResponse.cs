using System.Text.Json.Serialization;

namespace Infrastructure.OsPlaces;

internal class OsPlacesApiResponse
{
    [JsonPropertyName("results")]
    public List<OsPlacesResult> Results { get; set; } = [];
}

internal class OsPlacesResult
{
    [JsonPropertyName("DPA")]
    public OsPlacesDpa? Dpa { get; set; }
}

internal class OsPlacesDpa
{
    [JsonPropertyName("UPRN")]
    public string Uprn { get; set; } = string.Empty;

    [JsonPropertyName("ADDRESS")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("BUILDING_NAME")]
    public string? BuildingName { get; set; }

    [JsonPropertyName("BUILDING_NUMBER")]
    public string? BuildingNumber { get; set; }

    [JsonPropertyName("THOROUGHFARE_NAME")]
    public string? ThoroughfareName { get; set; }

    [JsonPropertyName("DEPENDENT_LOCALITY")]
    public string? DependentLocality { get; set; }

    [JsonPropertyName("POST_TOWN")]
    public string PostTown { get; set; } = string.Empty;

    [JsonPropertyName("POSTCODE")]
    public string Postcode { get; set; } = string.Empty;

    [JsonPropertyName("ORGANISATION_NAME")]
    public string? OrganisationName { get; set; }
}