using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Moq;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using OneGround.ZGW.Zaken.Web.Handlers.v1;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakResultaatResolverTests
{
    private const string ResultaatUrl = "https://zrc.test/resultaten/55555555-5555-5555-5555-555555555555";

    [Fact]
    public async Task ResolveAsync_ResultaatFound_SendsGetZaakResultaatQueryForParsedIdAndReturnsMappedResultaat()
    {
        var zaakResultaat = new ZaakResultaat { Id = new("55555555-5555-5555-5555-555555555555") };
        var mappedResultaat = new ResultaatResponseDto { Uuid = zaakResultaat.Id.ToString() };
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.Is<GetZaakResultaatQuery>(q => q.Id == zaakResultaat.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<ZaakResultaat>(zaakResultaat, QueryStatus.OK));
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<ResultaatResponseDto>(zaakResultaat)).Returns(mappedResultaat);

        var resolver = new ZaakResultaatResolver(mediatorMock.Object, mapperMock.Object);
        var entity = new ZaakResponseDto { Resultaat = ResultaatUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "resultaat" });

        Assert.Same(mappedResultaat, result);
    }

    [Fact]
    public async Task ResolveAsync_ResultaatNotFound_ResolvesToNullInsteadOfFailingTheRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetZaakResultaatQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<ZaakResultaat>(null, QueryStatus.NotFound));

        var resolver = new ZaakResultaatResolver(mediatorMock.Object, Mock.Of<IMapper>());
        var entity = new ZaakResponseDto { Resultaat = ResultaatUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "resultaat" });

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_ResultaatForbidden_ResolvesToNullInsteadOfFailingTheRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetZaakResultaatQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<ZaakResultaat>(null, QueryStatus.Forbidden));

        var resolver = new ZaakResultaatResolver(mediatorMock.Object, Mock.Of<IMapper>());
        var entity = new ZaakResponseDto { Resultaat = ResultaatUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "resultaat" });

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_NoResultaatUrl_ReturnsNullWithoutQuerying()
    {
        var mediatorMock = new Mock<IMediator>();

        var resolver = new ZaakResultaatResolver(mediatorMock.Object, Mock.Of<IMapper>());
        var entity = new ZaakResponseDto { Resultaat = null };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "resultaat" });

        Assert.Null(result);
        mediatorMock.Verify(m => m.Send(It.IsAny<GetZaakResultaatQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Path_IsResultaat_AndHasNoParent()
    {
        var resolver = new ZaakResultaatResolver(Mock.Of<IMediator>(), Mock.Of<IMapper>());

        Assert.Equal("resultaat", resolver.Path);
        Assert.Null(resolver.Parent);
    }
}
