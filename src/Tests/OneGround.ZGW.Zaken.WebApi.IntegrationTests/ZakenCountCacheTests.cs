using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.DataAccess.Encryption;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.IntegrationTests.Common.Authentication;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Controllers;
using Xunit;
using Xunit.Abstractions;
using AutorisatieResponseDto = OneGround.ZGW.Autorisaties.Contracts.v1.Responses.AutorisatieResponseDto;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

[Collection(ZakenApiCollection.Name)]
public class ZakenCountCacheTests
{
    // RvIG omnummertabel test BSN, never issued.
    private const string TestBsn = "999993653";
    private const string BsnWithoutZaken = "999990482";
    private const int ZakenTypeA = 60;
    private const int ZakenTypeB = 2;

    private readonly ZakenWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _out;

    public ZakenCountCacheTests(ZakenApiFixture fixture, ITestOutputHelper output)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
        _out = output;
    }

    [Fact]
    public async Task Zaken_restricted_client_does_not_change_the_count_of_a_privileged_client()
    {
        const string rsin = TestRsins.H;
        for (var i = 0; i < ZakenTypeA; i++)
            await SeedZaakAsync(rsin, ZaakTypes.A, VertrouwelijkheidAanduiding.geheim);
        for (var i = 0; i < ZakenTypeB; i++)
            await SeedZaakAsync(rsin, ZaakTypes.B, VertrouwelijkheidAanduiding.openbaar);
        var restricted = NewClientAuthorizedForTypeBOnly();
        var privileged = NewPrivilegedClient();

        var r = await ListAsync(restricted, rsin, "");
        _out.WriteLine($"[zrc-poison] restricted page 1 -> {r.status} count={r.count} results={r.results} next={r.next ?? "null"}");
        var p1 = await ListAsync(privileged, rsin, "");
        _out.WriteLine($"[zrc-poison] privileged page 1 -> {p1.status} count={p1.count} results={p1.results} next={p1.next ?? "null"}");
        var p2 = await ListAsync(privileged, rsin, "?page=2");
        _out.WriteLine($"[zrc-poison] privileged page 2 -> {p2.status} count={p2.count} results={p2.results}");

        Assert.Equal(ZakenTypeA + ZakenTypeB, p1.count);
        Assert.NotNull(p1.next);
        Assert.Equal(HttpStatusCode.OK, p2.status);
    }

    [Fact]
    public async Task Zaken_restricted_client_does_not_learn_the_count_of_unauthorized_zaken_for_a_bsn()
    {
        const string rsin = TestRsins.I;
        for (var i = 0; i < 4; i++)
            await SeedZaakAsync(rsin, ZaakTypes.A, VertrouwelijkheidAanduiding.geheim, TestBsn);
        await SeedZaakAsync(rsin, ZaakTypes.B, VertrouwelijkheidAanduiding.openbaar);
        var restricted = NewClientAuthorizedForTypeBOnly();
        var privileged = NewPrivilegedClient();
        var q = $"?rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn={TestBsn}";

        var p = await ListAsync(privileged, rsin, q);
        _out.WriteLine($"[zrc-bsn] privileged search BSN -> {p.status} count={p.count} results={p.results}");

        var r = await ListAsync(restricted, rsin, q);
        _out.WriteLine($"[zrc-bsn] restricted search BSN -> {r.status} count={r.count} results={r.results}");

        Assert.Equal(0, r.results);
        Assert.Equal(0, r.count);
    }

    [Fact]
    public async Task Zaken_baseline_restricted_client_alone_gets_its_own_count()
    {
        const string rsin = TestRsins.J;
        for (var i = 0; i < 4; i++)
            await SeedZaakAsync(rsin, ZaakTypes.A, VertrouwelijkheidAanduiding.geheim, TestBsn);
        var restricted = NewClientAuthorizedForTypeBOnly();
        var r = await ListAsync(restricted, rsin, $"?rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn={TestBsn}");
        _out.WriteLine($"[zrc-baseline] restricted search BSN alone -> {r.status} count={r.count} results={r.results}");
        Assert.Equal(0, r.count);
    }

    [Fact]
    public async Task Zaken_bsn_search_without_results_writes_one_audit_entry_with_a_keyed_hash()
    {
        const string rsin = TestRsins.J;
        var client = NewPrivilegedClient();

        var r = await ListAsync(client, rsin, $"?rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn={BsnWithoutZaken}");

        using var scope = _factory.Services.CreateScope();
        var expectedHash = scope.ServiceProvider.GetRequiredService<IHashRotationService>().CreateLatestHash(BsnWithoutZaken).Hash;
        var context = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();
        var entries = context
            .AuditTrailDeltas.AsEnumerable()
            .Where(a => a.ApplicatieId == client && a.ActieWeergave == "Op BSN gefilterde lijst van zaken opgevraagd")
            .ToList();

        Assert.Equal(0, r.results);
        var entry = Assert.Single(entries);
        Assert.Contains(expectedHash, entry.Toelichting);
        Assert.DoesNotContain(
            context.AuditTrailDeltas.AsEnumerable().Where(a => a.ApplicatieId == client),
            a => (a.ActieWeergave + a.Toelichting + a.ResourceUrl + a.ResourceWeergave + a.DeltaJson).Contains(BsnWithoutZaken)
        );
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
                Component = "zrc",
                Scopes = [AuthorizationScopes.Zaken.Read],
                ZaakType = ZaakTypes.B,
                MaxVertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.zeer_geheim.ToString(),
            }
        );
        return clientId;
    }

    private async Task<(HttpStatusCode status, int? count, string next, int results)> ListAsync(string clientId, string rsin, string query)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/zaken" + query);
        request.Headers.Add("Api-Version", Api.LatestVersion_1_5);
        request.Headers.Add("Accept-Crs", "EPSG:4326");
        request.Headers.Authorization = TestIdentity.Create(clientId, rsin);
        using var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK)
        {
            _out.WriteLine($"    (status {(int)response.StatusCode}: {body[..Math.Min(body.Length, 200)]})");
            return (response.StatusCode, null, null, 0);
        }
        var json = JObject.Parse(body);
        return (response.StatusCode, json.Value<int?>("count"), json.Value<string>("next"), ((JArray)json["results"])?.Count ?? 0);
    }

    private async Task SeedZaakAsync(string rsin, string zaakType, VertrouwelijkheidAanduiding vha, string bsn = null)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();
        var zaak = new Zaak
        {
            Id = Guid.NewGuid(),
            Owner = rsin,
            Bronorganisatie = rsin,
            VerantwoordelijkeOrganisatie = rsin,
            Identificatie = $"ZAAK-{Guid.NewGuid():N}",
            Zaaktype = zaakType,
            Startdatum = DateOnly.FromDateTime(DateTime.UtcNow),
            Communicatiekanaal = "",
            Selectielijstklasse = "",
            VertrouwelijkheidAanduiding = vha,
        };
        context.Zaken.Add(zaak);
        if (bsn != null)
        {
            var rol = new OneGround.ZGW.Zaken.DataModel.ZaakRol.ZaakRol
            {
                Id = Guid.NewGuid(),
                Owner = rsin,
                CreationTime = DateTime.UtcNow,
                Zaak = zaak,
                BetrokkeneType = BetrokkeneType.natuurlijk_persoon,
                RolType = "https://catalogi.integrationtest.invalid/api/v1/roltypen/00000000-0000-0000-0000-000000000001",
                Roltoelichting = "lab",
                Omschrijving = "belanghebbende",
                OmschrijvingGeneriek = OmschrijvingGeneriek.belanghebbende,
                Registratiedatum = DateTime.UtcNow,
            };
            rol.NatuurlijkPersoon = new OneGround.ZGW.Zaken.DataModel.ZaakRol.NatuurlijkPersoonZaakRol
            {
                Id = Guid.NewGuid(),
                ZaakRol = rol,
                InpBsnEncrypted = bsn, // ZrcDbContext encrypts this and computes InpBsnHash on save.
            };
            context.ZaakRollen.Add(rol);
        }
        await context.SaveChangesAsync();
    }
}
