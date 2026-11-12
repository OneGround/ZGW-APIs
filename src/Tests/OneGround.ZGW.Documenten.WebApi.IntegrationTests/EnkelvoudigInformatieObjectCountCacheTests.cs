using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Controllers;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.IntegrationTests.Common.Authentication;
using Xunit;
using Xunit.Abstractions;
using AutorisatieResponseDto = OneGround.ZGW.Autorisaties.Contracts.v1.Responses.AutorisatieResponseDto;

namespace OneGround.ZGW.Documenten.WebApi.IntegrationTests;

[Collection(DocumentenApiCollection.Name)]
public class EnkelvoudigInformatieObjectCountCacheTests
{
    private const string IotA = "https://catalogi.integrationtest.invalid/api/v1/informatieobjecttypen/aaaaaaaa-0000-0000-0000-00000000000a";
    private const string IotB = "https://catalogi.integrationtest.invalid/api/v1/informatieobjecttypen/bbbbbbbb-0000-0000-0000-00000000000b";
    private const int DocsTypeA = 150;
    private const int DocsTypeB = 3;

    private readonly DocumentenWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _out;

    public EnkelvoudigInformatieObjectCountCacheTests(DocumentenApiFixture fixture, ITestOutputHelper output)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
        _out = output;
    }

    [Fact]
    public async Task Baseline_privileged_client_alone_sees_all_documents_and_a_next_page()
    {
        const string rsin = TestRsins.D;
        await SeedAsync(rsin);
        var privileged = NewPrivilegedClient();

        var (status, count, next, results) = await ListAsync(privileged, rsin, page: 1);
        _out.WriteLine($"[baseline] privileged page 1 -> {status} count={count} results={results} next={next ?? "null"}");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(DocsTypeA + DocsTypeB, count);
        Assert.NotNull(next);
    }

    [Fact]
    public async Task Restricted_client_does_not_change_the_count_of_a_privileged_client()
    {
        const string rsin = TestRsins.B;
        await SeedAsync(rsin);
        var restricted = NewClientAuthorizedForTypeBOnly();
        var privileged = NewPrivilegedClient();

        var r = await ListAsync(restricted, rsin, page: 1);
        _out.WriteLine($"[poison] restricted page 1 -> {r.status} count={r.count} results={r.results} next={r.next ?? "null"}");

        var p1 = await ListAsync(privileged, rsin, page: 1);
        _out.WriteLine($"[poison] privileged page 1 -> {p1.status} count={p1.count} results={p1.results} next={p1.next ?? "null"}");
        var p2 = await ListAsync(privileged, rsin, page: 2);
        _out.WriteLine($"[poison] privileged page 2 -> {p2.status} count={p2.count} results={p2.results}");

        Assert.Equal(DocsTypeA + DocsTypeB, p1.count);
        Assert.NotNull(p1.next);
        Assert.Equal(HttpStatusCode.OK, p2.status);
    }

    [Theory]
    [InlineData(Api.LatestVersion_1_1, TestRsins.G)]
    [InlineData(Api.LatestVersion_1_7, TestRsins.H)]
    public async Task Restricted_client_does_not_change_the_count_of_a_privileged_client_in_other_versions(string version, string rsin)
    {
        await SeedAsync(rsin);
        var restricted = NewClientAuthorizedForTypeBOnly();
        var privileged = NewPrivilegedClient();

        var r = await ListAsync(restricted, rsin, page: 1, version);
        _out.WriteLine($"[poison {version}] restricted page 1 -> {r.status} count={r.count} results={r.results}");
        var p = await ListAsync(privileged, rsin, page: 1, version);
        _out.WriteLine($"[poison {version}] privileged page 1 -> {p.status} count={p.count} results={p.results}");

        Assert.Equal(HttpStatusCode.OK, r.status);
        Assert.Equal(DocsTypeB, r.count);
        Assert.Equal(DocsTypeA + DocsTypeB, p.count);
    }

    [Fact]
    public async Task Restricted_client_does_not_change_the_verzendingen_count_of_a_privileged_client()
    {
        const string rsin = TestRsins.I;
        const int verzendingenTypeA = 3;
        const int verzendingenTypeB = 2;
        await SeedVerzendingenAsync(rsin, verzendingenTypeA, verzendingenTypeB);
        var restricted = NewClientAuthorizedForTypeBOnly();
        var privileged = NewPrivilegedClient();

        var r = await ListAsync(restricted, rsin, page: 1, Api.LatestVersion_1_5, "/api/v1/verzendingen");
        _out.WriteLine($"[poison verzendingen] restricted page 1 -> {r.status} count={r.count} results={r.results}");
        var p = await ListAsync(privileged, rsin, page: 1, Api.LatestVersion_1_5, "/api/v1/verzendingen");
        _out.WriteLine($"[poison verzendingen] privileged page 1 -> {p.status} count={p.count} results={p.results}");

        Assert.Equal(HttpStatusCode.OK, r.status);
        Assert.Equal(verzendingenTypeB, r.count);
        Assert.Equal(verzendingenTypeA + verzendingenTypeB, p.count);
    }

    [Fact]
    public async Task Restricted_client_does_not_read_the_count_of_documents_it_is_not_authorized_for()
    {
        const string rsin = TestRsins.C;
        await SeedAsync(rsin);
        var restricted = NewClientAuthorizedForTypeBOnly();
        var privileged = NewPrivilegedClient();

        var p = await ListAsync(privileged, rsin, page: 1);
        _out.WriteLine($"[leak] privileged page 1 -> {p.status} count={p.count} results={p.results}");

        var r = await ListAsync(restricted, rsin, page: 1);
        _out.WriteLine($"[leak] restricted page 1 -> {r.status} count={r.count} results={r.results} next={r.next ?? "null"}");

        Assert.Equal(DocsTypeB, r.results);
        Assert.Equal(DocsTypeB, r.count);
    }

    [Fact]
    public Task Restricted_client_does_not_change_the_page_anchor_of_a_privileged_client() => AnchorScenarioAsync(TestRsins.E, restrictedFirst: true);

    [Fact]
    public Task Control_privileged_client_alone_pages_through_the_200_newest_without_gaps() =>
        AnchorScenarioAsync(TestRsins.F, restrictedFirst: false);

    private async Task AnchorScenarioAsync(string rsin, bool restrictedFirst)
    {
        const int total = 400;
        var titles = new List<string>();
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DrcDbContext>();
            var docs = new List<EnkelvoudigInformatieObject>();
            var t0 = DateTime.UtcNow;
            for (var i = 0; i < total; i++)
            {
                var iot = (i % 8) < 5 ? IotB : IotA;
                var now = t0.AddSeconds(-i);
                var eio = new EnkelvoudigInformatieObject
                {
                    Id = Guid.NewGuid(),
                    Owner = rsin,
                    CreationTime = now,
                    InformatieObjectType = iot,
                    CatalogusId = Guid.NewGuid(),
                    LatestVertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.openbaar,
                };
                var title = $"doc-{i:D4}-{(iot == IotA ? "A" : "B")}";
                titles.Add(title);
                eio.EnkelvoudigInformatieObjectVersies.Add(
                    new EnkelvoudigInformatieObjectVersie
                    {
                        Id = Guid.NewGuid(),
                        Owner = rsin,
                        CreationTime = now,
                        Identificatie = $"DOC-{rsin}-{i:D4}",
                        Bronorganisatie = rsin,
                        Titel = title,
                        Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar,
                        Auteur = "lab",
                        Taal = "nld",
                        Versie = 1,
                        BeginRegistratie = now,
                        Bestandsnaam = "lab.txt",
                        CreatieDatum = DateOnly.FromDateTime(now),
                        InformatieObject = eio,
                    }
                );
                docs.Add(eio);
            }
            context.EnkelvoudigInformatieObjecten.AddRange(docs);
            await context.SaveChangesAsync();
            foreach (var eio in docs)
                eio.LatestEnkelvoudigInformatieObjectVersieId = eio.EnkelvoudigInformatieObjectVersies[0].Id;
            await context.SaveChangesAsync();
        }
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DrcDbContext>();
            titles = context
                .EnkelvoudigInformatieObjecten.Where(e => e.Owner == rsin)
                .OrderByDescending(e => e.CreationTime)
                .ThenBy(e => e.Id)
                .Select(e => e.LatestEnkelvoudigInformatieObjectVersie.Titel)
                .ToList();
        }
        var restricted = NewClientAuthorizedForTypeBOnly();
        var privileged = NewPrivilegedClient();

        if (restrictedFirst)
        {
            var r = await ListTitlesAsync(restricted, rsin, 1);
            _out.WriteLine($"[anchor] restricted page 1 -> {r.status} count={r.count} results={r.titles.Count} last={r.titles[^1]}");
        }
        _out.WriteLine($"[anchor] scenario={(restrictedFirst ? "ATTACK (restricted lists first)" : "CONTROL (privileged alone)")}");

        var seen = new List<string>();
        for (var page = 1; page <= 5; page++)
        {
            var p = await ListTitlesAsync(privileged, rsin, page);
            _out.WriteLine(
                $"[anchor] privileged page {page} -> {p.status} count={p.count} results={p.titles.Count} first={(p.titles.Count > 0 ? p.titles[0] : "-")} last={(p.titles.Count > 0 ? p.titles[^1] : "-")} next={(p.next == null ? "null" : "yes")}"
            );
            if (p.status != HttpStatusCode.OK)
                break;
            seen.AddRange(p.titles);
            if (p.next == null)
                break;
        }
        var unique = new HashSet<string>(seen);
        var missing = titles.FindAll(t => !unique.Contains(t));
        _out.WriteLine(
            $"[anchor] privileged received {unique.Count} unique of {total}; missing {missing.Count} (first missing: {(missing.Count > 0 ? missing[0] : "-")}..{(missing.Count > 0 ? missing[^1] : "-")})"
        );

        var p1 = await ListTitlesAsync(privileged, rsin, 1);
        var p2 = await ListTitlesAsync(privileged, rsin, 2);
        var expectedFirst200 = new HashSet<string>(titles.GetRange(0, 200));
        var got = new HashSet<string>(p1.titles);
        got.UnionWith(p2.titles);
        var skipped = new List<string>(expectedFirst200);
        skipped.RemoveAll(got.Contains);
        var dup = p1.titles.FindAll(p2.titles.Contains).Count;
        _out.WriteLine(
            $"[anchor] expected page2 first={titles[100]} actual page2 first={(p2.titles.Count > 0 ? p2.titles[0] : "-")}; of the 200 newest, privileged skipped {skipped.Count}, duplicates p1/p2={dup}"
        );
        Assert.Equal(titles[100], p2.titles.Count > 0 ? p2.titles[0] : null);
        Assert.Empty(skipped);
    }

    private async Task<(HttpStatusCode status, int? count, string next, List<string> titles)> ListTitlesAsync(string clientId, string rsin, int page)
    {
        var path = "/api/v1/enkelvoudiginformatieobjecten" + (page > 1 ? $"?page={page}" : "");
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Api-Version", Api.LatestVersion_1_5);
        request.Headers.Authorization = TestIdentity.Create(clientId, rsin);
        using var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        var list = new List<string>();
        if (response.StatusCode != HttpStatusCode.OK)
            return (response.StatusCode, null, null, list);
        var json = JObject.Parse(body);
        foreach (var item in (JArray)json["results"])
            list.Add(item.Value<string>("titel"));
        return (response.StatusCode, json.Value<int?>("count"), json.Value<string>("next"), list);
    }

    private string NewPrivilegedClient()
    {
        var clientId = $"privileged-{Guid.NewGuid():N}";
        _factory.AutorisatiesApi.GrantAllAuthorizations(clientId);
        return clientId;
    }

    private string NewClientAuthorizedForTypeBOnly()
    {
        var clientId = $"restricted-{Guid.NewGuid():N}";
        _factory.AutorisatiesApi.Grant(
            clientId,
            new AutorisatieResponseDto
            {
                Component = "drc",
                Scopes = [AuthorizationScopes.Documenten.Read],
                InformatieObjectType = IotB,
                MaxVertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.zeer_geheim.ToString(),
            }
        );
        return clientId;
    }

    private async Task<(HttpStatusCode status, int? count, string next, int results)> ListAsync(
        string clientId,
        string rsin,
        int page,
        string version = Api.LatestVersion_1_5,
        string resource = "/api/v1/enkelvoudiginformatieobjecten"
    )
    {
        var path = resource + (page > 1 ? $"?page={page}" : "");
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Api-Version", version);
        request.Headers.Authorization = TestIdentity.Create(clientId, rsin);
        using var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK)
            return (response.StatusCode, null, null, 0);
        var json = JObject.Parse(body);
        return (response.StatusCode, json.Value<int?>("count"), json.Value<string>("next"), ((JArray)json["results"])?.Count ?? 0);
    }

    private async Task SeedVerzendingenAsync(string rsin, int typeA, int typeB)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DrcDbContext>();
        for (var i = 0; i < typeA + typeB; i++)
        {
            var now = DateTime.UtcNow.AddSeconds(-i);
            var eio = new EnkelvoudigInformatieObject
            {
                Id = Guid.NewGuid(),
                Owner = rsin,
                CreationTime = now,
                InformatieObjectType = i < typeA ? IotA : IotB,
                CatalogusId = Guid.NewGuid(),
                LatestVertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.openbaar,
            };
            context.EnkelvoudigInformatieObjecten.Add(eio);
            context.Verzendingen.Add(
                new Verzending
                {
                    Id = Guid.NewGuid(),
                    CreationTime = now,
                    Betrokkene = "https://klanten.integrationtest.invalid/api/v1/klanten/00000000-0000-0000-0000-000000000001",
                    AardRelatie = OneGround.ZGW.Documenten.DataModel.AardRelatie.geadresseerde,
                    Contactpersoon = "https://klanten.integrationtest.invalid/api/v1/contactpersonen/00000000-0000-0000-0000-000000000001",
                    InformatieObject = eio,
                }
            );
        }
        await context.SaveChangesAsync();
    }

    private async Task SeedAsync(string rsin)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DrcDbContext>();
        var docs = new List<EnkelvoudigInformatieObject>();
        for (var i = 0; i < DocsTypeA + DocsTypeB; i++)
        {
            var iot = i < DocsTypeA ? IotA : IotB;
            var now = DateTime.UtcNow.AddSeconds(-i);
            var eio = new EnkelvoudigInformatieObject
            {
                Id = Guid.NewGuid(),
                Owner = rsin,
                CreationTime = now,
                InformatieObjectType = iot,
                CatalogusId = Guid.NewGuid(),
                LatestVertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.openbaar,
            };
            var versie = new EnkelvoudigInformatieObjectVersie
            {
                Id = Guid.NewGuid(),
                Owner = rsin,
                CreationTime = now,
                Identificatie = $"DOC-{rsin}-{i:D4}",
                Bronorganisatie = rsin,
                Titel = iot == IotA ? $"type-A document {i}" : $"type-B document {i}",
                Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar,
                Auteur = "lab",
                Taal = "nld",
                Versie = 1,
                BeginRegistratie = now,
                Bestandsnaam = "lab.txt",
                CreatieDatum = DateOnly.FromDateTime(now),
                InformatieObject = eio,
            };
            eio.EnkelvoudigInformatieObjectVersies.Add(versie);
            docs.Add(eio);
        }
        context.EnkelvoudigInformatieObjecten.AddRange(docs);
        await context.SaveChangesAsync();
        foreach (var eio in docs)
        {
            eio.LatestEnkelvoudigInformatieObjectVersieId = eio.EnkelvoudigInformatieObjectVersies[0].Id;
        }
        await context.SaveChangesAsync();
    }
}
