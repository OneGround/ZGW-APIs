using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Catalogi.Web.Expands.v1._4;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Http;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.ExpandsTests;

public class ExternalJsonResolverTests
{
    private const string ProcestypeUrl = "https://api.example.test/procestypen/1";
    private const string OmschrijvingUrl = "https://api.example.test/omschrijvingen/2";
    private const string KlasseUrl = "https://api.example.test/klassen/3";

    private static Mock<IExternalJsonClient> ClientReturning(string url, JObject json)
    {
        var client = new Mock<IExternalJsonClient>();
        client.Setup(c => c.GetJsonObjectAsync(url, It.IsAny<CancellationToken>())).ReturnsAsync(json);
        return client;
    }

    [Fact]
    public async Task ZaakType_SelectielijstProcestype_FetchesTheSelectielijstProcestypeUrl()
    {
        var json = new JObject { ["naam"] = "procestype" };
        var client = ClientReturning(ProcestypeUrl, json);

        var resolver = new ZaakType_SelectielijstProcestype_Resolver(client.Object);

        var result = await resolver.ResolveAsync(
            new ZaakTypeResponseDto { SelectielijstProcestype = ProcestypeUrl },
            new Dictionary<string, object>(),
            new HashSet<string> { "selectielijstprocestype" }
        );

        Assert.Same(json, result);
        client.Verify(c => c.GetJsonObjectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResultaatType_ResultaatTypeOmschrijving_FetchesTheResultaatTypeOmschrijvingUrl()
    {
        var json = new JObject { ["omschrijving"] = "afgehandeld" };
        var client = ClientReturning(OmschrijvingUrl, json);

        var resolver = new ResultaatType_ResultaatTypeOmschrijving_Resolver(client.Object);

        var result = await resolver.ResolveAsync(
            new ResultaatTypeResponseDto { ResultaatTypeOmschrijving = OmschrijvingUrl, SelectieLijstKlasse = KlasseUrl },
            new Dictionary<string, object>(),
            new HashSet<string> { "resultaattypeomschrijving" }
        );

        Assert.Same(json, result);
        client.Verify(c => c.GetJsonObjectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResultaatType_SelectielijstKlasse_FetchesTheSelectielijstKlasseUrl()
    {
        var json = new JObject { ["omschrijving"] = "klasse" };
        var client = ClientReturning(KlasseUrl, json);

        var resolver = new ResultaatType_SelectielijstKlasse_Resolver(client.Object);

        var result = await resolver.ResolveAsync(
            new ResultaatTypeResponseDto { ResultaatTypeOmschrijving = OmschrijvingUrl, SelectieLijstKlasse = KlasseUrl },
            new Dictionary<string, object>(),
            new HashSet<string> { "selectielijstklasse" }
        );

        Assert.Same(json, result);
        client.Verify(c => c.GetJsonObjectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Resolvers_PlugIntoTheExpandEngine_AndPutTheJsonUnderTheExpandKeys()
    {
        var client = new Mock<IExternalJsonClient>();
        client.Setup(c => c.GetJsonObjectAsync(KlasseUrl, It.IsAny<CancellationToken>())).ReturnsAsync(new JObject { ["omschrijving"] = "klasse" });
        // An empty url (field not set) must end up as an empty object, not as a missing key
        client.Setup(c => c.GetJsonObjectAsync("", It.IsAny<CancellationToken>())).ReturnsAsync(new JObject());

        var engine = new ExpandEngine<ResultaatTypeResponseDto>([
            new ResultaatType_ResultaatTypeOmschrijving_Resolver(client.Object),
            new ResultaatType_SelectielijstKlasse_Resolver(client.Object),
        ]);
        var entity = new ResultaatTypeResponseDto { ResultaatTypeOmschrijving = "", SelectieLijstKlasse = KlasseUrl };

        await engine.ResolveAsync(entity, ["resultaattypeomschrijving", "selectielijstklasse"]);

        Assert.Equal(["resultaattypeomschrijving", "selectielijstklasse"], entity.Expand.Keys.Order().ToArray());
        Assert.Empty((JObject)entity.Expand["resultaattypeomschrijving"]);
        Assert.Equal("klasse", (string)((JObject)entity.Expand["selectielijstklasse"])["omschrijving"]);
    }

    [Fact]
    public void Paths_AreTopLevel()
    {
        var client = Mock.Of<IExternalJsonClient>();

        Assert.Equal("selectielijstprocestype", new ZaakType_SelectielijstProcestype_Resolver(client).Path);
        Assert.Equal("resultaattypeomschrijving", new ResultaatType_ResultaatTypeOmschrijving_Resolver(client).Path);
        Assert.Equal("selectielijstklasse", new ResultaatType_SelectielijstKlasse_Resolver(client).Path);
        Assert.Null(new ZaakType_SelectielijstProcestype_Resolver(client).Parent);
    }
}
