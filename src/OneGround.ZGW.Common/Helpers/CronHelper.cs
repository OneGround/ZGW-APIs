using System;

namespace OneGround.ZGW.Common.Helpers;

public static class CronHelper
{
    public static string CreateCronForIntervalMinutes(int minutes)
    {
        if (minutes <= 0)
            minutes = 1;
        if (minutes < 60)
        {
            return $"*/{minutes} * * * *";
        }
        var hours = minutes / 60;
        if (minutes % 60 == 0 && hours < 24)
        {
            return $"0 */{hours} * * *";
        }
        var days = Math.Max(1, minutes / (60 * 24));
        return $"0 0 */{days} * *";
    }
}
