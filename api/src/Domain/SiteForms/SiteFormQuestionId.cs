namespace Domain.SiteForms;

public record SiteFormQuestionId(Guid Value)
{
    public static SiteFormQuestionId Empty() => new(Guid.Empty);
    public static SiteFormQuestionId New() => new(Guid.NewGuid());
    public static SiteFormQuestionId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}