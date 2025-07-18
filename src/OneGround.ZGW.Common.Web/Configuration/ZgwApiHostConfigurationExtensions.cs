using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using OneGround.ZGW.Common.Configuration;

namespace OneGround.ZGW.Common.Web.Configuration;

public static class ZgwApiHostConfigurationExtensions
{
    public static WebApplicationBuilder ConfigureZgwWebHostDefaults(this WebApplicationBuilder builder, string serviceName)
    {
        builder.ConfigureHostDefaults(serviceName);

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
        });

        return builder;
    }
}
