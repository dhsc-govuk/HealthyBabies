using Application.Common;
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

        // SMD04 is stored day-first, as the client emits it. Reading it with InvariantCulture was
        // month-first, so "03/10/2026" became 10 March and any day above 12 failed to parse at all —
        // silently dropping a closed service from the collection (BLUM-7521 in a quieter form).
        var closedDateValue = service.GetAnswer(ClosedDateQuestionCode)?.Value;
        if (!FormDateParser.TryParse(closedDateValue, out var closedDate))
        {
            return false;
        }

        return closedDate.Date >= collectionStartDate.Date;
    }
}