using Domain.Common;

namespace Domain.DataCollections.Forms;

/// <summary>
/// A reusable lookup table/data source for form field options.
/// Examples: Service Categories, Service Types, Delivery Methods, etc.
/// This allows options to be maintained separately and reused across multiple forms.
/// </summary>
public class DataSource : AuditableEntity<DataSourceId>
{
    private readonly List<DataSourceItem> _items = new();

    /// <summary>
    /// Gets unique code for this data source (e.g., "service-categories", "delivery-methods").
    /// Used for referencing in form fields.
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Gets display name (e.g., "Service Categories").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets description of what this data source contains.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets a value indicating whether whether this data source is active and available for use.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Gets a value indicating whether whether items in this data source are hierarchical (parent-child).
    /// e.g., Categories → Subcategories.
    /// </summary>
    public bool IsHierarchical { get; private set; }

    /// <summary>
    /// Gets items in this data source.
    /// </summary>
    public IReadOnlyCollection<DataSourceItem> Items => _items.AsReadOnly();

    private DataSource()
    {
    }

    private DataSource(
        DataSourceId id,
        string code,
        string name,
        string? description,
        bool isHierarchical)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(code, nameof(code));
        Guard.NotNullOrEmptyOrWhiteSpace(name, nameof(name));

        Id = id;
        Code = code;
        Name = name;
        Description = description;
        IsHierarchical = isHierarchical;
    }

    public static DataSource Create(
        string code,
        string name,
        string? description = null,
        bool isHierarchical = false)
    {
        return new DataSource(
            DataSourceId.New(),
            code,
            name,
            description,
            isHierarchical);
    }

    public void UpdateDetails(string name, string? description)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(name, nameof(name));

        Name = name;
        Description = description;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Adds an item to this data source.
    /// </summary>
    /// <returns></returns>
    public DataSourceItem AddItem(
        string value,
        string label,
        int displayOrder,
        DataSourceItemId? parentItemId = null,
        string? metadata = null)
    {
        var item = DataSourceItem.Create(
            Id,
            value,
            label,
            displayOrder,
            parentItemId,
            metadata);

        _items.Add(item);
        return item;
    }

    /// <summary>
    /// Removes an item from this data source.
    /// </summary>
    public void RemoveItem(DataSourceItemId itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            _items.Remove(item);
        }
    }

    /// <summary>
    /// Gets root-level items (no parent).
    /// </summary>
    /// <returns></returns>
    public IEnumerable<DataSourceItem> GetRootItems()
    {
        return _items.Where(i => i.ParentItemId == null && i.IsActive)
                     .OrderBy(i => i.DisplayOrder);
    }

    /// <summary>
    /// Gets child items for a given parent.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<DataSourceItem> GetChildItems(DataSourceItemId parentId)
    {
        return _items.Where(i => i.ParentItemId == parentId && i.IsActive)
                     .OrderBy(i => i.DisplayOrder);
    }
}