using Domain.Users;

namespace Domain;

/// <summary>
/// Base class for entities that require both audit tracking and soft delete functionality.
/// Extends AuditableEntity and implements ISoftDelete.
/// </summary>
public abstract class SoftDeletableEntity<TKey> : AuditableEntity<TKey>, ISoftDelete
{
    public bool IsDeleted { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }
    public UserId? DeletedById { get; protected set; }
    public User? DeletedBy { get; protected set; }

    /// <summary>
    /// Marks the entity as soft deleted.
    /// </summary>
    public virtual void Delete(UserId deletedBy)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedById = deletedBy;
        DeletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Restores a soft-deleted entity.
    /// </summary>
    public virtual void Restore()
    {
        if (!IsDeleted)
        {
            return;
        }

        IsDeleted = false;
        DeletedById = null;
        DeletedAt = null;
        DeletedBy = null;
    }
}