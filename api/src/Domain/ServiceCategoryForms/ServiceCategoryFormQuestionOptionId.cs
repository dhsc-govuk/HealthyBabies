namespace Domain.ServiceCategoryForms;

public record ServiceCategoryFormQuestionOptionId(Guid Value)
{
    public static ServiceCategoryFormQuestionOptionId New() => new(Guid.NewGuid());
    public static ServiceCategoryFormQuestionOptionId Empty() => new(Guid.Empty);
}