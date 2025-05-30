using Roxit.ZGW.Common.Messaging.Configuration;

namespace Roxit.ZGW.Documenten.Messaging.Configuration;

internal class DocumentenEventBusConfiguration : EventBusConfiguration
{
    public string ReceiveQueue { get; set; }
    public ushort ReceivePrefetchCount { get; set; } = 16;
    public int ReceiveEndpointTimeout { get; set; } = 8;
    public int ReceiveEndpointRetries { get; set; } = 1;
}
