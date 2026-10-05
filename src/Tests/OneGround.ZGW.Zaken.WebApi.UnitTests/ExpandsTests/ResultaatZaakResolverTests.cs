using System;
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

public class ResultaatZaakResolverTests
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

        var resolver = new ResultaatZaakResolver(mediatorMock.Object, mapperMock.Object, EmptyZaakExpandEngine());
        var entity = new ResultaatResponseDto { Zaak = ZaakUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "zaak" });

        Assert.Same(mappedZaak, result);
    }

    [Fact]
    public async Task ResolveAsync_ZaakNotFound_ThrowsExpandInternalQueryHandlerException()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<Zaak>(null, QueryStatus.NotFound));

        var resolver = new ResultaatZaakResolver(mediatorMock.Object, Mock.Of<IMapper>(), EmptyZaakExpandEngine());
        var entity = new ResultaatResponseDto { Zaak = ZaakUrl };

        var ex = await Assert.ThrowsAsync<ExpandInternalQueryHandlerException>(() =>
            resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "zaak" })
        );

        Assert.Equal("zaak", ex.Resource);
        Assert.Equal(QueryStatus.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task ResolveAsync_ZaakForbidden_ThrowsExpandInternalQueryHandlerException()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<Zaak>(null, QueryStatus.Forbidden));

        var resolver = new ResultaatZaakResolver(mediatorMock.Object, Mock.Of<IMapper>(), EmptyZaakExpandEngine());
        var entity = new ResultaatResponseDto { Zaak = ZaakUrl };

        var ex = await Assert.ThrowsAsync<ExpandInternalQueryHandlerException>(() =>
            resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "zaak" })
        );

        Assert.Equal("zaak", ex.Resource);
        Assert.Equal(QueryStatus.Forbidden, ex.StatusCode);
    }

    [Fact]
    public async Task ResolveAsync_NoZaakUrl_ReturnsNullWithoutQuerying()
    {
        var mediatorMock = new Mock<IMediator>();

        var resolver = new ResultaatZaakResolver(mediatorMock.Object, Mock.Of<IMapper>(), EmptyZaakExpandEngine());
        var entity = new ResultaatResponseDto { Zaak = null };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "zaak" });

        Assert.Null(result);
        mediatorMock.Verify(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_ZaakZaaktypeRequested_DelegatesToZaakExpandEngineResolveAsync()
    {
        var zaak = new Zaak { Id = new("11111111-1111-1111-1111-111111111111") };
        var mappedZaak = new ZaakResponseDto { Uuid = zaak.Id.ToString() };
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.Is<GetZaakQuery>(q => q.Id == zaak.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<Zaak>(zaak, QueryStatus.OK));
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<ZaakResponseDto>(zaak)).Returns(mappedZaak);

        var zaaktypeResolverMock = new Mock<IExpandResolver<ZaakResponseDto>>();
        zaaktypeResolverMock.SetupGet(r => r.Path).Returns("zaaktype");
        zaaktypeResolverMock.SetupGet(r => r.Parent).Returns((string)null);
        var zaakExpandEngine = new Lazy<ExpandEngine<ZaakResponseDto>>(() => new ExpandEngine<ZaakResponseDto>([zaaktypeResolverMock.Object]));

        var resolver = new ResultaatZaakResolver(mediatorMock.Object, mapperMock.Object, zaakExpandEngine);
        var entity = new ResultaatResponseDto { Zaak = ZaakUrl };

        await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "zaak", "zaak.zaaktype" });

        zaaktypeResolverMock.Verify(
            r => r.ResolveAsync(mappedZaak, It.IsAny<IReadOnlyDictionary<string, object>>(), It.IsAny<IReadOnlySet<string>>()),
            Times.Once
        );
    }

    [Fact]
    public void Path_IsZaak_AndHasNoParent_AndDeclaresZaakZaaktypeAsAdditionalPath()
    {
        var resolver = new ResultaatZaakResolver(Mock.Of<IMediator>(), Mock.Of<IMapper>(), EmptyZaakExpandEngine());

        Assert.Equal("zaak", resolver.Path);
        Assert.Null(resolver.Parent);
        Assert.Equal([("zaak.zaaktype", "zaak")], resolver.AdditionalPaths);
    }

    private static Lazy<ExpandEngine<ZaakResponseDto>> EmptyZaakExpandEngine() => new(() => new ExpandEngine<ZaakResponseDto>([]));
}
