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
    public DataCollectionType Type { get; private set; }

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
        bool isDraft,
        DataCollectionType type)
    {
        Id = id;
        Name = name;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        IsDraft = isDraft;
        IsSubmittedByAllLocalAuthorities = isSubmittedByAllLocalAuthorities;
        IsClosed = false;
        Type = type;
        CreatedAt = DateTime.UtcNow;
    }

    public static DataCollection New(
        DataCollectionId id,
        string name,
        string? description,
        DateTime startDate,
        DateTime endDate,
        bool isSubmittedByAllLocalAuthorities = true,
        bool saveAsDraft = false,
        DataCollectionType? type = null)
    {
        Guard.NotNull(id, nameof(id));
        Guard.NotNullOrEmpty(name, nameof(name));
        Guard.Ensure(startDate < endDate, "Start date must be before end date");

        return new DataCollection(id, name, description, startDate, endDate, isSubmittedByAllLocalAuthorities, saveAsDraft, type ?? DataCollectionType.DeliveryPlan);
    }

    public void UpdateAndSaveAsDraft(
        string name,
        string? description,
        DateTime startDate,
        DateTime endDate,
        bool isSubmittedByAllLocalAuthorities,
        DataCollectionType? type = null)
    {
        Guard.NotNullOrEmpty(name, nameof(name));
        Guard.Ensure(startDate < endDate, "Start date must be before end date");

        var previousEndDate = EndDate;
        Name = name;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        IsSubmittedByAllLocalAuthorities = isSubmittedByAllLocalAuthorities;
        IsDraft = true;
        Type = type ?? DataCollectionType.DeliveryPlan;
        UpdatedAt = DateTime.UtcNow;
        CascadeEndDateToLocalAuthorities(previousEndDate, endDate);
    }

    public void UpdateAndPublish(
        string name,
        string? description,
        DateTime startDate,
        DateTime endDate,
        bool isSubmittedByAllLocalAuthorities,
        DataCollectionType? type = null)
    {
        Guard.NotNullOrEmpty(name, nameof(name));
        Guard.Ensure(startDate < endDate, "Start date must be before end date");

        var previousEndDate = EndDate;
        Name = name;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        IsSubmittedByAllLocalAuthorities = isSubmittedByAllLocalAuthorities;
        IsDraft = false;
        Type = type ?? DataCollectionType.DeliveryPlan;
        UpdatedAt = DateTime.UtcNow;
        CascadeEndDateToLocalAuthorities(previousEndDate, endDate);
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

    // A local authority's own extended end date overrides the collection's default EndDate for that
    // local authority - callers must use this instead of EndDate directly for deadline/lock checks.
    public DateTime GetEffectiveEndDate(OrganisationId localAuthorityId)
    {
        var assignment = _localAuthorities.FirstOrDefault(la => la.LocalAuthorityId == localAuthorityId);
        return assignment?.EndDate ?? EndDate;
    }

    private static DateTime ToUkDate(DateTime value) => UkCalendar.ToUkDate(value);

    // Number of whole UK calendar days remaining before the deadline; 0 means today is the last day.
    public int GetDaysRemaining(OrganisationId localAuthorityId) =>
        (ToUkDate(GetEffectiveEndDate(localAuthorityId)) - ToUkDate(DateTime.UtcNow)).Days;

    // True once the UK calendar day after the deadline has begun - the deadline day itself stays open.
    public bool IsClosedFor(OrganisationId localAuthorityId) =>
        ToUkDate(DateTime.UtcNow) > ToUkDate(GetEffectiveEndDate(localAuthorityId));

    // Changes only the dates - deliberately leaves IsDraft and IsClosed alone, unlike
    // UpdateAndSaveAsDraft/UpdateAndPublish. Either date may be null to leave it unchanged.
    public void UpdateDates(DateTime? startDate, DateTime? endDate)
    {
        var newStartDate = startDate ?? StartDate;
        var newEndDate = endDate ?? EndDate;
        Guard.Ensure(newStartDate < newEndDate, "Start date must be before end date");

        var previousEndDate = EndDate;
        StartDate = newStartDate;
        EndDate = newEndDate;
        UpdatedAt = DateTime.UtcNow;
        CascadeEndDateToLocalAuthorities(previousEndDate, newEndDate);
    }

    // Local authority assignments snapshot the collection end date at assign time (see
    // AssignLocalAuthority) and GetEffectiveEndDate prefers that snapshot, so roll forward the ones
    // still sitting on the old value - otherwise an end date change is invisible to every assigned
    // local authority. Assignments extended individually keep their own date, and null ones already
    // track the collection through the GetEffectiveEndDate fallback. Every path that moves EndDate
    // must call this, or the assignment snapshots drift and stop matching for good.
    private void CascadeEndDateToLocalAuthorities(DateTime previousEndDate, DateTime newEndDate)
    {
        if (newEndDate == previousEndDate)
        {
            return;
        }

        foreach (var assignment in _localAuthorities.Where(la => la.EndDate == previousEndDate))
        {
            assignment.UpdateEndDate(newEndDate);
        }
    }

    // A collection open to all local authorities (IsSubmittedByAllLocalAuthorities) has no
    // DataCollectionLocalAuthority rows at all - everyone falls back to the collection-wide EndDate
    // via GetEffectiveEndDate. Giving one authority its own deadline (e.g. on reopen) therefore has
    // to create that authority's assignment on demand rather than silently no-op when it's missing.
    public void UpdateLocalAuthorityEndDate(OrganisationId localAuthorityId, DateTime? endDate)
    {
        var assignment = _localAuthorities.FirstOrDefault(la => la.LocalAuthorityId == localAuthorityId);
        if (assignment == null)
        {
            assignment = DataCollectionLocalAuthority.Create(Id, localAuthorityId, assignedById: null, EndDate);
            _localAuthorities.Add(assignment);
        }

        assignment.UpdateEndDate(endDate);
        UpdatedAt = DateTime.UtcNow;
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

    // Drops the collection's local authority scope, used when it is switched to "submitted by all
    // local authorities". Assignments double as the store for a single authority's own deadline
    // (see UpdateLocalAuthorityEndDate), and those must survive: clearing the lot silently reverted
    // every due date extension - and every resubmit-by date set by a reopen - to the collection-wide
    // EndDate on the next edit.
    //
    // Only a date past EndDate is an extension. Anything on or before it is either an untouched
    // assignment snapshot or a stale one left on a date the collection has since moved past -
    // snapshots only started rolling forward when CascadeEndDateToLocalAuthorities arrived, and
    // nothing backfilled the rows written before that. Keeping a stale one would pin its authority
    // to a deadline earlier than everybody else's, locking it out of a collection that is still
    // open (IsClosedFor reads the same snapshot) with nothing in the UI to explain why.
    public void ClearLocalAuthorityAssignments()
    {
        _localAuthorities.RemoveAll(la => la.EndDate == null || la.EndDate <= EndDate);
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

        var todayUk = ToUkDate(DateTime.UtcNow);
        if (todayUk < ToUkDate(StartDate))
        {
            return DataCollectionStatus.Planned;
        }

        if (todayUk <= ToUkDate(EndDate))
        {
            return DataCollectionStatus.Open;
        }

        return DataCollectionStatus.Closed;
    }
}