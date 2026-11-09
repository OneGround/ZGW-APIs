using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.Zaken.Web.Controllers;
using Xunit;
using V1Routes = OneGround.ZGW.Zaken.Web.Contracts.v1.ApiRoutes;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// POST of a rol keeps the identification object of the betrokkene (betrokkeneIdentificatie) for every betrokkene type, in every API version, and the
/// identificatie of a medewerker may hold 24 characters up to version 1.5 and 254 from version 1.7.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class ZaakRollenBetrokkeneIdentificatieTests(ZakenApiFixture fixture) : ZakenApiTestBase(fixture)
{
    private const string Rsin = TestRsins.A;

    // A medewerker identificatie longer than the 24 characters the older versions accept
    private const string LongIdentificatie = "1234567890123456789012345";

    public static TheoryData<string, string, string> Betrokkenen =>
        new()
        {
            { "medewerker", "identificatie", "mdw-1" },
            { "organisatorische_eenheid", "identificatie", "oe-1" },
            { "vestiging", "vestigingsNummer", "123456789012" },
            { "niet_natuurlijk_persoon", "innNnpId", "123456789" },
            { "natuurlijk_persoon", "anpIdentificatie", "anp-1" },
        };

    [Theory]
    [MemberData(nameof(Betrokkenen))]
    public async Task Version_1_7_keeps_the_betrokkeneIdentificatie(string betrokkeneType, string property, string value)
    {
        var (response, _) = await PostAsync(Api.LatestVersion_1_7, betrokkeneType, new JObject { [property] = value });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = JObject.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(value, created["betrokkeneIdentificatie"]?.Value<string>(property));
    }

    [Theory]
    [MemberData(nameof(Betrokkenen))]
    public async Task Version_1_5_keeps_the_betrokkeneIdentificatie(string betrokkeneType, string property, string value)
    {
        var (response, _) = await PostAsync(Api.LatestVersion_1_5, betrokkeneType, new JObject { [property] = value });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = JObject.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(value, created["betrokkeneIdentificatie"]?.Value<string>(property));
    }

    [Fact]
    public async Task Version_1_7_accepts_a_medewerker_identificatie_longer_than_24_characters_and_returns_it_on_read()
    {
        var (response, clientId) = await PostAsync(Api.LatestVersion_1_7, "medewerker", MedewerkerIdentificatie(LongIdentificatie));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = JObject.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(LongIdentificatie, created["betrokkeneIdentificatie"]!.Value<string>("identificatie"));

        var read = await Client.SendAsync(
            ZakenEndpointRequests.Create(HttpMethod.Get, new Uri(created.Value<string>("url")!).AbsolutePath, Api.LatestVersion_1_7, clientId, Rsin)
        );
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(
            LongIdentificatie,
            JObject.Parse(await read.Content.ReadAsStringAsync())["betrokkeneIdentificatie"]!.Value<string>("identificatie")
        );
    }

    [Theory]
    [InlineData(Api.LatestVersion_1_0)]
    [InlineData(Api.LatestVersion_1_2)]
    [InlineData(Api.LatestVersion_1_5)]
    public async Task Older_versions_still_refuse_a_medewerker_identificatie_longer_than_24_characters(string apiVersion)
    {
        var (response, _) = await PostAsync(apiVersion, "medewerker", MedewerkerIdentificatie(LongIdentificatie));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("betrokkeneIdentificatie.identificatie", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(Api.LatestVersion_1_7, 254, HttpStatusCode.Created)]
    [InlineData(Api.LatestVersion_1_7, 255, HttpStatusCode.BadRequest)]
    [InlineData(Api.LatestVersion_1_5, 24, HttpStatusCode.Created)]
    [InlineData(Api.LatestVersion_1_5, 25, HttpStatusCode.BadRequest)]
    [InlineData(Api.LatestVersion_1_2, 24, HttpStatusCode.Created)]
    [InlineData(Api.LatestVersion_1_2, 25, HttpStatusCode.BadRequest)]
    public async Task The_medewerker_identificatie_maximum_is_24_up_to_version_1_5_and_254_from_version_1_7(
        string apiVersion,
        int length,
        HttpStatusCode expected
    )
    {
        var (response, _) = await PostAsync(apiVersion, "medewerker", MedewerkerIdentificatie(new string('x', length)));

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Version_1_7_keeps_the_kvkNummer_of_a_vestiging()
    {
        var (response, _) = await PostAsync(
            Api.LatestVersion_1_7,
            "vestiging",
            new JObject { ["vestigingsNummer"] = "123456789012", ["kvknummer"] = "12345678" }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = JObject.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("12345678", created["betrokkeneIdentificatie"]!.Value<string>("kvknummer"));
    }

    [Fact]
    public async Task Version_1_7_keeps_the_contactpersoonRol()
    {
        var contactpersoonRol = new JObject { ["naam"] = "Paul", ["emailadres"] = "paul@example.com" };

        var (response, _) = await PostAsync(Api.LatestVersion_1_7, "medewerker", MedewerkerIdentificatie("mdw-1"), contactpersoonRol);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = JObject.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Paul", created["contactpersoonRol"]!.Value<string>("naam"));
    }

    private static JObject MedewerkerIdentificatie(string identificatie) => new() { ["identificatie"] = identificatie, ["achternaam"] = "Janssen" };

    private async Task<(HttpResponseMessage Response, string ClientId)> PostAsync(
        string apiVersion,
        string betrokkeneType,
        JObject betrokkeneIdentificatie,
        JObject contactpersoonRol = null
    )
    {
        var clientId = ZrcClients.WithAllAuthorizations(Factory);
        var zaak = await ZakenSeed.SeedZaakAsync(Factory, rsin: Rsin);

        var body = new JObject
        {
            ["zaak"] = zaak.Url,
            ["betrokkeneType"] = betrokkeneType,
            ["roltype"] = zaak.Catalogi.RolType,
            ["roltoelichting"] = "Integratietest",
            ["betrokkeneIdentificatie"] = betrokkeneIdentificatie,
        };

        if (contactpersoonRol != null)
        {
            body["contactpersoonRol"] = contactpersoonRol;
        }

        var request = ZakenEndpointRequests.Create(HttpMethod.Post, "/" + V1Routes.ZaakRollen.Create, apiVersion, clientId, zaak.Rsin, body);

        return (await Client.SendAsync(request), clientId);
    }
}
