namespace Domain.DataCollections.Forms;

public record DataSourceItemId(Guid Value)
{
    public static DataSourceItemId New() => new(Guid.NewGuid());
    public static DataSourceItemId From(Guid value) => new(value);
}