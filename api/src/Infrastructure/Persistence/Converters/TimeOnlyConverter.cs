using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Converters;

public class TimeOnlyConverter() : ValueConverter<TimeOnly, TimeSpan>(timeOnly => timeOnly.ToTimeSpan(),
    timeSpan => TimeOnly.FromTimeSpan(timeSpan));