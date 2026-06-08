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

    public static string CreateRecurringMinuteInterval(int intervalInMinutes)
    {
        // Generate a *recurring* Cron that fires every N minutes (a "*/N" minute step),
        // as opposed to CreateOneTimeCron which matches a single instant. A one-time cron
        // is fragile: if its single matching instant is missed (the Hangfire server is down,
        // e.g. during a Consul key rotation) or the run fails after exhausting its retries,
        // the next occurrence is ~a year away and the job is effectively stuck. A recurring
        // interval keeps firing on a fixed cadence, so Hangfire automatically restarts it
        // shortly after the server recovers.
        //
        // The minute field only supports 0-59, so clamp the interval to [1, 59]. Renewing a
        // little more often than strictly required is harmless; never renewing is what breaks
        // the token-renewal jobs.
        var interval = Math.Clamp(intervalInMinutes, 1, 59);

        // Format: Minute Hour DayOfMonth Month DayOfWeek
        return $"*/{interval} * * * *";
    }
}
