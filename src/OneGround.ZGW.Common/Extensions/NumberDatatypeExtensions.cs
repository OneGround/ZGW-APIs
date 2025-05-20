using System;

namespace OneGround.ZGW.Common.Extensions;

public static class NumberDatatypeExtensions
{
    public static string ToReadableFileSize(this long input)
    {
        if (input <= 1E3 - 1)
        {
            return $"{input} bytes";
        }

        if (input < 1E6 - 1)
        {
            return $"{Math.Round(input / 1E3, 1)} KB";
        }

        return $"{Math.Round(input / 1E6, 1)} MB";
    }

    public static string ToReadableTime(this double msec)
    {
        var ts = TimeSpan.FromMilliseconds(msec);

        if ((int)ts.TotalSeconds < 3)
        {
            return $"{Math.Round(msec, 0)} milliseconds";
        }

        if ((int)ts.TotalMinutes < 3)
        {
            return $"{Math.Round(msec / 1000.0, 0)} seconds";
        }

        return $"{Math.Round(msec / 1000.0 / 60.0, 0)} minutes";
    }
}
