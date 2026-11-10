using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakTypeResultaatTypenResolverTests
{
    [Fact]
    public async Task ResolveAsync_ParentZaaktypeAlreadyHasResultaattypenExpanded_ReadsThemFromParent()
    {
        var resultaattypen = new List<ResultaatTypeResponseDto>
        {
            new() { Url = "http://catalogi.local/api/v1/resultaattypen/1" },
            new() { Url = "http://catalogi.local/api/v1/resultaattypen/2" },
        };
        var zaaktype = new ZaakTypeResponseDto { Expand = new Dictionary<string, object> { ["resultaattypen"] = resultaattypen } };

        var resolver = new ZaakTypeResultaatTypenResolver();
        var resolved = new Dictionary<string, object> { ["zaaktype"] = zaaktype };

        var result = await resolver.ResolveAsync(new ZaakResponseDto(), resolved, new HashSet<string> { "zaaktype", "zaaktype.resultaattypen" });

        Assert.Equal(resultaattypen.Select(r => r.Url), ((List<ResultaatTypeResponseDto>)result).Select(r => r.Url));
    }

    [Fact]
    public async Task ResolveAsync_ParentNotResolved_ReturnsNull()
    {
        var resolver = new ZaakTypeResultaatTypenResolver();

        var result = await resolver.ResolveAsync(
            new ZaakResponseDto(),
            new Dictionary<string, object>(),
            new HashSet<string> { "zaaktype.resultaattypen" }
        );

        Assert.Null(result);
    }

    [Fact]
    public void Path_And_Parent_AreCorrect()
    {
        var resolver = new ZaakTypeResultaatTypenResolver();

        Assert.Equal("zaaktype.resultaattypen", resolver.Path);
        Assert.Equal("zaaktype", resolver.Parent);
    }
}
