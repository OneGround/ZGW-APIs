using System;

namespace OneGround.ZGW.Common.Helpers;

public static class CronHelper
{
    public static string CreateOneTimeCron(int minutesFromNow)
    {
        // 1. Calculate the exact future time
        var targetTime = DateTime.UtcNow.AddMinutes(minutesFromNow);

        // 2. Generate a Cron that matches that specific Minute/Hour/Day
        // Format: Minute Hour DayOfMonth Month DayOfWeek
        return $"{targetTime.Minute} {targetTime.Hour} {targetTime.Day} {targetTime.Month} *";
    }
}
