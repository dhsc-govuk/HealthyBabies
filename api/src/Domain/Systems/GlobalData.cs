namespace Domain.Systems;

public class GlobalData : AuditableEntity<GlobalDataId>
{
    public string Entity { get; set; }
    public string Value { get; set; }
    public string? Description { get; set; }

    public GlobalData(GlobalDataId id, string entity, string value, string? description)
    {
        Id = id;
        Entity = entity;
        Value = value;
        Description = description;
    }

    public static GlobalData New(string entity, string value, string? description)
        => new(GlobalDataId.New(), entity, value, description);

    public void UpdateDetails(string entity, string value, string? description)
    {
        Value = value;
        Description = description;
    }
}