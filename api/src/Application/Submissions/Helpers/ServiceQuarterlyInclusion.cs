using System.Globalization;
using Domain.Services;

namespace Application.Submissions.Helpers;

public static class ServiceQuarterlyInclusion
{
    private const string StatusQuestionCode = "SMD03";
    private const string ClosedDateQuestionCode = "SMD04";
    private const string NoLongerOfferedValue = "2";

    public static bool IsIncludedInQuarterlyServiceUsers(Service service, DateTime collectionStartDate)
    {
        var statusValue = service.GetAnswer(StatusQuestionCode)?.Value;
        if (statusValue != NoLongerOfferedValue)
        {
            return true;
        }

        var closedDateValue = service.GetAnswer(ClosedDateQuestionCode)?.Value;
        if (!DateTime.TryParse(closedDateValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var closedDate))
        {
            return false;
        }

        return closedDate.Date >= collectionStartDate.Date;
    }
}