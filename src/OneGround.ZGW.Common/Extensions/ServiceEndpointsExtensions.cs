using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.Services;

namespace OneGround.ZGW.Common.Extensions;

public static class ServiceEndpointsExtensions
{
    public static void AddServiceEndpoints(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddSingleton<IServiceDiscovery, EndpointsAppSettingsServiceDiscovery>();
        services.Configure<EndpointConfiguration>(configuration.GetSection("Services"));
    }
}
