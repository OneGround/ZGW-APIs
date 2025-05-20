using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.Logging;

namespace OneGround.ZGW.Common.Web.Configuration;

public static class ZgwApiHostConfigurationExtensions
{
    public static WebApplicationBuilder ConfigureZgwWebHostDefaults(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Host.SetConfigurationProviders();
        builder.Host.UseAndConfigureSerilog(serviceName, $"zgw-{serviceName.ToLower()}-api.log");

        if (builder.Environment.IsLocal())
        {
            builder.Host.UseDefaultServiceProvider(o =>
            {
                o.ValidateOnBuild = true;
                o.ValidateScopes = true;
            });
        }

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
        });

        return builder;
    }
}
