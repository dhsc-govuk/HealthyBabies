namespace Domain.SiteForms;

public record SiteFormQuestionOptionId(Guid Value)
{
    public static SiteFormQuestionOptionId Empty() => new(Guid.Empty);
    public static SiteFormQuestionOptionId New() => new(Guid.NewGuid());
    public static SiteFormQuestionOptionId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}