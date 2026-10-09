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

public class ZaakStatusStatusTypeResolverTests
{
    private const string StatusTypeUrl = "http://catalogi.local/api/v1/statustypen/22222222-2222-2222-2222-222222222222";

    [Fact]
    public async Task ResolveAsync_StatusResolvedWithStatustype_ReturnsStatustype()
    {
        var statustype = new StatusTypeResponseDto { Url = StatusTypeUrl };
        var serviceAgentMock = new Mock<ICatalogiServiceAgentDecorator>();
        serviceAgentMock
            .Setup(a => a.GetStatusTypeByUrlAsync(StatusTypeUrl))
            .ReturnsAsync(new ServiceAgentResponse<StatusTypeResponseDto>(statustype));

        var resolver = new ZaakStatusStatusTypeResolver(serviceAgentMock.Object, new GenericCache<StatusTypeResponseDto>());
        var status = new StatusResponseDto { StatusType = StatusTypeUrl };
        var resolved = new Dictionary<string, object> { ["status"] = status };

        var result = await resolver.ResolveAsync(new ZaakResponseDto(), resolved, new HashSet<string> { "status", "status.statustype" });

        Assert.Same(statustype, result);
    }

    [Fact]
    public async Task ResolveAsync_ParentStatusNotResolved_ReturnsNullWithoutQuerying()
    {
        var serviceAgentMock = new Mock<ICatalogiServiceAgentDecorator>();

        var resolver = new ZaakStatusStatusTypeResolver(serviceAgentMock.Object, new GenericCache<StatusTypeResponseDto>());

        var result = await resolver.ResolveAsync(
            new ZaakResponseDto(),
            new Dictionary<string, object>(),
            new HashSet<string> { "status.statustype" }
        );

        Assert.Null(result);
        serviceAgentMock.Verify(a => a.GetStatusTypeByUrlAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Path_And_Parent_AreCorrect()
    {
        var resolver = new ZaakStatusStatusTypeResolver(Mock.Of<ICatalogiServiceAgentDecorator>(), new GenericCache<StatusTypeResponseDto>());

        Assert.Equal("status.statustype", resolver.Path);
        Assert.Equal("status", resolver.Parent);
    }
}
