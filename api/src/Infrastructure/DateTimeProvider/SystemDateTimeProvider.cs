using Application.Common.Interfaces;

namespace Infrastructure.DateTimeProvider;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime GetNow() => DateTime.Now;

    public DateTime GetUtcNow() => DateTime.UtcNow;
}