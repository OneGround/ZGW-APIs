using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakDeelzakenResolverTests
{
    private const string ZaakUrl = "https://zrc.test/zaken/11111111-1111-1111-1111-111111111111";
    private const string DeelzaakUrl1 = "https://zrc.test/zaken/22222222-2222-2222-2222-222222222222";
    private const string DeelzaakUrl2 = "https://zrc.test/zaken/33333333-3333-3333-3333-333333333333";

    private static readonly ZaakResponseDto EntityWithTwoDeelzaken = new() { Url = ZaakUrl, Deelzaken = [DeelzaakUrl1, DeelzaakUrl2] };

    // ZaakDeelzakenResolver resolves IMediator from a fresh DI scope per call (see the class doc
    // comment: GetAllZakenQuery's handler can create a session-scoped PostgreSQL temp table). A real
    // ServiceCollection exercises that CreateScope() call for real, unlike a bare IMediator mock
    // would. The ExpandEngine<ZaakResponseDto> reuse for nested paths is resolved lazily from this
    // same provider too (see ZaakHoofdzaakResolver's own remarks for why).
    private static IServiceProvider BuildServiceProvider(IMediator mediator, ExpandEngine<ZaakResponseDto> expandEngine = null)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => mediator);
        services.AddSingleton(expandEngine ?? new ExpandEngine<ZaakResponseDto>([]));
        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData("EPSG:4326", 4326)]
    [InlineData("EPSG:4937", 4937)]
    [InlineData("EPSG:28992", 28992)]
    [InlineData(null, 28992)] // no header: the default of the query (the CRS in which the geometry is stored)
    [InlineData("EPSG:9999", 28992)] // not supported: never fail inside an expand
    public async Task ResolveAsync_AcceptCrsHeader_DeterminesTheSridOfTheFetchedDeelzaken(string acceptCrs, int expectedSrid)
    {
        GetAllZakenQuery sent = null;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllZakenQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<QueryResult<PagedResult<Zaak>>>, CancellationToken>((q, _) => sent = (GetAllZakenQuery)q)
            .ReturnsAsync(new QueryResult<PagedResult<Zaak>>(new PagedResult<Zaak> { PageResult = [], Count = 0 }, QueryStatus.OK));

        var resolver = new ZaakDeelzakenResolver(
            BuildServiceProvider(mediatorMock.Object),
            Mock.Of<IMapper>(),
            AcceptCrsTestHelper.AccessorWith(acceptCrs)
        );

        await resolver.ResolveAsync(EntityWithTwoDeelzaken, new Dictionary<string, object>(), new HashSet<string> { "deelzaken" });

        Assert.NotNull(sent);
        Assert.Equal(expectedSrid, sent.SRID);
    }

    [Fact]
    public async Task ResolveAsync_ZaakHasDeelzaken_SendsGetAllZakenQueryFilteredByUuidInAndSizedToDeelzaakCountWithNullSafeFilterAndReturnsMappedList()
    {
        var deelzaak1 = new Zaak { Id = new("22222222-2222-2222-2222-222222222222") };
        var deelzaak2 = new Zaak { Id = new("33333333-3333-3333-3333-333333333333") };
        var pagedResult = new PagedResult<Zaak> { PageResult = [deelzaak1, deelzaak2], Count = 2 };
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m =>
                m.Send(
                    It.Is<GetAllZakenQuery>(q =>
                        q.GetAllZakenFilter.Uuid__in.Count == 2
                        && q.GetAllZakenFilter.Uuid__in.Contains(deelzaak1.Id)
                        && q.GetAllZakenFilter.Uuid__in.Contains(deelzaak2.Id)
                        // Guards against the NullReferenceException risk documented on the resolver:
                        // GetAllZakenQueryHandler's filter chain calls .Any() on these unconditionally.
                        && q.GetAllZakenFilter.Zaaktype__in != null
                        && q.GetAllZakenFilter.Archiefnominatie__in != null
                        && q.GetAllZakenFilter.Archiefstatus__in != null
                        && q.GetAllZakenFilter.Bronorganisatie__in != null
                        && q.Pagination.Page == 1
                        && q.Pagination.Size == 2
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new QueryResult<PagedResult<Zaak>>(pagedResult, QueryStatus.OK));
        var mappedDeelzaak1 = new ZaakResponseDto { Uuid = deelzaak1.Id.ToString() };
        var mappedDeelzaak2 = new ZaakResponseDto { Uuid = deelzaak2.Id.ToString() };
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<ZaakResponseDto>>(pagedResult.PageResult)).Returns([mappedDeelzaak1, mappedDeelzaak2]);

        var resolver = new ZaakDeelzakenResolver(BuildServiceProvider(mediatorMock.Object), mapperMock.Object);

        var result = await resolver.ResolveAsync(EntityWithTwoDeelzaken, new Dictionary<string, object>(), new HashSet<string> { "deelzaken" });

        var deelzaken = Assert.IsType<List<ZaakResponseDto>>(result);
        Assert.Equal([mappedDeelzaak1, mappedDeelzaak2], deelzaken);
    }

    [Fact]
    public async Task ResolveAsync_NoDeelzaken_ReturnsEmptyListWithoutQuerying()
    {
        var mediatorMock = new Mock<IMediator>();

        var resolver = new ZaakDeelzakenResolver(BuildServiceProvider(mediatorMock.Object), Mock.Of<IMapper>());
        var entity = new ZaakResponseDto { Url = ZaakUrl, Deelzaken = null };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "deelzaken" });

        var deelzaken = Assert.IsType<List<ZaakResponseDto>>(result);
        Assert.Empty(deelzaken);
        mediatorMock.Verify(m => m.Send(It.IsAny<GetAllZakenQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_DeelzakenStatusRequested_ForwardsRelativePathToReusedExpandEngine()
    {
        var deelzaak1 = new Zaak { Id = new("22222222-2222-2222-2222-222222222222") };
        var pagedResult = new PagedResult<Zaak> { PageResult = [deelzaak1], Count = 1 };
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllZakenQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<PagedResult<Zaak>>(pagedResult, QueryStatus.OK));
        var mappedDeelzaak1 = new ZaakResponseDto { Uuid = deelzaak1.Id.ToString() };
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<ZaakResponseDto>>(pagedResult.PageResult)).Returns([mappedDeelzaak1]);

        var statusResolverMock = new Mock<IExpandResolver<ZaakResponseDto>>();
        statusResolverMock.SetupGet(r => r.Path).Returns("status");
        statusResolverMock.SetupGet(r => r.Parent).Returns((string)null);
        var expandEngine = new ExpandEngine<ZaakResponseDto>([statusResolverMock.Object]);

        var entityWithOneDeelzaak = new ZaakResponseDto { Url = ZaakUrl, Deelzaken = [DeelzaakUrl1] };
        var resolver = new ZaakDeelzakenResolver(BuildServiceProvider(mediatorMock.Object, expandEngine), mapperMock.Object);

        await resolver.ResolveAsync(entityWithOneDeelzaak, new Dictionary<string, object>(), new HashSet<string> { "deelzaken", "deelzaken.status" });

        statusResolverMock.Verify(
            r => r.ResolveAsync(mappedDeelzaak1, It.IsAny<IReadOnlyDictionary<string, object>>(), It.IsAny<IReadOnlySet<string>>()),
            Times.Once
        );
    }

    [Fact]
    public void Path_IsDeelzaken_AndHasNoParent_AndDeclaresExpectedAdditionalPaths()
    {
        var resolver = new ZaakDeelzakenResolver(BuildServiceProvider(Mock.Of<IMediator>()), Mock.Of<IMapper>());

        Assert.Equal("deelzaken", resolver.Path);
        Assert.Null(resolver.Parent);
        Assert.Equal(
            [
                ("deelzaken.zaaktype", "deelzaken"),
                ("deelzaken.zaaktype.catalogus", "deelzaken.zaaktype"),
                ("deelzaken.status", "deelzaken"),
                ("deelzaken.status.statustype", "deelzaken.status"),
                ("deelzaken.resultaat", "deelzaken"),
                ("deelzaken.resultaat.resultaattype", "deelzaken.resultaat"),
                ("deelzaken.rollen", "deelzaken"),
                ("deelzaken.rollen.roltype", "deelzaken.rollen"),
                ("deelzaken.zaakobjecten", "deelzaken"),
                ("deelzaken.zaakobjecten.zaakobjecttype", "deelzaken.zaakobjecten"),
                ("deelzaken.zaakinformatieobjecten", "deelzaken"),
                ("deelzaken.zaakinformatieobjecten.informatieobject", "deelzaken.zaakinformatieobjecten"),
            ],
            resolver.AdditionalPaths
        );
    }
}
