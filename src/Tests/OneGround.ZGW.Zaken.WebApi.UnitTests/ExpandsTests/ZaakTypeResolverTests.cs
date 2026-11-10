using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._4.Expands;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakTypeResolverTests
{
    private const string ZaakTypeUrl = "http://catalogi.local/api/v1/zaaktypen/11111111-1111-1111-1111-111111111111";

    [Fact]
    public async Task ResolveAsync_ZaaktypeOnlyRequested_CallsServiceAgentWithoutExpandAndReturnsZaaktype()
    {
        var zaaktype = new ZaakTypeResponseDto { Url = ZaakTypeUrl };
        var serviceAgentMock = new Mock<ICatalogiServiceAgentDecorator>();
        serviceAgentMock.Setup(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl, null)).ReturnsAsync(new ServiceAgentResponse<ZaakTypeResponseDto>(zaaktype));

        var resolver = new ZaakTypeResolver(serviceAgentMock.Object, new GenericCache<ZaakTypeResponseDto>());
        var entity = new ZaakResponseDto { Zaaktype = ZaakTypeUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "zaaktype" });

        Assert.Same(zaaktype, result);
        serviceAgentMock.Verify(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl, null), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_CatalogusAlsoRequested_PassesExpandCatalogusToServiceAgent()
    {
        var zaaktype = new ZaakTypeResponseDto { Url = ZaakTypeUrl };
        var serviceAgentMock = new Mock<ICatalogiServiceAgentDecorator>();
        serviceAgentMock
            .Setup(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl, "catalogus"))
            .ReturnsAsync(new ServiceAgentResponse<ZaakTypeResponseDto>(zaaktype));

        var resolver = new ZaakTypeResolver(serviceAgentMock.Object, new GenericCache<ZaakTypeResponseDto>());
        var entity = new ZaakResponseDto { Zaaktype = ZaakTypeUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "zaaktype", "zaaktype.catalogus" });

        Assert.Same(zaaktype, result);
        serviceAgentMock.Verify(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl, "catalogus"), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_ResultaattypenRequested_PassesExpandResultaattypenToServiceAgent()
    {
        var zaaktype = new ZaakTypeResponseDto { Url = ZaakTypeUrl };
        var serviceAgentMock = new Mock<ICatalogiServiceAgentDecorator>();
        serviceAgentMock
            .Setup(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl, "resultaattypen"))
            .ReturnsAsync(new ServiceAgentResponse<ZaakTypeResponseDto>(zaaktype));

        var resolver = new ZaakTypeResolver(serviceAgentMock.Object, new GenericCache<ZaakTypeResponseDto>());

        var result = await resolver.ResolveAsync(
            new ZaakResponseDto { Zaaktype = ZaakTypeUrl },
            new Dictionary<string, object>(),
            new HashSet<string> { "zaaktype", "zaaktype.resultaattypen" }
        );

        Assert.Same(zaaktype, result);
        serviceAgentMock.Verify(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl, "resultaattypen"), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_CatalogusAndResultaattypenRequested_PassesBothExpandsToServiceAgent()
    {
        var zaaktype = new ZaakTypeResponseDto { Url = ZaakTypeUrl };
        var serviceAgentMock = new Mock<ICatalogiServiceAgentDecorator>();
        serviceAgentMock
            .Setup(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl, "catalogus,resultaattypen"))
            .ReturnsAsync(new ServiceAgentResponse<ZaakTypeResponseDto>(zaaktype));

        var resolver = new ZaakTypeResolver(serviceAgentMock.Object, new GenericCache<ZaakTypeResponseDto>());

        var result = await resolver.ResolveAsync(
            new ZaakResponseDto { Zaaktype = ZaakTypeUrl },
            new Dictionary<string, object>(),
            new HashSet<string> { "zaaktype", "zaaktype.catalogus", "zaaktype.resultaattypen" }
        );

        Assert.Same(zaaktype, result);
        serviceAgentMock.Verify(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl, "catalogus,resultaattypen"), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_StatustypenRequested_PassesExpandStatustypenToServiceAgent()
    {
        var zaaktype = new ZaakTypeResponseDto { Url = ZaakTypeUrl };
        var serviceAgentMock = new Mock<ICatalogiServiceAgentDecorator>();
        serviceAgentMock
            .Setup(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl, "statustypen"))
            .ReturnsAsync(new ServiceAgentResponse<ZaakTypeResponseDto>(zaaktype));

        var resolver = new ZaakTypeResolver(serviceAgentMock.Object, new GenericCache<ZaakTypeResponseDto>());

        var result = await resolver.ResolveAsync(
            new ZaakResponseDto { Zaaktype = ZaakTypeUrl },
            new Dictionary<string, object>(),
            new HashSet<string> { "zaaktype", "zaaktype.statustypen" }
        );

        Assert.Same(zaaktype, result);
        serviceAgentMock.Verify(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl, "statustypen"), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_SameZaaktypeWithAndWithoutCatalogus_IsNotMixedUpByTheSharedCache()
    {
        // Note: the top-level "zaaktype" and the nested "hoofdzaak.zaaktype.catalogus" share one cache within a request
        var withoutCatalogus = new ZaakTypeResponseDto { Url = ZaakTypeUrl };
        var withCatalogus = new ZaakTypeResponseDto { Url = ZaakTypeUrl };
        var serviceAgentMock = new Mock<ICatalogiServiceAgentDecorator>();
        serviceAgentMock
            .Setup(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl, null))
            .ReturnsAsync(new ServiceAgentResponse<ZaakTypeResponseDto>(withoutCatalogus));
        serviceAgentMock
            .Setup(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl, "catalogus"))
            .ReturnsAsync(new ServiceAgentResponse<ZaakTypeResponseDto>(withCatalogus));

        var resolver = new ZaakTypeResolver(serviceAgentMock.Object, new GenericCache<ZaakTypeResponseDto>());
        var entity = new ZaakResponseDto { Zaaktype = ZaakTypeUrl };

        var first = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "zaaktype" });
        var second = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "zaaktype", "zaaktype.catalogus" });
        var secondAgain = await resolver.ResolveAsync(
            entity,
            new Dictionary<string, object>(),
            new HashSet<string> { "zaaktype", "zaaktype.catalogus" }
        );

        Assert.Same(withoutCatalogus, first);
        Assert.Same(withCatalogus, second);
        Assert.Same(withCatalogus, secondAgain);
        serviceAgentMock.Verify(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl, It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ResolveAsync_SameZaaktypeWithAndWithoutResultaattypen_IsNotMixedUpByTheSharedCache()
    {
        var withoutResultaattypen = new ZaakTypeResponseDto { Url = ZaakTypeUrl };
        var withResultaattypen = new ZaakTypeResponseDto { Url = ZaakTypeUrl };
        var serviceAgentMock = new Mock<ICatalogiServiceAgentDecorator>();
        serviceAgentMock
            .Setup(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl, null))
            .ReturnsAsync(new ServiceAgentResponse<ZaakTypeResponseDto>(withoutResultaattypen));
        serviceAgentMock
            .Setup(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl, "resultaattypen"))
            .ReturnsAsync(new ServiceAgentResponse<ZaakTypeResponseDto>(withResultaattypen));

        var resolver = new ZaakTypeResolver(serviceAgentMock.Object, new GenericCache<ZaakTypeResponseDto>());
        var entity = new ZaakResponseDto { Zaaktype = ZaakTypeUrl };

        var first = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "zaaktype" });
        var second = await resolver.ResolveAsync(
            entity,
            new Dictionary<string, object>(),
            new HashSet<string> { "zaaktype", "zaaktype.resultaattypen" }
        );
        var secondAgain = await resolver.ResolveAsync(
            entity,
            new Dictionary<string, object>(),
            new HashSet<string> { "zaaktype", "zaaktype.resultaattypen" }
        );

        Assert.Same(withoutResultaattypen, first);
        Assert.Same(withResultaattypen, second);
        Assert.Same(withResultaattypen, secondAgain);
        serviceAgentMock.Verify(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl, It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ResolveAsync_ExpandOrderDoesNotDependOnTheOrderOfTheRequestedPaths()
    {
        var zaaktype = new ZaakTypeResponseDto { Url = ZaakTypeUrl };
        var serviceAgentMock = new Mock<ICatalogiServiceAgentDecorator>();
        serviceAgentMock
            .Setup(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl, "catalogus,resultaattypen"))
            .ReturnsAsync(new ServiceAgentResponse<ZaakTypeResponseDto>(zaaktype));

        var resolver = new ZaakTypeResolver(serviceAgentMock.Object, new GenericCache<ZaakTypeResponseDto>());
        var entity = new ZaakResponseDto { Zaaktype = ZaakTypeUrl };

        await resolver.ResolveAsync(
            entity,
            new Dictionary<string, object>(),
            new HashSet<string> { "zaaktype", "zaaktype.resultaattypen", "zaaktype.catalogus" }
        );
        await resolver.ResolveAsync(
            entity,
            new Dictionary<string, object>(),
            new HashSet<string> { "zaaktype", "zaaktype.catalogus", "zaaktype.resultaattypen" }
        );

        serviceAgentMock.Verify(a => a.GetZaakTypeByUrlAsync(ZaakTypeUrl, "catalogus,resultaattypen"), Times.Once);
    }

    [Fact]
    public async Task Path_IsZaaktype_AndHasNoParent()
    {
        var resolver = new ZaakTypeResolver(Mock.Of<ICatalogiServiceAgentDecorator>(), new GenericCache<ZaakTypeResponseDto>());

        Assert.Equal("zaaktype", resolver.Path);
        Assert.Null(resolver.Parent);
    }
}
