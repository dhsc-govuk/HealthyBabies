namespace Domain.ServiceCategories;

public record ServiceCategoryId(Guid Value)
{
    public static ServiceCategoryId New() => new(Guid.NewGuid());
    public static ServiceCategoryId Empty() => new(Guid.Empty);
}