using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakTypeSelectielijstProcestypeResolverTests
{
    [Fact]
    public async Task ResolveAsync_ParentAlreadyHasTheDocumentExpanded_ReadsItFromParent()
    {
        var json = new JObject { ["naam"] = "document" };
        var parent = new ZaakTypeResponseDto { Expand = new Dictionary<string, object> { ["selectielijstprocestype"] = json } };

        var resolver = new ZaakTypeSelectielijstProcestypeResolver();
        var resolved = new Dictionary<string, object> { ["zaaktype"] = parent };

        var result = await resolver.ResolveAsync(new ZaakResponseDto(), resolved, new HashSet<string> { "zaaktype.selectielijstprocestype" });

        Assert.Same(json, result);
    }

    [Fact]
    public async Task ResolveAsync_ParentNotResolved_ReturnsNull()
    {
        var resolver = new ZaakTypeSelectielijstProcestypeResolver();

        var result = await resolver.ResolveAsync(
            new ZaakResponseDto(),
            new Dictionary<string, object>(),
            new HashSet<string> { "zaaktype.selectielijstprocestype" }
        );

        Assert.Null(result);
    }

    [Fact]
    public void Path_And_Parent_AreCorrect()
    {
        var resolver = new ZaakTypeSelectielijstProcestypeResolver();

        Assert.Equal("zaaktype.selectielijstprocestype", resolver.Path);
        Assert.Equal("zaaktype", resolver.Parent);
    }
}
