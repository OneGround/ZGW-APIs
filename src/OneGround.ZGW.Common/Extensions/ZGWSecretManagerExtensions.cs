using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.Services;

namespace OneGround.ZGW.Common.Extensions;

public static class ZGWSecretManagerExtensions
{
    public static IServiceCollection AddZGWSecretManager(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ZgwServiceAccountConfiguration>(configuration.GetSection("ZgwServiceAccountCredentials"));
        return services.AddSingleton<ICachedZGWSecrets, CachedZGWSecrets>();
    }
}
