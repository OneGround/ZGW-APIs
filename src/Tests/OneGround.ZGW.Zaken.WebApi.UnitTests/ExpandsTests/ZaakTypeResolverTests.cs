using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Expands;
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
    public async Task Path_IsZaaktype_AndHasNoParent()
    {
        var resolver = new ZaakTypeResolver(Mock.Of<ICatalogiServiceAgentDecorator>(), new GenericCache<ZaakTypeResponseDto>());

        Assert.Equal("zaaktype", resolver.Path);
        Assert.Null(resolver.Parent);
    }
}
