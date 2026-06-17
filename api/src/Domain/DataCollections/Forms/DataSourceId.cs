namespace Domain.DataCollections.Forms;

public record DataSourceId(Guid Value)
{
    public static DataSourceId New() => new(Guid.NewGuid());
    public static DataSourceId From(Guid value) => new(value);
}