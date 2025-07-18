using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OneGround.ZGW.Common.Logging;

namespace OneGround.ZGW.Common.Configuration;

public static class ZgwListenerHostConfigurationExtensions
{
    public static IHostApplicationBuilder ConfigureHostDefaults(this IHostApplicationBuilder builder, string serviceName)
    {
        var logFileName = $"zgw-{serviceName.ToLower().Replace("_", "-")}.log";

        builder.UseAndConfigureSerilog(serviceName, logFileName);
        builder.SetConfigurationProviders();

        if (builder.Environment.IsInternal())
        {
            builder.ConfigureContainer(
                new DefaultServiceProviderFactory(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true })
            );
        }

        return builder;
    }
}
