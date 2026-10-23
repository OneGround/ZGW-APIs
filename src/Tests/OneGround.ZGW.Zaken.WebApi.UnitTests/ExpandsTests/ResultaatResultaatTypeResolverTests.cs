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

public class ResultaatResultaatTypeResolverTests
{
    private const string ResultaatTypeUrl = "http://catalogi.local/api/v1/resultaattypen/44444444-4444-4444-4444-444444444444";

    [Fact]
    public async Task ResolveAsync_ResultaatTypeFound_CallsServiceAgentAndReturnsResultaatType()
    {
        var resultaattype = new ResultaatTypeResponseDto { Url = ResultaatTypeUrl };
        var serviceAgentMock = new Mock<ICatalogiServiceAgentDecorator>();
        serviceAgentMock
            .Setup(a => a.GetResultaatTypeByUrlAsync(ResultaatTypeUrl))
            .ReturnsAsync(new ServiceAgentResponse<ResultaatTypeResponseDto>(resultaattype));

        var resolver = new ResultaatResultaatTypeResolver(serviceAgentMock.Object, new GenericCache<ResultaatTypeResponseDto>());
        var entity = new ResultaatResponseDto { ResultaatType = ResultaatTypeUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "resultaattype" });

        Assert.Same(resultaattype, result);
        serviceAgentMock.Verify(a => a.GetResultaatTypeByUrlAsync(ResultaatTypeUrl), Times.Once);
    }

    [Fact]
    public void Path_IsResultaattype_AndHasNoParent()
    {
        var resolver = new ResultaatResultaatTypeResolver(Mock.Of<ICatalogiServiceAgentDecorator>(), new GenericCache<ResultaatTypeResponseDto>());

        Assert.Equal("resultaattype", resolver.Path);
        Assert.Null(resolver.Parent);
    }
}
