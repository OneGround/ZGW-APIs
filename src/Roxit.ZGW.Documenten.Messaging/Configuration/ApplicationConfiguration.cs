using System;

namespace Roxit.ZGW.Documenten.Messaging.Configuration;

public class ApplicationConfiguration
{
    public string EnabledForRsins { get; set; } = "";
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan OlderThanDuration { get; set; } = TimeSpan.FromMinutes(60);
    public int BatchSize { get; set; } = 20;
}
