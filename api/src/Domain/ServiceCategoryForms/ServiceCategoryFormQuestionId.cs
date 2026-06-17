namespace Domain.ServiceCategoryForms;

public record ServiceCategoryFormQuestionId(Guid Value)
{
    public static ServiceCategoryFormQuestionId New() => new(Guid.NewGuid());
    public static ServiceCategoryFormQuestionId Empty() => new(Guid.Empty);
}