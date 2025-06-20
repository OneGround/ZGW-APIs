using Duende.IdentityModel.Client;

namespace OneGround.ZGW.Common.Authentication;

public interface IZgwAuthDiscoveryCache
{
    IDiscoveryCache DiscoveryCache { get; }
}
