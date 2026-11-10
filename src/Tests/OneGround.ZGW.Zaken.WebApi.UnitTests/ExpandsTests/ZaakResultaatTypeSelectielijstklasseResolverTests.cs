using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakResultaatTypeSelectielijstklasseResolverTests
{
    [Fact]
    public async Task ResolveAsync_ParentAlreadyHasTheDocumentExpanded_ReadsItFromParent()
    {
        var json = new JObject { ["naam"] = "document" };
        var parent = new ResultaatTypeResponseDto { Expand = new Dictionary<string, object> { ["selectielijstklasse"] = json } };

        var resolver = new ZaakResultaatTypeSelectielijstklasseResolver();
        var resolved = new Dictionary<string, object> { ["resultaat.resultaattype"] = parent };

        var result = await resolver.ResolveAsync(
            new ZaakResponseDto(),
            resolved,
            new HashSet<string> { "resultaat.resultaattype.selectielijstklasse" }
        );

        Assert.Same(json, result);
    }

    [Fact]
    public async Task ResolveAsync_ParentNotResolved_ReturnsNull()
    {
        var resolver = new ZaakResultaatTypeSelectielijstklasseResolver();

        var result = await resolver.ResolveAsync(
            new ZaakResponseDto(),
            new Dictionary<string, object>(),
            new HashSet<string> { "resultaat.resultaattype.selectielijstklasse" }
        );

        Assert.Null(result);
    }

    [Fact]
    public void Path_And_Parent_AreCorrect()
    {
        var resolver = new ZaakResultaatTypeSelectielijstklasseResolver();

        Assert.Equal("resultaat.resultaattype.selectielijstklasse", resolver.Path);
        Assert.Equal("resultaat.resultaattype", resolver.Parent);
    }
}
