using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NodaTime;

namespace OneGround.ZGW.Common.Helpers;

public static class ProfileHelper
{
    public static string Fix0Period(Period period)
    {
        if (period == null)
            return null;

        // Note: Fixes the issue when the Period has "no period" representation will be "D" and is stored as "00:00:00". In this case we return valid Period as "P0D"
        return period == Period.Zero ? "P0D" : period.ToString();
    }

    public static string StringDateFromDate(DateOnly? date)
    {
        return date?.ToString("yyyy-MM-dd");
    }

    public static string StringDateFromDateTime(DateTime? date, bool withTime = false)
    {
        return date.HasValue ? StringDateFromDateTime(date.Value, withTime) : null;
    }

    public static string StringDateFromDateTime(DateTime date, bool withTime = false)
    {
        if (date == default)
        {
            return null;
        }

        return withTime
            ? date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) + "Z"
            : date.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static bool? BooleanFromString(string boolean)
    {
        if (string.IsNullOrWhiteSpace(boolean))
            return null;

        var result = bool.Parse(boolean);

        return result;
    }

    public static DateTime? DateTimeFromString(string date)
    {
        if (string.IsNullOrWhiteSpace(date))
            return null;

        var result = DateTime.Parse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        //var result2 = DateTime.Parse(date);
        //if (result.Kind is DateTimeKind.Utc)
        return result;

        //result = DateTime.SpecifyKind(result, DateTimeKind.Utc);
        //return result;
    }

    public static DateOnly DateFromString(string date)
    {
        if (string.IsNullOrWhiteSpace(date))
            throw new InvalidOperationException("Date is required");

        if (date.Length != 10)
            throw new InvalidOperationException($"{date} is not in a valid format (yyyy-mm-dd).");

        return DateOnly.Parse(date, CultureInfo.InvariantCulture);
    }

    public static DateOnly? DateFromStringOptional(string date)
    {
        if (string.IsNullOrWhiteSpace(date))
            return null;

        if (date.Length != 10)
            throw new InvalidOperationException($"{date} is not in a valid format (yyyy-mm-dd).");

        return DateOnly.Parse(date, CultureInfo.InvariantCulture);
    }

    public static DateOnly? TryDateFromStringOptional(string date)
    {
        if (string.IsNullOrWhiteSpace(date))
            return null;

        if (date.Length != 10)
            return null;

        if (!DateOnly.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            return null;

        return result;
    }

    public static IEnumerable<string> ArrayFromString(string values)
    {
        if (string.IsNullOrWhiteSpace(values))
            return null;

        return values.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(p => p.Trim());
    }

    private static T EnumFromString<T>(string value)
        where T : struct
    {
        if (value == null)
            throw new ArgumentNullException(value);

        if (!Enum.TryParse(value.Trim(), out T result) || !Enum.IsDefined(typeof(T), result))
            throw new InvalidOperationException($"{typeof(T).Name} {value} not implemented.");

        return result;
    }

    public static IEnumerable<T> EnumArrayFromString<T>(string values)
        where T : struct
    {
        if (string.IsNullOrWhiteSpace(values))
            return [];

        return values.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(EnumFromString<T>);
    }

    public static string EmptyWhenNull(string value)
    {
        return value ?? string.Empty;
    }

    public static string Convert2letterTo3Letter(string taal, Dictionary<string, string> dictionary)
    {
        if (dictionary.TryGetValue(taal.ToLower(), out var result))
        {
            return result;
        }
        else
        {
            return taal.ToLower();
        }
    }
}
