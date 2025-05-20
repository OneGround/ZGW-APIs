using OneGround.ZGW.Common.Messaging.Configuration;

namespace OneGround.ZGW.Documenten.Messaging.Configuration;

internal class DocumentenEventBusConfiguration : EventBusConfiguration
{
    public string ReceiveQueue { get; set; }
    public ushort ReceivePrefetchCount { get; set; } = 16;
    public int ReceiveEndpointTimeout { get; set; } = 8;
    public int ReceiveEndpointRetries { get; set; } = 1;
}
