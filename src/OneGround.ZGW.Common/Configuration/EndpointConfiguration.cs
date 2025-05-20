namespace OneGround.ZGW.Common.Configuration;

public class EndpointConfiguration
{
    public DiscoverableService ZRC { get; set; }
    public DiscoverableService ZTC { get; set; }
    public DiscoverableService AC { get; set; }
    public DiscoverableService BRC { get; set; }
    public DiscoverableService DRC { get; set; }
    public DiscoverableService NRC { get; set; }
    public DiscoverableService RL { get; set; }
}
