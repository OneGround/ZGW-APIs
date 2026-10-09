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
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakEigenschapZaakResolverTests
{
    private const string ZaakUrl = "https://zrc.test/zaken/11111111-1111-1111-1111-111111111111";

    [Fact]
    public async Task ResolveAsync_ZaakFound_SendsGetZaakQueryForParsedIdAndReturnsMappedZaak()
    {
        var zaak = new Zaak { Id = new("11111111-1111-1111-1111-111111111111") };
        var mappedZaak = new ZaakResponseDto { Uuid = zaak.Id.ToString() };
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.Is<GetZaakQuery>(q => q.Id == zaak.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<Zaak>(zaak, QueryStatus.OK));
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<ZaakResponseDto>(zaak)).Returns(mappedZaak);

        var resolver = new ZaakEigenschapZaakResolver(ExpandCaches.ZaakLookup(mediatorMock.Object), mapperMock.Object);
        var entity = new ZaakEigenschapResponseDto { Zaak = ZaakUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "zaak" });

        Assert.Same(mappedZaak, result);
    }

    [Fact]
    public async Task ResolveAsync_ZaakNotFound_ResolvesToNullInsteadOfFailingTheRequest()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<Zaak>(null, QueryStatus.NotFound));

        var resolver = new ZaakEigenschapZaakResolver(ExpandCaches.ZaakLookup(mediatorMock.Object), Mock.Of<IMapper>());
        var entity = new ZaakEigenschapResponseDto { Zaak = ZaakUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "zaak" });

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_NoZaakUrl_ReturnsNullWithoutQuerying()
    {
        var mediatorMock = new Mock<IMediator>();

        var resolver = new ZaakEigenschapZaakResolver(ExpandCaches.ZaakLookup(mediatorMock.Object), Mock.Of<IMapper>());
        var entity = new ZaakEigenschapResponseDto { Zaak = null };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "zaak" });

        Assert.Null(result);
        mediatorMock.Verify(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Path_IsZaak_AndHasNoParent()
    {
        var resolver = new ZaakEigenschapZaakResolver(ExpandCaches.ZaakLookup(Mock.Of<IMediator>()), Mock.Of<IMapper>());

        Assert.Equal("zaak", resolver.Path);
        Assert.Null(resolver.Parent);
    }
}
