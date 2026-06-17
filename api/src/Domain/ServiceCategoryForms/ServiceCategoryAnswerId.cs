namespace Domain.ServiceCategoryForms;

public record ServiceCategoryAnswerId(Guid Value)
{
    public static ServiceCategoryAnswerId New() => new(Guid.NewGuid());
    public static ServiceCategoryAnswerId Empty() => new(Guid.Empty);
}