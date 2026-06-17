using Domain.Common;
using Domain.Organisations;
using Domain.Users;

namespace Domain.DataCollections;

public class DataCollection : SoftDeletableEntity<DataCollectionId>
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public bool IsDraft { get; private set; }
    public bool IsSubmittedByAllLocalAuthorities { get; private set; }
    public bool IsClosed { get; private set; }

    private readonly List<DataCollectionLocalAuthority> _localAuthorities = new();
    public IReadOnlyCollection<DataCollectionLocalAuthority> LocalAuthorities => _localAuthorities.AsReadOnly();

    private readonly List<DataCollectionFormModuleAssignment> _formModuleAssignments = new List<DataCollectionFormModuleAssignment>();
    public IReadOnlyCollection<DataCollectionFormModuleAssignment> FormModuleAssignments => _formModuleAssignments.AsReadOnly();

    private DataCollection(
        DataCollectionId id,
        string name,
        string? description,
        DateTime startDate,
        DateTime endDate,
        bool isSubmittedByAllLocalAuthorities,
        bool isDraft)
    {
        Id = id;
        Name = name;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        IsDraft = isDraft;
        IsSubmittedByAllLocalAuthorities = isSubmittedByAllLocalAuthorities;
        IsClosed = false;
        CreatedAt = DateTime.UtcNow;
    }

    public static DataCollection New(
        DataCollectionId id,
        string name,
        string? description,
        DateTime startDate,
        DateTime endDate,
        bool isSubmittedByAllLocalAuthorities = true,
        bool saveAsDraft = false)
    {
        Guard.NotNull(id, nameof(id));
        Guard.NotNullOrEmpty(name, nameof(name));
        Guard.Ensure(startDate < endDate, "Start date must be before end date");

        return new DataCollection(id, name, description, startDate, endDate, isSubmittedByAllLocalAuthorities, saveAsDraft);
    }

    public void UpdateAndSaveAsDraft(
        string name,
        string? description,
        DateTime startDate,
        DateTime endDate,
        bool isSubmittedByAllLocalAuthorities)
    {
        Guard.NotNullOrEmpty(name, nameof(name));
        Guard.Ensure(startDate < endDate, "Start date must be before end date");

        Name = name;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        IsSubmittedByAllLocalAuthorities = isSubmittedByAllLocalAuthorities;
        IsDraft = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAndPublish(
        string name,
        string? description,
        DateTime startDate,
        DateTime endDate,
        bool isSubmittedByAllLocalAuthorities)
    {
        Guard.NotNullOrEmpty(name, nameof(name));
        Guard.Ensure(startDate < endDate, "Start date must be before end date");

        Name = name;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        IsSubmittedByAllLocalAuthorities = isSubmittedByAllLocalAuthorities;
        IsDraft = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignLocalAuthority(OrganisationId localAuthorityId, UserId? assignedById = null)
    {
        if (_localAuthorities.Any(la => la.LocalAuthorityId == localAuthorityId))
        {
            return;
        }

        var assignment = DataCollectionLocalAuthority.Create(Id, localAuthorityId, assignedById, EndDate);
        _localAuthorities.Add(assignment);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateLocalAuthorityEndDate(OrganisationId localAuthorityId, DateTime? endDate)
    {
        var assignment = _localAuthorities.FirstOrDefault(la => la.LocalAuthorityId == localAuthorityId);
        if (assignment != null)
        {
            assignment.UpdateEndDate(endDate);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveLocalAuthority(OrganisationId localAuthorityId)
    {
        var assignment = _localAuthorities.FirstOrDefault(la => la.LocalAuthorityId == localAuthorityId);
        if (assignment != null)
        {
            _localAuthorities.Remove(assignment);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void ClearLocalAuthorities()
    {
        _localAuthorities.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignFormModule(DataCollectionFormModuleId formModuleId)
    {
        if (_formModuleAssignments.Any(fm => fm.FormModuleId == formModuleId))
        {
            return;
        }

        var assignment = DataCollectionFormModuleAssignment.Create(Id, formModuleId);
        _formModuleAssignments.Add(assignment);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveFormModule(DataCollectionFormModuleId formModuleId)
    {
        var assignment = _formModuleAssignments.FirstOrDefault(fm => fm.FormModuleId == formModuleId);
        if (assignment != null)
        {
            _formModuleAssignments.Remove(assignment);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void ClearFormModules()
    {
        _formModuleAssignments.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateFormModules(IReadOnlyList<DataCollectionFormModuleId> formModuleIds)
    {
        var currentIds = _formModuleAssignments.Select(fm => fm.FormModuleId).ToHashSet();
        var newIds = formModuleIds.ToHashSet();

        var toRemove = currentIds.Except(newIds).ToList();
        foreach (var id in toRemove)
        {
            RemoveFormModule(id);
        }

        var toAdd = newIds.Except(currentIds).ToList();
        foreach (var id in toAdd)
        {
            AssignFormModule(id);
        }
    }

    public void RevertToDraft()
    {
        IsDraft = true;
        IsClosed = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        IsClosed = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reopen()
    {
        IsClosed = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public DataCollectionStatus GetStatus()
    {
        if (IsDraft)
        {
            return DataCollectionStatus.Draft;
        }

        if (IsClosed)
        {
            return DataCollectionStatus.Closed;
        }

        var now = DateTime.UtcNow;
        if (now < StartDate)
        {
            return DataCollectionStatus.Planned;
        }

        if (now >= StartDate && now <= EndDate)
        {
            return DataCollectionStatus.Open;
        }

        return DataCollectionStatus.Closed;
    }
}