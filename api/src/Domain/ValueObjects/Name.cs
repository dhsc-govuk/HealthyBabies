using Domain.Common;

namespace Domain.ValueObjects;

public class Name : ValueObject
{
    public Name(string firstName, string lastName)
    {
        Guard.NotNullOrEmptyOrWhiteSpace(firstName, nameof(firstName));
        Guard.NotNullOrEmptyOrWhiteSpace(lastName, nameof(lastName));
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    public string FirstName { get; private set; }
    public string LastName { get; private set; }

    public override string ToString()
    {
        return string.Join(" ", FirstName, LastName);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
    }
}