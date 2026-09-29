using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Expands;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakObject;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakObjectZaakObjectTypeResolverTests
{
    private const string ZaakObjectTypeUrl = "http://catalogi.local/api/v1/zaakobjecttypen/66666666-6666-6666-6666-666666666666";

    [Fact]
    public async Task ResolveAsync_ZaakObjectTypeFound_CallsServiceAgentAndReturnsZaakObjectType()
    {
        var zaakobjecttype = new ZaakObjectTypeResponseDto { Url = ZaakObjectTypeUrl };
        var serviceAgentMock = new Mock<ICatalogiServiceAgentDecorator>();
        serviceAgentMock
            .Setup(a => a.GetZaakObjectTypeByUrlAsync(ZaakObjectTypeUrl))
            .ReturnsAsync(new ServiceAgentResponse<ZaakObjectTypeResponseDto>(zaakobjecttype));

        var resolver = new ZaakObjectZaakObjectTypeResolver(serviceAgentMock.Object, new GenericCache<ZaakObjectTypeResponseDto>());
        var entity = new ZaakObjectResponseDto { ZaakObjectType = ZaakObjectTypeUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "zaakobjecttype" });

        Assert.Same(zaakobjecttype, result);
        serviceAgentMock.Verify(a => a.GetZaakObjectTypeByUrlAsync(ZaakObjectTypeUrl), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_NoZaakObjectTypeUrl_ReturnsNullWithoutCallingServiceAgent()
    {
        // ZaakObjectType is optional per the VNG spec (unlike ROL's RolType, which is required), so a
        // ZAAKOBJECT with no zaakobjecttype set must resolve to null rather than call the ZTC with an
        // empty/null url -- reproduces the production 502 ("Externe service 'ZTC' niet beschikbaar")
        // seen at GET /zaken?expand=...,zaakobjecten.zaakobjecttype for a ZAAKOBJECT without one.
        var serviceAgentMock = new Mock<ICatalogiServiceAgentDecorator>();

        var resolver = new ZaakObjectZaakObjectTypeResolver(serviceAgentMock.Object, new GenericCache<ZaakObjectTypeResponseDto>());
        var entity = new ZaakObjectResponseDto { ZaakObjectType = null };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "zaakobjecttype" });

        Assert.Null(result);
        serviceAgentMock.Verify(a => a.GetZaakObjectTypeByUrlAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Path_IsZaakobjecttype_AndHasNoParent()
    {
        var resolver = new ZaakObjectZaakObjectTypeResolver(Mock.Of<ICatalogiServiceAgentDecorator>(), new GenericCache<ZaakObjectTypeResponseDto>());

        Assert.Equal("zaakobjecttype", resolver.Path);
        Assert.Null(resolver.Parent);
    }
}
