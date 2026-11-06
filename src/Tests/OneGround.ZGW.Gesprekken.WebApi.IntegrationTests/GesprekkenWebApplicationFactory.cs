using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Afsluiten;
using OneGround.ZGW.Gesprekken.Web.Bestanden;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.IntegrationTests.Common.Containers;

namespace OneGround.ZGW.Gesprekken.WebApi.IntegrationTests;

/// <summary>
/// Boots the Gesprekken API in-process, with the Zaken API stubbed.
/// </summary>
internal sealed class GesprekkenWebApplicationFactory : ZgwWebApplicationFactory<Program>
{
    public const string ZakenApi = "https://zaken.integrationtest.invalid/api/v1/zaken/";

    public GesprekkenWebApplicationFactory(IntegrationTestContainers containers)
        : base(containers)
    {
        var fallback = OutboundHttp.Responder;
        OutboundHttp.Responder = request => TryRespondAsZakenApi(request) ?? fallback(request);
    }

    /// <summary>
    /// Zaak URLs the stubbed Zaken API knows, with their einddatum (null for an open zaak).
    /// </summary>
    public ConcurrentDictionary<string, string> Zaken { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string NewZaak(bool closed = false)
    {
        var url = ZakenApi + Guid.NewGuid();
        Zaken[url] = closed ? "2026-06-28" : null;
        return url;
    }

    public ConcurrentQueue<Guid> Klantcontacten { get; } = new();

    public InMemoryBestandOpslag Opslag { get; } = new();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.Replace(ServiceDescriptor.Scoped<IKlantcontactService>(_ => new RecordingKlantcontactService(Klantcontacten)));
        services.Replace(ServiceDescriptor.Singleton<IBestandOpslag>(Opslag));
    }

    /// <summary>
    /// Stands in for the object storage. <see cref="Upload"/> does what the client's PUT to the upload URL would do.
    /// </summary>
    internal sealed class InMemoryBestandOpslag : IBestandOpslag
    {
        private const string Root = "https://opslag.integrationtest.invalid/";

        public ConcurrentDictionary<string, long> Objecten { get; } = new();

        public void Upload(string uploadUrl, long omvang) => Objecten[uploadUrl[Root.Length..].Split('?')[0]] = omvang;

        public Task<string> GetUploadUrlAsync(string sleutel, string contentType, DateTime verlooptOp) =>
            Task.FromResult($"{Root}{sleutel}?signature=upload");

        public Task<string> GetDownloadUrlAsync(string sleutel, string bestandsnaam, DateTime verlooptOp) =>
            Task.FromResult($"{Root}{sleutel}?signature=download");

        public Task<long?> GetOmvangAsync(string sleutel, CancellationToken cancellationToken) =>
            Task.FromResult(Objecten.TryGetValue(sleutel, out var omvang) ? omvang : (long?)null);

        public Task VerwijderAsync(string sleutel, CancellationToken cancellationToken)
        {
            Objecten.TryRemove(sleutel, out _);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingKlantcontactService(ConcurrentQueue<Guid> klantcontacten) : IKlantcontactService
    {
        public Task<string> MaakAanAsync(Gesprek gesprek, CancellationToken cancellationToken)
        {
            klantcontacten.Enqueue(gesprek.Id);
            return Task.FromResult($"https://klantinteracties.integrationtest.invalid/api/v1/klantcontacten/{gesprek.Id}");
        }
    }

    private HttpResponseMessage TryRespondAsZakenApi(HttpRequestMessage request)
    {
        var url = request.RequestUri!.GetLeftPart(UriPartial.Path);
        if (!url.StartsWith(ZakenApi, StringComparison.OrdinalIgnoreCase))
            return null;

        if (!Zaken.TryGetValue(url, out var einddatum))
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(
                    """{"code":"not_found","title":"Niet gevonden.","status":404}""",
                    Encoding.UTF8,
                    "application/problem+json"
                ),
            };

        var json = einddatum == null ? $$"""{"url":"{{url}}","einddatum":null}""" : $$"""{"url":"{{url}}","einddatum":"{{einddatum}}"}""";
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
