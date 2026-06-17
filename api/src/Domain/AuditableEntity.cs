using Domain.Users;

namespace Domain;

/// <summary>
/// Base class for entities that require audit tracking (created/modified).
/// For soft delete functionality, also implement ISoftDelete.
/// </summary>
public abstract class AuditableEntity<TKey> : IEntity<TKey>
{
    public TKey Id { get; protected set; } = default!;
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }
    public UserId? CreatedById { get; protected set; }
    public User? CreatedBy { get; protected set; }
    public UserId? LastModifiedById { get; protected set; }
    public User? LastModifiedBy { get; protected set; }

    /// <summary>
    /// Sets the creation audit fields.
    /// </summary>
    public void SetCreated(UserId createdBy, DateTime? createdAt = null)
    {
        CreatedById = createdBy;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the modification audit fields.
    /// </summary>
    public void SetModified(UserId modifiedBy, DateTime? modifiedAt = null)
    {
        LastModifiedById = modifiedBy;
        UpdatedAt = modifiedAt ?? DateTime.UtcNow;
    }
}