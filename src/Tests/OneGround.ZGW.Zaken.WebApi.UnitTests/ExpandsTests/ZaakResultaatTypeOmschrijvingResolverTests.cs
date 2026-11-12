using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakResultaatTypeOmschrijvingResolverTests
{
    [Fact]
    public async Task ResolveAsync_ParentAlreadyHasTheDocumentExpanded_ReadsItFromParent()
    {
        var json = new JObject { ["naam"] = "document" };
        var parent = new ResultaatTypeResponseDto { Expand = new Dictionary<string, object> { ["resultaattypeomschrijving"] = json } };

        var resolver = new ZaakResultaatTypeOmschrijvingResolver();
        var resolved = new Dictionary<string, object> { ["resultaat.resultaattype"] = parent };

        var result = await resolver.ResolveAsync(
            new ZaakResponseDto(),
            resolved,
            new HashSet<string> { "resultaat.resultaattype.resultaattypeomschrijving" }
        );

        Assert.Same(json, result);
    }

    [Fact]
    public async Task ResolveAsync_ParentNotResolved_ReturnsNull()
    {
        var resolver = new ZaakResultaatTypeOmschrijvingResolver();

        var result = await resolver.ResolveAsync(
            new ZaakResponseDto(),
            new Dictionary<string, object>(),
            new HashSet<string> { "resultaat.resultaattype.resultaattypeomschrijving" }
        );

        Assert.Null(result);
    }

    [Fact]
    public void Path_And_Parent_AreCorrect()
    {
        var resolver = new ZaakResultaatTypeOmschrijvingResolver();

        Assert.Equal("resultaat.resultaattype.resultaattypeomschrijving", resolver.Path);
        Assert.Equal("resultaat.resultaattype", resolver.Parent);
    }
}
