using Microsoft.Extensions.Hosting;
using OneGround.ZGW.Common.Logging;

namespace OneGround.ZGW.Common.Configuration;

public static class ZgwListenerHostConfigurationExtensions
{
    public static IHostBuilder ConfigureZgwListenerHostDefaults(this IHostBuilder builder, string serviceName)
    {
        var logFileName = $"zgw-{serviceName.ToLower().Replace("_", "-")}.log";

        builder.UseAndConfigureSerilog(serviceName, logFileName);
        builder.SetConfigurationProviders();

        builder.UseDefaultServiceProvider(
            (context, options) =>
            {
                if (context.HostingEnvironment.IsLocal())
                {
                    options.ValidateOnBuild = true;
                    options.ValidateScopes = true;
                }
            }
        );

        return builder;
    }
}
