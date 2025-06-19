using Duende.IdentityModel.Client;

namespace OneGround.ZGW.Common.Authentication;

public class ZgwAuthDiscoveryCache : IZgwAuthDiscoveryCache
{
    public ZgwAuthDiscoveryCache(IDiscoveryCache discoveryCache)
    {
        DiscoveryCache = discoveryCache;
    }

    public IDiscoveryCache DiscoveryCache { get; }
}
