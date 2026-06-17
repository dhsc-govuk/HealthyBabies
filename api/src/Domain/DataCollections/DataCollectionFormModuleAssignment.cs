using Domain.Common;

namespace Domain.DataCollections;

public class DataCollectionFormModuleAssignment : AuditableEntity<Guid>
{
    public DataCollectionId DataCollectionId { get; private set; }
    public DataCollection DataCollection { get; private set; } = null!;

    public DataCollectionFormModuleId FormModuleId { get; private set; }
    public DataCollectionFormModule FormModule { get; private set; } = null!;

    private DataCollectionFormModuleAssignment()
    {
        DataCollectionId = null!;
        FormModuleId = null!;
    }

    private DataCollectionFormModuleAssignment(
        DataCollectionId dataCollectionId,
        DataCollectionFormModuleId formModuleId)
    {
        Id = Guid.NewGuid();
        DataCollectionId = dataCollectionId;
        FormModuleId = formModuleId;
        CreatedAt = DateTime.UtcNow;
    }

    public static DataCollectionFormModuleAssignment Create(
        DataCollectionId dataCollectionId,
        DataCollectionFormModuleId formModuleId)
    {
        Guard.NotNull(dataCollectionId, nameof(dataCollectionId));
        Guard.NotNull(formModuleId, nameof(formModuleId));

        return new DataCollectionFormModuleAssignment(dataCollectionId, formModuleId);
    }
}