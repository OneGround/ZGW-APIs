using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Configuration;
using OneGround.ZGW.Common.Web.Http;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Http;

/// <summary>
/// Exercises the real connect-time guard against a listener on the loopback interface.
/// </summary>
public class ExternalJsonClientHandlerTests
{
    [Fact]
    public async Task Handler_LoopbackAddress_IsRefusedByDefault()
    {
        using var server = LoopbackServer.Start();
        using var client = new HttpClient(ExternalJsonClientServiceCollectionExtensions.CreateHandler(new ExternalJsonClientSettings()));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync($"http://127.0.0.1:{server.Port}/"));

        // Note: SocketsHttpHandler wraps what its ConnectCallback throws; the client relies on finding it as the inner exception
        Assert.IsType<BlockedAddressException>(exception.InnerException);

        Assert.Equal(0, server.Connections);
    }

    [Fact]
    public async Task Handler_LocalhostName_IsRefusedByDefault()
    {
        using var server = LoopbackServer.Start();
        using var client = new HttpClient(ExternalJsonClientServiceCollectionExtensions.CreateHandler(new ExternalJsonClientSettings()));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync($"http://localhost:{server.Port}/"));

        Assert.Equal(0, server.Connections);
    }

    [Fact]
    public async Task Handler_LoopbackAddress_IsAllowedWhenPrivateAddressesAreEnabled()
    {
        using var server = LoopbackServer.Start();
        using var client = new HttpClient(
            ExternalJsonClientServiceCollectionExtensions.CreateHandler(new ExternalJsonClientSettings { AllowPrivateAddresses = true })
        );

        var response = await client.GetAsync($"http://127.0.0.1:{server.Port}/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"ok":true}""", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Handler_DoesNotFollowRedirects()
    {
        using var server = LoopbackServer.Start(redirectTo: "http://169.254.169.254/latest/meta-data");
        using var client = new HttpClient(
            ExternalJsonClientServiceCollectionExtensions.CreateHandler(new ExternalJsonClientSettings { AllowPrivateAddresses = true })
        );

        var response = await client.GetAsync($"http://127.0.0.1:{server.Port}/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private sealed class LoopbackServer : System.IDisposable
    {
        private readonly TcpListener _listener;
        private readonly string _redirectTo;

        private LoopbackServer(string redirectTo)
        {
            _redirectTo = redirectTo;
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            _ = AcceptLoopAsync();
        }

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
        public int Connections { get; private set; }

        public static LoopbackServer Start(string redirectTo = null) => new(redirectTo);

        private async Task AcceptLoopAsync()
        {
            try
            {
                while (true)
                {
                    using var tcp = await _listener.AcceptTcpClientAsync();
                    Connections++;

                    var stream = tcp.GetStream();
                    await stream.ReadAsync(new byte[4096]);

                    var response = _redirectTo is null
                        ? "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: 11\r\nConnection: close\r\n\r\n{\"ok\":true}"
                        : $"HTTP/1.1 302 Found\r\nLocation: {_redirectTo}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";

                    await stream.WriteAsync(Encoding.ASCII.GetBytes(response));
                }
            }
            catch
            {
                // Listener stopped.
            }
        }

        public void Dispose() => _listener.Stop();
    }
}
