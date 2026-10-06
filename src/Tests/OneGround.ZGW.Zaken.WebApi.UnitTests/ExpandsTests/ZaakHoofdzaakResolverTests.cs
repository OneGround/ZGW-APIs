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

public class ZaakHoofdzaakResolverTests
{
    private const string HoofdzaakUrl = "https://zrc.test/zaken/44444444-4444-4444-4444-444444444444";

    private static Lazy<ExpandEngine<ZaakResponseDto>> LazyEngine(ExpandEngine<ZaakResponseDto> expandEngine) => new(() => expandEngine);

    [Fact]
    public async Task ResolveAsync_HoofdzaakOnlyRequested_SendsGetZaakQueryForParsedIdAndReturnsMappedZaakWithoutResolvingNestedPaths()
    {
        var hoofdzaak = new Zaak { Id = new("44444444-4444-4444-4444-444444444444") };
        var mappedHoofdzaak = new ZaakResponseDto { Uuid = hoofdzaak.Id.ToString() };
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.Is<GetZaakQuery>(q => q.Id == hoofdzaak.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<Zaak>(hoofdzaak, QueryStatus.OK));
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<ZaakResponseDto>(hoofdzaak)).Returns(mappedHoofdzaak);

        var zaaktypeResolverMock = new Mock<IExpandResolver<ZaakResponseDto>>();
        zaaktypeResolverMock.SetupGet(r => r.Path).Returns("zaaktype");
        zaaktypeResolverMock.SetupGet(r => r.Parent).Returns((string)null);
        var expandEngine = new ExpandEngine<ZaakResponseDto>([zaaktypeResolverMock.Object]);

        var resolver = new ZaakHoofdzaakResolver(mediatorMock.Object, mapperMock.Object, LazyEngine(expandEngine));
        var entity = new ZaakResponseDto { Hoofdzaak = HoofdzaakUrl };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "hoofdzaak" });

        Assert.Same(mappedHoofdzaak, result);
        zaaktypeResolverMock.Verify(
            r => r.ResolveAsync(It.IsAny<ZaakResponseDto>(), It.IsAny<IReadOnlyDictionary<string, object>>(), It.IsAny<IReadOnlySet<string>>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ResolveAsync_HoofdzaakStatusRequested_ForwardsRelativePathToReusedExpandEngine()
    {
        var hoofdzaak = new Zaak { Id = new("44444444-4444-4444-4444-444444444444") };
        var mappedHoofdzaak = new ZaakResponseDto { Uuid = hoofdzaak.Id.ToString() };
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<Zaak>(hoofdzaak, QueryStatus.OK));
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<ZaakResponseDto>(hoofdzaak)).Returns(mappedHoofdzaak);

        var statusResolverMock = new Mock<IExpandResolver<ZaakResponseDto>>();
        statusResolverMock.SetupGet(r => r.Path).Returns("status");
        statusResolverMock.SetupGet(r => r.Parent).Returns((string)null);
        var expandEngine = new ExpandEngine<ZaakResponseDto>([statusResolverMock.Object]);

        var resolver = new ZaakHoofdzaakResolver(mediatorMock.Object, mapperMock.Object, LazyEngine(expandEngine));
        var entity = new ZaakResponseDto { Hoofdzaak = HoofdzaakUrl };

        await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "hoofdzaak", "hoofdzaak.status" });

        statusResolverMock.Verify(
            r => r.ResolveAsync(mappedHoofdzaak, It.IsAny<IReadOnlyDictionary<string, object>>(), It.IsAny<IReadOnlySet<string>>()),
            Times.Once
        );
        Assert.NotNull(mappedHoofdzaak.Expand);
    }

    [Fact]
    public async Task ResolveAsync_HoofdzaakNotFound_ThrowsExpandInternalQueryHandlerException()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<Zaak>(null, QueryStatus.NotFound));

        var resolver = new ZaakHoofdzaakResolver(mediatorMock.Object, Mock.Of<IMapper>(), LazyEngine(new ExpandEngine<ZaakResponseDto>([])));
        var entity = new ZaakResponseDto { Hoofdzaak = HoofdzaakUrl };

        var ex = await Assert.ThrowsAsync<ExpandInternalQueryHandlerException>(() =>
            resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "hoofdzaak" })
        );

        Assert.Equal("hoofdzaak", ex.Resource);
        Assert.Equal(QueryStatus.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task ResolveAsync_HoofdzaakForbidden_ThrowsExpandInternalQueryHandlerException()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<Zaak>(null, QueryStatus.Forbidden));

        var resolver = new ZaakHoofdzaakResolver(mediatorMock.Object, Mock.Of<IMapper>(), LazyEngine(new ExpandEngine<ZaakResponseDto>([])));
        var entity = new ZaakResponseDto { Hoofdzaak = HoofdzaakUrl };

        var ex = await Assert.ThrowsAsync<ExpandInternalQueryHandlerException>(() =>
            resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "hoofdzaak" })
        );

        Assert.Equal("hoofdzaak", ex.Resource);
        Assert.Equal(QueryStatus.Forbidden, ex.StatusCode);
    }

    [Fact]
    public async Task ResolveAsync_NoHoofdzaakUrl_ReturnsNullWithoutQuerying()
    {
        var mediatorMock = new Mock<IMediator>();

        var resolver = new ZaakHoofdzaakResolver(mediatorMock.Object, Mock.Of<IMapper>(), LazyEngine(new ExpandEngine<ZaakResponseDto>([])));
        var entity = new ZaakResponseDto { Hoofdzaak = null };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "hoofdzaak" });

        Assert.Null(result);
        mediatorMock.Verify(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_HoofdzaakDeelzakenZaaktypeRequested_ForwardsRelativePathsToReusedExpandEngine()
    {
        var hoofdzaak = new Zaak { Id = new("44444444-4444-4444-4444-444444444444") };
        var mappedHoofdzaak = new ZaakResponseDto { Uuid = hoofdzaak.Id.ToString() };
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<Zaak>(hoofdzaak, QueryStatus.OK));
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<ZaakResponseDto>(hoofdzaak)).Returns(mappedHoofdzaak);

        var deelzakenResolverMock = new Mock<IExpandResolver<ZaakResponseDto>>();
        deelzakenResolverMock.SetupGet(r => r.Path).Returns("deelzaken");
        deelzakenResolverMock.SetupGet(r => r.Parent).Returns((string)null);
        var expandEngine = new ExpandEngine<ZaakResponseDto>([deelzakenResolverMock.Object]);

        var resolver = new ZaakHoofdzaakResolver(mediatorMock.Object, mapperMock.Object, LazyEngine(expandEngine));
        var entity = new ZaakResponseDto { Hoofdzaak = HoofdzaakUrl };

        await resolver.ResolveAsync(
            entity,
            new Dictionary<string, object>(),
            new HashSet<string> { "hoofdzaak", "hoofdzaak.deelzaken", "hoofdzaak.deelzaken.zaaktype" }
        );

        // Verifies the double prefix-stripping actually happened: the mock must see "deelzaken.zaaktype"
        // (both "hoofdzaak." layers stripped), not the raw "hoofdzaak.deelzaken.zaaktype", and not an
        // empty set either.
        deelzakenResolverMock.Verify(
            r =>
                r.ResolveAsync(
                    mappedHoofdzaak,
                    It.IsAny<IReadOnlyDictionary<string, object>>(),
                    It.Is<IReadOnlySet<string>>(p => p.Contains("deelzaken") && p.Contains("deelzaken.zaaktype") && p.Count == 2)
                ),
            Times.Once
        );
    }

    [Fact]
    public void Path_IsHoofdzaak_AndHasNoParent_AndDeclaresExpectedAdditionalPaths()
    {
        var resolver = new ZaakHoofdzaakResolver(Mock.Of<IMediator>(), Mock.Of<IMapper>(), LazyEngine(new ExpandEngine<ZaakResponseDto>([])));

        Assert.Equal("hoofdzaak", resolver.Path);
        Assert.Null(resolver.Parent);
        Assert.Equal(
            [
                ("hoofdzaak.zaaktype", "hoofdzaak"),
                ("hoofdzaak.zaaktype.catalogus", "hoofdzaak.zaaktype"),
                ("hoofdzaak.status", "hoofdzaak"),
                ("hoofdzaak.status.statustype", "hoofdzaak.status"),
                ("hoofdzaak.resultaat", "hoofdzaak"),
                ("hoofdzaak.resultaat.resultaattype", "hoofdzaak.resultaat"),
                ("hoofdzaak.rollen", "hoofdzaak"),
                ("hoofdzaak.rollen.roltype", "hoofdzaak.rollen"),
                ("hoofdzaak.zaakobjecten", "hoofdzaak"),
                ("hoofdzaak.zaakobjecten.zaakobjecttype", "hoofdzaak.zaakobjecten"),
                ("hoofdzaak.zaakinformatieobjecten", "hoofdzaak"),
                ("hoofdzaak.zaakinformatieobjecten.informatieobject", "hoofdzaak.zaakinformatieobjecten"),
                ("hoofdzaak.deelzaken", "hoofdzaak"),
                ("hoofdzaak.deelzaken.zaaktype", "hoofdzaak.deelzaken"),
                ("hoofdzaak.deelzaken.status", "hoofdzaak.deelzaken"),
                ("hoofdzaak.deelzaken.resultaat", "hoofdzaak.deelzaken"),
            ],
            resolver.AdditionalPaths
        );
    }
}
