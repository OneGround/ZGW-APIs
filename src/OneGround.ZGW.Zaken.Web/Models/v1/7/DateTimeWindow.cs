using System;
using System.Globalization;

namespace OneGround.ZGW.Zaken.Web.Models.v1._7;

/// <summary>
/// The period an exact date(time) filter matches: <see cref="From"/> up to, but excluding, <see cref="Until"/> (UTC).
/// Stored timestamps have a precision of microseconds, so an exact filter can only match on the precision the caller gave.
/// </summary>
public record DateTimeWindow(DateTime From, DateTime Until)
{
    /// <summary>
    /// A date without a time (2026-10-23) covers that whole day, a date with a time covers that whole second.
    /// Returns null when no value is given.
    /// </summary>
    public static DateTimeWindow FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var parsed = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        var hasTime = value.Contains(':') || value.Contains('T') || value.Contains('t');
        if (!hasTime)
            return new DateTimeWindow(parsed.Date, parsed.Date.AddDays(1));

        var from = new DateTime(parsed.Ticks - parsed.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        return new DateTimeWindow(from, from.AddSeconds(1));
    }
}
