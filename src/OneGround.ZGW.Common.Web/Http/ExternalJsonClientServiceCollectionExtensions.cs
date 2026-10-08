using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

        // Note: warns at startup when the protection against server-side request forgery has been switched off.
        services.AddHostedService<ExternalJsonClientStartupCheck>();

        services.AddHttpContextAccessor();

        services
            .AddHttpClient(ExternalJsonClient.HttpClientName)
            // Note: the factory's own request logging writes the full url, which may carry a secret in its query string.
            .RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(sp => CreateHandler(sp.GetRequiredService<IOptions<ExternalJsonClientSettings>>().Value));

        // Note: scoped on purpose, the client holds the per-request cache and time budget.
        services.AddScoped<IExternalJsonClient, ExternalJsonClient>();

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
        if (addresses.Length == 0)
            throw new HttpRequestException($"The host '{context.DnsEndPoint.Host}' did not resolve to any address.");

        if (!allowPrivateAddresses && Array.Exists(addresses, a => !ExternalUrlPolicy.IsPublicAddress(a)))
            throw new BlockedAddressException(context.DnsEndPoint.Host);

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

/// <summary>
/// The host resolved to an address that is not public. Kept apart from an ordinary connection failure because it may be an attempt at
/// server-side request forgery, and so deserves its own log event.
/// </summary>
public class BlockedAddressException : HttpRequestException
{
    public BlockedAddressException(string host)
        : base($"The host '{host}' resolves to an address that is not allowed.") { }
}

internal sealed class ExternalJsonClientStartupCheck : IHostedService
{
    private readonly ExternalJsonClientSettings _settings;
    private readonly ILogger<ExternalJsonClientStartupCheck> _logger;

    public ExternalJsonClientStartupCheck(IOptions<ExternalJsonClientSettings> settings, ILogger<ExternalJsonClientStartupCheck> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_settings.AllowPrivateAddresses)
            _logger.LogWarning("External JSON client: AllowPrivateAddresses is enabled, the protection against server-side request forgery is off");

        foreach (var entry in _settings.AllowedHosts)
        {
            if (!ExternalUrlPolicy.IsValidHostEntry(entry))
                _logger.LogWarning("External JSON client: AllowedHosts entry '{Entry}' is not a valid host name and will never match", entry);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
