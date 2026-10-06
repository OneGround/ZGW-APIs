using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Common.Web.Configuration;

namespace OneGround.ZGW.Common.Web.Http;

public static class ExternalJsonClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IExternalJsonClient"/>, configured from <paramref name="configuration"/> (<see cref="ExternalJsonClientSettings"/>).
    /// </summary>
    public static IServiceCollection AddExternalJsonClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ExternalJsonClientSettings>(configuration);

        services
            .AddHttpClient(ExternalJsonClient.HttpClientName)
            // Note: the factory's own request logging writes the full url, which may carry a secret in its query string.
            .RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(sp => CreateHandler(sp.GetRequiredService<IOptions<ExternalJsonClientSettings>>().Value));

        services.AddSingleton<IExternalJsonClient, ExternalJsonClient>();

        return services;
    }

    /// <summary>
    /// The handler checks the addresses a host name resolves to at connect time (so DNS rebinding cannot slip an internal address past
    /// the url check), follows no redirects (a redirect could point to an internal address or a host that is not allowed) and bypasses
    /// any proxy (which would otherwise be the one being connected to).
    /// </summary>
    public static SocketsHttpHandler CreateHandler(ExternalJsonClientSettings settings) =>
        new()
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            UseCookies = false,
            ConnectCallback = (context, cancellationToken) => ConnectAsync(context, settings.AllowPrivateAddresses, cancellationToken),
        };

    private static async ValueTask<Stream> ConnectAsync(
        SocketsHttpConnectionContext context,
        bool allowPrivateAddresses,
        CancellationToken cancellationToken
    )
    {
        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);

        // Note: refuse when ANY resolved address is non-public, not just the one that would be picked.
        if (addresses.Length == 0 || (!allowPrivateAddresses && Array.Exists(addresses, a => !ExternalUrlPolicy.IsPublicAddress(a))))
            throw new HttpRequestException("The host resolves to an address that is not allowed.");

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
