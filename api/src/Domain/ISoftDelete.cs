using Domain.Users;

namespace Domain;

/// <summary>
/// Interface for entities that support soft delete functionality.
/// Implement this interface only on entities that require soft delete behavior.
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    UserId? DeletedById { get; }
    User? DeletedBy { get; }

    /// <summary>
    /// Marks the entity as deleted.
    /// </summary>
    void Delete(UserId deletedBy);

    /// <summary>
    /// Restores a soft-deleted entity.
    /// </summary>
    void Restore();
}