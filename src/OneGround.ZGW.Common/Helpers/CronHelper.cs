using System;
using System.Linq;

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

    // Divisors of 60 (excluding 60 itself) in descending order. A "*/N" minute step only fires
    // on a strictly even cadence when N divides 60 evenly; otherwise the runs bunch up around the
    // hour boundary (e.g. "*/50" fires at :00 and :50, so the :50 -> :00 gap is only 10 minutes).
    private static readonly int[] EvenMinuteIntervals = [30, 20, 15, 12, 10, 6, 5, 4, 3, 2, 1];

    public static string CreateRecurringMinuteInterval(int maxMinutesBetweenRuns)
    {
        // Generate a *recurring* Cron ("*/N" minute step), as opposed to CreateOneTimeCron
        // which matches a single instant. A one-time cron is fragile: if its single matching
        // instant is missed (the Hangfire server is down, e.g. during a Consul key rotation)
        // or the run fails after exhausting its retries, the next occurrence is ~a year away
        // and the job is effectively stuck. A recurring step keeps firing, so Hangfire
        // automatically restarts it shortly after the server recovers.
        //
        // Snap the interval to the largest divisor of 60 that is <= maxMinutesBetweenRuns. This
        // gives two guarantees:
        //   1. The chosen N divides 60, so "*/N" fires on a strictly even cadence (no bunching
        //      around the hour boundary).
        //   2. We round DOWN, never up, so the gap between runs never exceeds the requested
        //      maximum — the token is always renewed before it expires. Renewing a little more
        //      often than strictly required is harmless; never renewing is what breaks the job.
        // Values below 1 fall back to every minute. The effective ceiling is 30 minutes (the
        // largest divisor of 60 below 60), which is well within any realistic token lifetime.
        var interval = EvenMinuteIntervals.FirstOrDefault(d => d <= maxMinutesBetweenRuns, 1);

        // Format: Minute Hour DayOfMonth Month DayOfWeek
        return $"*/{interval} * * * *";
    }
}
