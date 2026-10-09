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

public class StatusStatusTypeResolverTests
{
    private const string StatusTypeUrl = "http://catalogi.local/api/v1/statustypen/22222222-2222-2222-2222-222222222222";

    [Fact]
    public async Task ResolveAsync_StatusTypeFound_CallsServiceAgentAndReturnsStatusType()
    {
        var statustype = new StatusTypeResponseDto { Url = StatusTypeUrl };
        var serviceAgentMock = new Mock<ICatalogiServiceAgentDecorator>();
        serviceAgentMock
            .Setup(a => a.GetStatusTypeByUrlAsync(StatusTypeUrl))
            .ReturnsAsync(new ServiceAgentResponse<StatusTypeResponseDto>(statustype));

        var resolver = new StatusStatusTypeResolver(serviceAgentMock.Object, new GenericCache<StatusTypeResponseDto>());
        var entity = new StatusResponseDto { StatusType = StatusTypeUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "statustype" });

        Assert.Same(statustype, result);
        serviceAgentMock.Verify(a => a.GetStatusTypeByUrlAsync(StatusTypeUrl), Times.Once);
    }

    [Fact]
    public void Path_IsStatustype_AndHasNoParent()
    {
        var resolver = new StatusStatusTypeResolver(Mock.Of<ICatalogiServiceAgentDecorator>(), new GenericCache<StatusTypeResponseDto>());

        Assert.Equal("statustype", resolver.Path);
        Assert.Null(resolver.Parent);
    }
}
