using Domain.Organisations;
using Domain.Users;

namespace Domain.DataCollections;

public class DataCollectionLocalAuthority
{
    public DataCollectionId DataCollectionId { get; private set; }
    public DataCollection DataCollection { get; private set; } = null!;
    public OrganisationId LocalAuthorityId { get; private set; }
    public Organisation LocalAuthority { get; private set; } = null!;
    public DateTime AssignedAt { get; private set; }
    public DateTime? EndDate { get; private set; }
    public UserId? AssignedById { get; private set; }
    public User? AssignedBy { get; private set; }

    private DataCollectionLocalAuthority(
        DataCollectionId dataCollectionId,
        OrganisationId localAuthorityId,
        UserId? assignedById,
        DateTime? endDate = null)
    {
        DataCollectionId = dataCollectionId;
        LocalAuthorityId = localAuthorityId;
        AssignedById = assignedById;
        AssignedAt = DateTime.UtcNow;
        EndDate = endDate;
    }

    public static DataCollectionLocalAuthority Create(
        DataCollectionId dataCollectionId,
        OrganisationId localAuthorityId,
        UserId? assignedById = null,
        DateTime? endDate = null)
    {
        return new DataCollectionLocalAuthority(dataCollectionId, localAuthorityId, assignedById, endDate);
    }

    public void UpdateEndDate(DateTime? endDate)
    {
        EndDate = endDate;
    }
}