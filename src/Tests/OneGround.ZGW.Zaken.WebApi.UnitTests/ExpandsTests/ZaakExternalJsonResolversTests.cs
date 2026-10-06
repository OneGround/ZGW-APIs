using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Http;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakExternalJsonResolversTests
{
    private const string KanaalUrl = "https://api.example.test/kanalen/telefoon";
    private const string KlasseUrl = "https://api.example.test/klassen/42";

    [Fact]
    public async Task Communicatiekanaal_FetchesTheCommunicatiekanaalUrlAndReturnsTheJsonObject()
    {
        var json = new JObject { ["naam"] = "Telefoon" };
        var client = new Mock<IExternalJsonClient>();
        client.Setup(c => c.GetJsonObjectAsync(KanaalUrl, It.IsAny<CancellationToken>())).ReturnsAsync(json);

        var resolver = new ZaakCommunicatiekanaalResolver(client.Object);
        var entity = new ZaakResponseDto { Communicatiekanaal = KanaalUrl, Selectielijstklasse = KlasseUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "communicatiekanaal" });

        Assert.Same(json, result);
        client.Verify(c => c.GetJsonObjectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Selectielijstklasse_FetchesTheSelectielijstklasseUrlAndReturnsTheJsonObject()
    {
        var json = new JObject { ["omschrijving"] = "Klasse 42" };
        var client = new Mock<IExternalJsonClient>();
        client.Setup(c => c.GetJsonObjectAsync(KlasseUrl, It.IsAny<CancellationToken>())).ReturnsAsync(json);

        var resolver = new ZaakSelectielijstklasseResolver(client.Object);
        var entity = new ZaakResponseDto { Communicatiekanaal = KanaalUrl, Selectielijstklasse = KlasseUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "selectielijstklasse" });

        Assert.Same(json, result);
        client.Verify(c => c.GetJsonObjectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Resolvers_PlugIntoTheExpandEngine_AndPutTheJsonUnderTheExpandKeys()
    {
        var client = new Mock<IExternalJsonClient>();
        client.Setup(c => c.GetJsonObjectAsync(KanaalUrl, It.IsAny<CancellationToken>())).ReturnsAsync(new JObject { ["naam"] = "Telefoon" });
        // An empty url (field not set) must end up as an empty object, not as a missing key
        client.Setup(c => c.GetJsonObjectAsync("", It.IsAny<CancellationToken>())).ReturnsAsync(new JObject());

        var engine = new ExpandEngine<ZaakResponseDto>([
            new ZaakCommunicatiekanaalResolver(client.Object),
            new ZaakSelectielijstklasseResolver(client.Object),
        ]);
        var entity = new ZaakResponseDto { Communicatiekanaal = KanaalUrl, Selectielijstklasse = "" };

        await engine.ResolveAsync(entity, ["communicatiekanaal", "selectielijstklasse"]);

        Assert.Equal(["communicatiekanaal", "selectielijstklasse"], entity.Expand.Keys.Order().ToArray());
        Assert.Equal("Telefoon", (string)((JObject)entity.Expand["communicatiekanaal"])["naam"]);
        Assert.Empty((JObject)entity.Expand["selectielijstklasse"]);
    }

    [Fact]
    public void Paths_AreTopLevel()
    {
        var client = Mock.Of<IExternalJsonClient>();

        Assert.Equal("communicatiekanaal", new ZaakCommunicatiekanaalResolver(client).Path);
        Assert.Null(new ZaakCommunicatiekanaalResolver(client).Parent);
        Assert.Equal("selectielijstklasse", new ZaakSelectielijstklasseResolver(client).Path);
        Assert.Null(new ZaakSelectielijstklasseResolver(client).Parent);
    }
}
