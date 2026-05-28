using Domain.Common;

namespace Domain.DataCollections.Forms.ValueObjects;

public static class SubmissionStatusConstants
{
    public const string Draft = "draft";
    public const string Submitted = "submitted";
    public const string UnderReview = "under_review";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string RequiresChanges = "requires_changes";
    public const string Cancelled = "cancelled";
}

/// <summary>
/// Status of a form submission.
/// </summary>
public class SubmissionStatus : ValueObject
{
    public string Value { get; private set; }

    private SubmissionStatus(string value)
    {
        Value = value;
    }

    public static SubmissionStatus From(string value)
    {
        var status = new SubmissionStatus(value);

        if (!SupportedStatuses.Contains(status))
        {
            throw new UnsupportedSubmissionStatusException(value);
        }

        return status;
    }

    public static SubmissionStatus Draft => new(SubmissionStatusConstants.Draft);
    public static SubmissionStatus Submitted => new(SubmissionStatusConstants.Submitted);
    public static SubmissionStatus UnderReview => new(SubmissionStatusConstants.UnderReview);
    public static SubmissionStatus Approved => new(SubmissionStatusConstants.Approved);
    public static SubmissionStatus Rejected => new(SubmissionStatusConstants.Rejected);
    public static SubmissionStatus RequiresChanges => new(SubmissionStatusConstants.RequiresChanges);
    public static SubmissionStatus Cancelled => new(SubmissionStatusConstants.Cancelled);

    public static implicit operator string(SubmissionStatus status)
    {
        return status.ToString();
    }

    public static explicit operator SubmissionStatus(string value)
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

    private static IEnumerable<SubmissionStatus> SupportedStatuses
    {
        get
        {
            yield return Draft;
            yield return Submitted;
            yield return UnderReview;
            yield return Approved;
            yield return Rejected;
            yield return RequiresChanges;
            yield return Cancelled;
        }
    }
}

public class UnsupportedSubmissionStatusException(string value) : Exception($"SubmissionStatus \"{value}\" is not supported.");