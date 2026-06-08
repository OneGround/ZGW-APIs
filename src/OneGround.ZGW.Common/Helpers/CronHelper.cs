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

    public static string CreateRecurringMinuteInterval(int maxMinutesBetweenRuns)
    {
        // Generate a *recurring* Cron ("*/N" minute step), as opposed to CreateOneTimeCron
        // which matches a single instant. A one-time cron is fragile: if its single matching
        // instant is missed (the Hangfire server is down, e.g. during a Consul key rotation)
        // or the run fails after exhausting its retries, the next occurrence is ~a year away
        // and the job is effectively stuck. A recurring step keeps firing, so Hangfire
        // automatically restarts it shortly after the server recovers.
        //
        // Note on "*/N" semantics: it does NOT fire at a strictly fixed N-minute cadence. It
        // fires at minutes 0, N, 2N, ... within each hour and then resets at the top of the
        // hour, so when N does not divide 60 the runs bunch up around the hour boundary (e.g.
        // "*/50" fires at :00 and :50, so the :50 -> :00 gap is only 10 minutes). The property
        // we rely on is the *upper bound*: the gap between consecutive runs never exceeds N
        // minutes. That is exactly what a renew-before-expiry job needs — bunching only ever
        // makes the renewal fire sooner, never later, which is harmless. So N is the MAXIMUM
        // minutes the token may go un-renewed, not a guaranteed exact interval.
        //
        // The minute field only supports 0-59, so clamp to [1, 59]. Renewing a little more
        // often than strictly required is harmless; never renewing is what breaks these jobs.
        var interval = Math.Clamp(maxMinutesBetweenRuns, 1, 59);

        // Format: Minute Hour DayOfMonth Month DayOfWeek
        return $"*/{interval} * * * *";
    }
}
