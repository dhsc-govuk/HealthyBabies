using Domain.Common;

namespace Domain.ValueObjects;

public static class Role
{
    public const string Admin = "admin";
    public const string OrganisationAdmin = "organisation admin";
}

public class UserRole : ValueObject
{
    public string Value { get; private set; }

    private UserRole(string role)
    {
        Value = role;
    }

    public static UserRole From(string value)
    {
        var role = new UserRole(value);

        if (!SupportedRoles.Contains(role))
        {
            throw new UnsupportedUserRoleException(value);
        }

        return role;
    }

    public static UserRole Admin => new(Role.Admin);
    public static UserRole OrganisationAdmin => new(Role.OrganisationAdmin);

    public static implicit operator string(UserRole role)
    {
        return role.ToString();
    }

    public static explicit operator UserRole(string value)
    {
        return From(value);
    }

    public override string ToString()
    {
        return Value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    private static IEnumerable<UserRole> SupportedRoles
    {
        get
        {
            yield return Admin;
            yield return OrganisationAdmin;
        }
    }
}

public class UnsupportedUserRoleException(string value) : Exception($"UserRole \"{value}\" is not supported.");