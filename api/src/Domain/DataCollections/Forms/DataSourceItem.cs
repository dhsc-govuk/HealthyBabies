using Domain.Common;

namespace Domain.DataCollections.Forms;

/// <summary>
/// An item within a data source lookup table.
/// Can be hierarchical (parent-child) for nested categories.
/// </summary>
public class DataSourceItem : AuditableEntity<DataSourceItemId>
{
    /// <summary>
    /// Gets reference to the parent data source.
    /// </summary>
    public DataSourceId DataSourceId { get; private set; } = default!;
    public DataSource? DataSource { get; private set; }

    /// <summary>
    /// Gets parent item ID for hierarchical data sources.
    /// e.g., "Parent-infant relationships" is parent of category items.
    /// </summary>
    public DataSourceItemId? ParentItemId { get; private set; }
    public DataSourceItem? ParentItem { get; private set; }

    /// <summary>
    /// Gets stored value when selected.
    /// </summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// Gets display label shown to users.
    /// </summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>
    /// Gets order in which this item appears.
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// Gets a value indicating whether whether this item is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Gets optional JSON metadata for additional properties.
    /// e.g., { "icon": "heart", "color": "#ff0000" }.
    /// </summary>
    public string? Metadata { get; private set; }

    private DataSourceItem()
    {
    }

    private DataSourceItem(
        DataSourceItemId id,
        DataSourceId dataSourceId,
        string value,
        string label,
        int displayOrder,
        DataSourceItemId? parentItemId,
        string? metadata)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(value, nameof(value));
        Guard.NotNullOrEmptyOrWhiteSpace(label, nameof(label));

        Id = id;
        DataSourceId = dataSourceId;
        Value = value;
        Label = label;
        DisplayOrder = displayOrder;
        ParentItemId = parentItemId;
        Metadata = metadata;
    }

    internal static DataSourceItem Create(
        DataSourceId dataSourceId,
        string value,
        string label,
        int displayOrder,
        DataSourceItemId? parentItemId = null,
        string? metadata = null)
    {
        return new DataSourceItem(
            DataSourceItemId.New(),
            dataSourceId,
            value,
            label,
            displayOrder,
            parentItemId,
            metadata);
    }

    public void UpdateDetails(string value, string label, int displayOrder, string? metadata = null)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(value, nameof(value));
        Guard.NotNullOrEmptyOrWhiteSpace(label, nameof(label));

        Value = value;
        Label = label;
        DisplayOrder = displayOrder;
        Metadata = metadata;
    }

    public void SetParent(DataSourceItemId? parentId) => ParentItemId = parentId;

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}