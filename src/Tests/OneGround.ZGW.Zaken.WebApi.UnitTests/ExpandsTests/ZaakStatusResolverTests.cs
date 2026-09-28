using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Moq;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakStatusResolverTests
{
    private const string StatusUrl = "https://zrc.test/statussen/33333333-3333-3333-3333-333333333333";

    [Fact]
    public async Task ResolveAsync_StatusFound_SendsGetZaakStatusQueryForParsedIdAndReturnsMappedStatus()
    {
        var zaakStatus = new ZaakStatus { Id = new("33333333-3333-3333-3333-333333333333") };
        var mappedStatus = new StatusResponseDto { Uuid = zaakStatus.Id.ToString() };
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.Is<GetZaakStatusQuery>(q => q.Id == zaakStatus.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<ZaakStatus>(zaakStatus, QueryStatus.OK));
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<StatusResponseDto>(zaakStatus)).Returns(mappedStatus);

        var resolver = new ZaakStatusResolver(mediatorMock.Object, mapperMock.Object);
        var entity = new ZaakResponseDto { Status = StatusUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "status" });

        Assert.Same(mappedStatus, result);
    }

    [Fact]
    public async Task ResolveAsync_StatusNotFound_ReturnsNull()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetZaakStatusQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<ZaakStatus>(null, QueryStatus.NotFound));

        var resolver = new ZaakStatusResolver(mediatorMock.Object, Mock.Of<IMapper>());
        var entity = new ZaakResponseDto { Status = StatusUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "status" });

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_NoStatusUrl_ReturnsNullWithoutQuerying()
    {
        var mediatorMock = new Mock<IMediator>();

        var resolver = new ZaakStatusResolver(mediatorMock.Object, Mock.Of<IMapper>());
        var entity = new ZaakResponseDto { Status = null };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "status" });

        Assert.Null(result);
        mediatorMock.Verify(m => m.Send(It.IsAny<GetZaakStatusQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Path_IsStatus_AndHasNoParent()
    {
        var resolver = new ZaakStatusResolver(Mock.Of<IMediator>(), Mock.Of<IMapper>());

        Assert.Equal("status", resolver.Path);
        Assert.Null(resolver.Parent);
    }
}
