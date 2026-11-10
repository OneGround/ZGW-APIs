using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakTypeStatusTypenResolverTests
{
    [Fact]
    public async Task ResolveAsync_ParentZaaktypeAlreadyHasStatustypenExpanded_ReadsThemFromParent()
    {
        var statustypen = new List<StatusTypeResponseDto>
        {
            new() { Url = "http://catalogi.local/api/v1/statustypen/1" },
            new() { Url = "http://catalogi.local/api/v1/statustypen/2" },
        };
        var zaaktype = new ZaakTypeResponseDto { Expand = new Dictionary<string, object> { ["statustypen"] = statustypen } };

        var resolver = new ZaakTypeStatusTypenResolver();
        var resolved = new Dictionary<string, object> { ["zaaktype"] = zaaktype };

        var result = await resolver.ResolveAsync(new ZaakResponseDto(), resolved, new HashSet<string> { "zaaktype", "zaaktype.statustypen" });

        Assert.Equal(statustypen.Select(r => r.Url), ((List<StatusTypeResponseDto>)result).Select(r => r.Url));
    }

    [Fact]
    public async Task ResolveAsync_ParentNotResolved_ReturnsNull()
    {
        var resolver = new ZaakTypeStatusTypenResolver();

        var result = await resolver.ResolveAsync(
            new ZaakResponseDto(),
            new Dictionary<string, object>(),
            new HashSet<string> { "zaaktype.statustypen" }
        );

        Assert.Null(result);
    }

    [Fact]
    public void Path_And_Parent_AreCorrect()
    {
        var resolver = new ZaakTypeStatusTypenResolver();

        Assert.Equal("zaaktype.statustypen", resolver.Path);
        Assert.Equal("zaaktype", resolver.Parent);
    }
}
