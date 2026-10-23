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

public class ZaakResultaatResultaatTypeResolverTests
{
    private const string ResultaatTypeUrl = "http://catalogi.local/api/v1/resultaattypen/44444444-4444-4444-4444-444444444444";

    [Fact]
    public async Task ResolveAsync_ResultaatResolvedWithResultaattype_ReturnsResultaattype()
    {
        var resultaattype = new ResultaatTypeResponseDto { Url = ResultaatTypeUrl };
        var serviceAgentMock = new Mock<ICatalogiServiceAgentDecorator>();
        serviceAgentMock
            .Setup(a => a.GetResultaatTypeByUrlAsync(ResultaatTypeUrl))
            .ReturnsAsync(new ServiceAgentResponse<ResultaatTypeResponseDto>(resultaattype));

        var resolver = new ZaakResultaatResultaatTypeResolver(serviceAgentMock.Object, new GenericCache<ResultaatTypeResponseDto>());
        var resultaat = new ResultaatResponseDto { ResultaatType = ResultaatTypeUrl };
        var resolved = new Dictionary<string, object> { ["resultaat"] = resultaat };

        var result = await resolver.ResolveAsync(new ZaakResponseDto(), resolved, new HashSet<string> { "resultaat", "resultaat.resultaattype" });

        Assert.Same(resultaattype, result);
    }

    [Fact]
    public async Task ResolveAsync_ParentResultaatNotResolved_ReturnsNullWithoutQuerying()
    {
        var serviceAgentMock = new Mock<ICatalogiServiceAgentDecorator>();

        var resolver = new ZaakResultaatResultaatTypeResolver(serviceAgentMock.Object, new GenericCache<ResultaatTypeResponseDto>());

        var result = await resolver.ResolveAsync(
            new ZaakResponseDto(),
            new Dictionary<string, object>(),
            new HashSet<string> { "resultaat.resultaattype" }
        );

        Assert.Null(result);
        serviceAgentMock.Verify(a => a.GetResultaatTypeByUrlAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Path_And_Parent_AreCorrect()
    {
        var resolver = new ZaakResultaatResultaatTypeResolver(
            Mock.Of<ICatalogiServiceAgentDecorator>(),
            new GenericCache<ResultaatTypeResponseDto>()
        );

        Assert.Equal("resultaat.resultaattype", resolver.Path);
        Assert.Equal("resultaat", resolver.Parent);
    }
}
