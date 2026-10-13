using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1._3.Expands;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakRol;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class RolRolTypeResolverTests
{
    private const string RolTypeUrl = "http://catalogi.local/api/v1/roltypen/66666666-6666-6666-6666-666666666666";

    [Fact]
    public async Task ResolveAsync_RolTypeFound_CallsServiceAgentAndReturnsRolType()
    {
        var roltype = new RolTypeResponseDto { Url = RolTypeUrl };
        var serviceAgentMock = new Mock<ICatalogiServiceAgentDecorator>();
        serviceAgentMock.Setup(a => a.GetRolTypeByUrlAsync(RolTypeUrl)).ReturnsAsync(new ServiceAgentResponse<RolTypeResponseDto>(roltype));

        var resolver = new RolRolTypeResolver(serviceAgentMock.Object, new GenericCache<RolTypeResponseDto>());
        var entity = new RolResponseDto { RolType = RolTypeUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "roltype" });

        Assert.Same(roltype, result);
        serviceAgentMock.Verify(a => a.GetRolTypeByUrlAsync(RolTypeUrl), Times.Once);
    }

    [Fact]
    public void Path_IsRoltype_AndHasNoParent()
    {
        var resolver = new RolRolTypeResolver(Mock.Of<ICatalogiServiceAgentDecorator>(), new GenericCache<RolTypeResponseDto>());

        Assert.Equal("roltype", resolver.Path);
        Assert.Null(resolver.Parent);
    }
}
