using System;
using PowerPositionService.Core.Interfaces;

namespace PowerPositionService.Core.Services;

public class DateTimeProvider : IDateTimeProvider
{
    private static readonly TimeZoneInfo LondonTimeZone;

    static DateTimeProvider()
    {
        // Handle both Windows and Linux timezone names
        try
        {
            LondonTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            LondonTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
        }
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime LondonNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, LondonTimeZone);

    public DateTime ConvertToLondon(DateTime utcDateTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, LondonTimeZone);
    }
}
