using Domain.Common;
using Domain.Organisations;
using Domain.SiteForms;

namespace Domain.Locations;

public class Location : IEntity<LocationId>
{
    private readonly List<SiteAnswer> _answers = new();

    public LocationId Id { get; private set; }
    public OrganisationId OrganisationId { get; private set; }
    public string Name { get; private set; }
    public string PostCode { get; private set; }
    public string ReferenceNumber { get; private set; }
    public string AddressLine1 { get; private set; }
    public string AddressLine2 { get; private set; }
    public string TownOrCity { get; private set; }
    public string County { get; private set; }
    public bool IsActive { get; private set; }

    public Organisation? Organisation { get; private set; }
    public IReadOnlyCollection<SiteAnswer> Answers => _answers.AsReadOnly();

    private Location()
    {
        Id = LocationId.Empty();
        OrganisationId = OrganisationId.Empty();
        Name = string.Empty;
        PostCode = string.Empty;
        ReferenceNumber = string.Empty;
        AddressLine1 = string.Empty;
        AddressLine2 = string.Empty;
        TownOrCity = string.Empty;
        County = string.Empty;
    }

    private Location(
        LocationId id,
        OrganisationId organisationId,
        string name,
        string postCode,
        string referenceNumber,
        string addressLine1,
        string addressLine2,
        string townOrCity,
        string county,
        bool isActive)
    {
        Guard.NotNull(id, nameof(id));
        Guard.NotNull(organisationId, nameof(organisationId));
        Guard.NotNullOrEmptyOrWhiteSpace(name, nameof(name));

        Id = id;
        OrganisationId = organisationId;
        Name = name;
        PostCode = postCode;
        ReferenceNumber = referenceNumber;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        TownOrCity = townOrCity;
        County = county;
        IsActive = isActive;
    }

    public static Location New(
        LocationId id,
        OrganisationId organisationId,
        string name,
        string postCode,
        string referenceNumber,
        string addressLine1 = "",
        string addressLine2 = "",
        string townOrCity = "",
        string county = "",
        bool isActive = true)
    {
        return new Location(
            id,
            organisationId,
            name,
            postCode,
            referenceNumber,
            addressLine1,
            addressLine2,
            townOrCity,
            county,
            isActive);
    }

    public void UpdateName(string name)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(name, nameof(name));
        Name = name;
    }

    public void UpdatePostCode(string postCode)
    {
        PostCode = postCode;
    }

    public void UpdateReferenceNumber(string referenceNumber)
    {
        ReferenceNumber = referenceNumber;
    }

    public void UpdateAddressLine1(string addressLine1)
    {
        AddressLine1 = addressLine1;
    }

    public void UpdateAddressLine2(string addressLine2)
    {
        AddressLine2 = addressLine2;
    }

    public void UpdateTownOrCity(string townOrCity)
    {
        TownOrCity = townOrCity;
    }

    public void UpdateCounty(string county)
    {
        County = county;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    public SiteAnswer AddAnswer(
        string questionCode,
        string questionLabel,
        string? questionHint,
        SiteFormQuestionType questionType,
        int displayOrder,
        string? value,
        string? displayValue,
        string? optionsSnapshot = null)
    {
        var answer = SiteAnswer.New(
            SiteAnswerId.New(),
            Id,
            questionCode,
            questionLabel,
            questionHint,
            questionType,
            displayOrder,
            value,
            displayValue,
            optionsSnapshot);
        _answers.Add(answer);
        return answer;
    }

    public void UpdateAnswer(string questionCode, string? value, string? displayValue)
    {
        var answer = _answers.FirstOrDefault(a => a.QuestionCode == questionCode);
        if (answer != null)
        {
            answer.UpdateAnswer(value, displayValue);
        }
    }

    public SiteAnswer? GetAnswer(string questionCode)
    {
        return _answers.FirstOrDefault(a => a.QuestionCode == questionCode);
    }

    public void ClearAnswers()
    {
        _answers.Clear();
    }
}