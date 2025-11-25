using System;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;

public static class ConfigureForwardedHeadersServiceCollectionExtensions
{
    public static IServiceCollection ConfigureForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;

            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();

            var knownNetworks = configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];
            if (knownNetworks.Length != 0)
            {
                foreach (var network in knownNetworks)
                {
                    var parts = network.Split('/');

                    if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var ipAddress) && int.TryParse(parts[1], out var prefixLength))
                    {
                        options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(ipAddress, prefixLength));
                    }
                    else
                    {
                        throw new FormatException(
                            $"Invalid network format in 'ForwardedHeaders:KnownNetworks': '{network}'. Expected format is '[IPAddress]/[PrefixLength]'."
                        );
                    }
                }
            }

            var knownProxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
            if (knownProxies.Length != 0)
            {
                foreach (var proxy in knownProxies)
                {
                    options.KnownProxies.Add(IPAddress.Parse(proxy));
                }
            }

            var resolverForwardedHeader = configuration.GetValue("Application:ResolveForwardedHost", false);
            if (resolverForwardedHeader)
            {
                options.ForwardedHeaders |= ForwardedHeaders.XForwardedHost;
            }
        });

        return services;
    }
}
