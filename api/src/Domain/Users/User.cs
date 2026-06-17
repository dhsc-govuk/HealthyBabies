using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Users;

public class User : SoftDeletableEntity<UserId>
{
    public Name Name { get; private set; }
    public string Email { get; private set; }
    public SubId SubId { get; private set; }
    public bool IsActive { get; private set; }
    public UserRole Role { get; private set; }

    private User(
        UserId id,
        Name name,
        string email,
        SubId subId,
        bool isActive,
        UserRole role)
    {
        Id = id;
        Name = name;
        Email = email;
        SubId = subId;
        IsActive = isActive;
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }

    public static User New(UserId id, Name name, string email, SubId subId, bool isActive, UserRole role)
    {
        Guard.NotNull(id, nameof(id));
        Guard.NotNull(name, nameof(name));
        Guard.NotNullOrEmpty(email, nameof(email));
        Guard.NotNull(subId, nameof(subId));
        Guard.NotNull(role, nameof(role));

        return new User(id, name, email, subId, isActive, role);
    }

    public void UpdateDetails(Name name, string email, bool isActive, UserRole? role = null)
    {
        Guard.NotNull(name, nameof(name));
        Guard.NotNullOrEmpty(email, nameof(email));

        Name = name;
        Email = email;
        IsActive = isActive;
        Role = role ?? Role;
        UpdatedAt = DateTime.UtcNow;
    }

    public override void Delete(UserId deletedBy)
    {
        base.Delete(deletedBy);
        IsActive = false;
    }

    /// <summary>
    /// Reactivates a soft-deleted user with new details and resets MFA requirement.
    /// Used when creating a user with an email that was previously soft-deleted.
    /// </summary>
    public void Reactivate(Name name, SubId subId, bool isActive, UserRole role)
    {
        Guard.NotNull(name, nameof(name));
        Guard.NotNull(subId, nameof(subId));
        Guard.NotNull(role, nameof(role));

        this.Restore();
        Name = name;
        SubId = subId;
        IsActive = isActive;
        Role = role;
        UpdatedAt = DateTime.UtcNow;
    }
}