using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakTypeCatalogusResolverTests
{
    [Fact]
    public async Task ResolveAsync_ParentZaaktypeAlreadyHasCatalogusExpanded_ReadsItFromParent()
    {
        var catalogus = new CatalogusResponseDto { Url = "http://catalogi.local/api/v1/catalogussen/1" };
        var zaaktype = new ZaakTypeResponseDto { Expand = new Dictionary<string, object> { ["catalogus"] = catalogus } };

        var resolver = new ZaakTypeCatalogusResolver();
        var resolved = new Dictionary<string, object> { ["zaaktype"] = zaaktype };

        var result = await resolver.ResolveAsync(new ZaakResponseDto(), resolved, new HashSet<string> { "zaaktype", "zaaktype.catalogus" });

        Assert.Same(catalogus, result);
    }

    [Fact]
    public async Task ResolveAsync_ParentNotResolved_ReturnsNull()
    {
        var resolver = new ZaakTypeCatalogusResolver();

        var result = await resolver.ResolveAsync(
            new ZaakResponseDto(),
            new Dictionary<string, object>(),
            new HashSet<string> { "zaaktype.catalogus" }
        );

        Assert.Null(result);
    }

    [Fact]
    public void Path_And_Parent_AreCorrect()
    {
        var resolver = new ZaakTypeCatalogusResolver();

        Assert.Equal("zaaktype.catalogus", resolver.Path);
        Assert.Equal("zaaktype", resolver.Parent);
    }
}
