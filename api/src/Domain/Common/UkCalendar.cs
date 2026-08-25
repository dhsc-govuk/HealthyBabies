namespace Domain.Common;

// Deadlines and resubmission dates represent a UK calendar date, not a precise instant. Postgres
// round-trips them as UTC, and UTC vs UK local time (BST) disagree on the calendar date for the
// last hour of each UK day, so every comparison against "today" must go through UK local time
// rather than taking .Date on a raw UTC value, or the date silently shifts by a day near midnight.
public static class UkCalendar
{
    private static readonly TimeZoneInfo UkTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

    public static DateTime ToUkDate(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, UkTimeZone).Date;
    }
}