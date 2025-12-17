namespace OneGround.ZGW.Notificaties.Messaging.Configuration;

internal class HangfireConfiguration
{
    public TimeSpan[] RetryScheduleTimeSpanList { get; private set; }

    public string RetrySchedule
    {
        set
        {
            var val = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(TimeSpan.Parse).ToArray();
            AssurValid(val);
            RetryScheduleTimeSpanList = val;
        }
    }

    public string ExpireFailedJobsScanAt { get; set; } = "05:00"; // UTC
    public TimeSpan ExpireFailedJobAfter { get; set; } = TimeSpan.FromDays(7);

    private static void AssurValid(TimeSpan[] value)
    {
        // Note: Timespan serie sometimes difficult to understand. You can have 00:00:00:05 or 00:00:05 or 1.00:00:00 but the ordering is important for correct working
        TimeSpan previous = default;
        foreach (var current in value)
        {
            if (previous.TotalSeconds >= current.TotalSeconds)
            {
                throw new InvalidOperationException(
                    $"{nameof(RetrySchedule)} of {nameof(HangfireConfiguration)} should be an ordered set of timespans."
                );
            }

            previous = current;
        }
    }
}
