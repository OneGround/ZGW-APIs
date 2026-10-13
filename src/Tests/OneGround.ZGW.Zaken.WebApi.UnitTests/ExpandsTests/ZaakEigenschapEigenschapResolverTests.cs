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

public class ZaakEigenschapEigenschapResolverTests
{
    private const string EigenschapUrl = "http://catalogi.local/api/v1/eigenschappen/66666666-6666-6666-6666-666666666666";

    [Fact]
    public async Task ResolveAsync_EigenschapFound_CallsServiceAgentAndReturnsEigenschap()
    {
        var eigenschap = new EigenschapResponseDto { Url = EigenschapUrl };
        var serviceAgentMock = new Mock<ICatalogiServiceAgentDecorator>();
        serviceAgentMock
            .Setup(a => a.GetEigenschapByUrlAsync(EigenschapUrl))
            .ReturnsAsync(new ServiceAgentResponse<EigenschapResponseDto>(eigenschap));

        var resolver = new ZaakEigenschapEigenschapResolver(serviceAgentMock.Object, new GenericCache<EigenschapResponseDto>());
        var entity = new ZaakEigenschapResponseDto { Eigenschap = EigenschapUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "eigenschap" });

        Assert.Same(eigenschap, result);
        serviceAgentMock.Verify(a => a.GetEigenschapByUrlAsync(EigenschapUrl), Times.Once);
    }

    [Fact]
    public void Path_IsEigenschap_AndHasNoParent()
    {
        var resolver = new ZaakEigenschapEigenschapResolver(Mock.Of<ICatalogiServiceAgentDecorator>(), new GenericCache<EigenschapResponseDto>());

        Assert.Equal("eigenschap", resolver.Path);
        Assert.Null(resolver.Parent);
    }
}
