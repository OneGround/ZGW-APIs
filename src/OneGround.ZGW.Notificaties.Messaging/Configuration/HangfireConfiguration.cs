namespace OneGround.ZGW.Notificaties.Messaging.Configuration;

internal class HangfireConfiguration
{
    private TimeSpan[] _scheduledRetries;

    public TimeSpan[] ScheduledRetries
    {
        get { return _scheduledRetries; }
        set
        {
            AssurValid(value);
            _scheduledRetries = value;
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
                    $"{nameof(ScheduledRetries)} of {nameof(HangfireConfiguration)} should be an ordered set of timespans."
                );
            }
            previous = current;
        }
    }
}
