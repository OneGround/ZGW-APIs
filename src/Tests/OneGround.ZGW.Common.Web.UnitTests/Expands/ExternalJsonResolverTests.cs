using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Http;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Expands;

public class ExternalJsonResolverTests
{
    private const string Url = "https://api.example.test/documents/1";

    private class Entity
    {
        public string Link { get; set; }
    }

    private class TestResolver(IExternalJsonClient externalJsonClient) : ExternalJsonResolver<Entity>(externalJsonClient)
    {
        public override string Path => "link";

        protected override string GetUrl(Entity entity) => entity.Link;
    }

    [Fact]
    public async Task ResolveAsync_FetchesTheUrlOfTheEntityAndReturnsTheJsonObjectAsIs()
    {
        var json = new JObject { ["naam"] = "document" };
        var client = new Mock<IExternalJsonClient>();
        client.Setup(c => c.GetJsonObjectAsync(Url, It.IsAny<CancellationToken>())).ReturnsAsync(json);

        var result = await new TestResolver(client.Object).ResolveAsync(
            new Entity { Link = Url },
            new Dictionary<string, object>(),
            new HashSet<string> { "link" }
        );

        Assert.Same(json, result);
        client.Verify(c => c.GetJsonObjectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_UrlNotSet_PassesItOnAndReturnsWhateverTheClientReturns()
    {
        var empty = new JObject();
        var client = new Mock<IExternalJsonClient>();
        client.Setup(c => c.GetJsonObjectAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(empty);

        var result = await new TestResolver(client.Object).ResolveAsync(
            new Entity { Link = null },
            new Dictionary<string, object>(),
            new HashSet<string> { "link" }
        );

        Assert.Same(empty, result);
    }

    [Fact]
    public void Parent_IsNull_SoThePathIsTopLevel()
    {
        var resolver = new TestResolver(Mock.Of<IExternalJsonClient>());

        Assert.Equal("link", resolver.Path);
        Assert.Null(resolver.Parent);
    }
}
