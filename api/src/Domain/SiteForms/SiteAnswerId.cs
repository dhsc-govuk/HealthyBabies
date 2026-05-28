namespace Domain.SiteForms;

public record SiteAnswerId(Guid Value)
{
    public static SiteAnswerId Empty() => new(Guid.Empty);
    public static SiteAnswerId New() => new(Guid.NewGuid());
    public static SiteAnswerId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}