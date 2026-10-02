using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakRelevanteAndereZakenResolverTests
{
    private const string ZaakUrl = "https://zrc.test/zaken/11111111-1111-1111-1111-111111111111";
    private const string RelevanteZaakUrl1 = "https://zrc.test/zaken/22222222-2222-2222-2222-222222222222";
    private const string RelevanteZaakUrl2 = "https://zrc.test/zaken/33333333-3333-3333-3333-333333333333";

    private static readonly ZaakResponseDto EntityWithTwoRelevanteAndereZaken = new()
    {
        Url = ZaakUrl,
        RelevanteAndereZaken =
        [
            new RelevanteAndereZaakDto { Url = RelevanteZaakUrl1, AardRelatie = "vervolg" },
            new RelevanteAndereZaakDto { Url = RelevanteZaakUrl2, AardRelatie = "bijdrage" },
        ],
    };

    // Same CreateScope()/lazy-engine-reuse reasoning as ZaakDeelzakenResolverTests.
    private static IServiceProvider BuildServiceProvider(IMediator mediator, ExpandEngine<ZaakResponseDto> expandEngine = null)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => mediator);
        services.AddSingleton(expandEngine ?? new ExpandEngine<ZaakResponseDto>([]));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task ResolveAsync_ZaakHasRelevanteAndereZaken_SendsGetAllZakenQueryFilteredByUrlsIgnoringAardRelatieAndReturnsMappedList()
    {
        var relevanteZaak1 = new Zaak { Id = new("22222222-2222-2222-2222-222222222222") };
        var relevanteZaak2 = new Zaak { Id = new("33333333-3333-3333-3333-333333333333") };
        var pagedResult = new PagedResult<Zaak> { PageResult = [relevanteZaak1, relevanteZaak2], Count = 2 };
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m =>
                m.Send(
                    It.Is<GetAllZakenQuery>(q =>
                        q.GetAllZakenFilter.Uuid__in.Count == 2
                        && q.GetAllZakenFilter.Uuid__in.Contains(relevanteZaak1.Id)
                        && q.GetAllZakenFilter.Uuid__in.Contains(relevanteZaak2.Id)
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
        var mappedRelevanteZaak1 = new ZaakResponseDto { Uuid = relevanteZaak1.Id.ToString() };
        var mappedRelevanteZaak2 = new ZaakResponseDto { Uuid = relevanteZaak2.Id.ToString() };
        var mapperMock = new Mock<IMapper>();
        mapperMock
            .Setup(m => m.Map<List<ZaakResponseDto>>(It.Is<List<Zaak>>(l => l.SequenceEqual(new[] { relevanteZaak1, relevanteZaak2 }))))
            .Returns([mappedRelevanteZaak1, mappedRelevanteZaak2]);

        var resolver = new ZaakRelevanteAndereZakenResolver(BuildServiceProvider(mediatorMock.Object), mapperMock.Object);

        var result = await resolver.ResolveAsync(
            EntityWithTwoRelevanteAndereZaken,
            new Dictionary<string, object>(),
            new HashSet<string> { "relevanteanderezaken" }
        );

        var relevanteAndereZaken = Assert.IsType<List<ZaakResponseDto>>(result);
        Assert.Equal([mappedRelevanteZaak1, mappedRelevanteZaak2], relevanteAndereZaken);
    }

    [Fact]
    public async Task ResolveAsync_NoRelevanteAndereZaken_ReturnsEmptyListWithoutQuerying()
    {
        var mediatorMock = new Mock<IMediator>();

        var resolver = new ZaakRelevanteAndereZakenResolver(BuildServiceProvider(mediatorMock.Object), Mock.Of<IMapper>());
        var entity = new ZaakResponseDto { Url = ZaakUrl, RelevanteAndereZaken = [] };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "relevanteanderezaken" });

        var relevanteAndereZaken = Assert.IsType<List<ZaakResponseDto>>(result);
        Assert.Empty(relevanteAndereZaken);
        mediatorMock.Verify(m => m.Send(It.IsAny<GetAllZakenQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_RelevanteAndereZakenStatusRequested_ForwardsRelativePathToReusedExpandEngine()
    {
        var relevanteZaak1 = new Zaak { Id = new("22222222-2222-2222-2222-222222222222") };
        var pagedResult = new PagedResult<Zaak> { PageResult = [relevanteZaak1], Count = 1 };
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllZakenQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<PagedResult<Zaak>>(pagedResult, QueryStatus.OK));
        var mappedRelevanteZaak1 = new ZaakResponseDto { Uuid = relevanteZaak1.Id.ToString() };
        var mapperMock = new Mock<IMapper>();
        mapperMock
            .Setup(m => m.Map<List<ZaakResponseDto>>(It.Is<List<Zaak>>(l => l.SequenceEqual(new[] { relevanteZaak1 }))))
            .Returns([mappedRelevanteZaak1]);

        var statusResolverMock = new Mock<IExpandResolver<ZaakResponseDto>>();
        statusResolverMock.SetupGet(r => r.Path).Returns("status");
        statusResolverMock.SetupGet(r => r.Parent).Returns((string)null);
        var expandEngine = new ExpandEngine<ZaakResponseDto>([statusResolverMock.Object]);

        var entityWithOneRelevanteZaak = new ZaakResponseDto
        {
            Url = ZaakUrl,
            RelevanteAndereZaken = [new RelevanteAndereZaakDto { Url = RelevanteZaakUrl1, AardRelatie = "vervolg" }],
        };
        var resolver = new ZaakRelevanteAndereZakenResolver(BuildServiceProvider(mediatorMock.Object, expandEngine), mapperMock.Object);

        await resolver.ResolveAsync(
            entityWithOneRelevanteZaak,
            new Dictionary<string, object>(),
            new HashSet<string> { "relevanteanderezaken", "relevanteanderezaken.status" }
        );

        statusResolverMock.Verify(
            r => r.ResolveAsync(mappedRelevanteZaak1, It.IsAny<IReadOnlyDictionary<string, object>>(), It.IsAny<IReadOnlySet<string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ResolveAsync_SameZaakReferencedTwiceWithDifferentAardRelatie_ReturnsBothExpandedEntriesPreservingOrder()
    {
        // RelevanteAndereZaken (unlike deelzaken) is a freestanding relation list, not a foreign-key
        // navigation collection, so it can legitimately reference the same ZAAK twice with a different
        // aardRelatie each time. Uuid__in naturally deduplicates in SQL, so the resolver must re-expand
        // the single fetched row against both original (duplicated) url entries -- see the class doc
        // comment and the code-review finding this guards against.
        var relevanteZaak1 = new Zaak { Id = new("22222222-2222-2222-2222-222222222222") };
        var pagedResult = new PagedResult<Zaak> { PageResult = [relevanteZaak1], Count = 1 };
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.Is<GetAllZakenQuery>(q => q.Pagination.Size == 2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<PagedResult<Zaak>>(pagedResult, QueryStatus.OK));
        var mappedRelevanteZaakA = new ZaakResponseDto { Uuid = relevanteZaak1.Id.ToString() };
        var mappedRelevanteZaakB = new ZaakResponseDto { Uuid = relevanteZaak1.Id.ToString() };
        var mapperMock = new Mock<IMapper>();
        mapperMock
            .Setup(m => m.Map<List<ZaakResponseDto>>(It.Is<List<Zaak>>(l => l.SequenceEqual(new[] { relevanteZaak1, relevanteZaak1 }))))
            .Returns([mappedRelevanteZaakA, mappedRelevanteZaakB]);

        var entity = new ZaakResponseDto
        {
            Url = ZaakUrl,
            RelevanteAndereZaken =
            [
                new RelevanteAndereZaakDto { Url = RelevanteZaakUrl1, AardRelatie = "vervolg" },
                new RelevanteAndereZaakDto { Url = RelevanteZaakUrl1, AardRelatie = "bijdrage" },
            ],
        };
        var resolver = new ZaakRelevanteAndereZakenResolver(BuildServiceProvider(mediatorMock.Object), mapperMock.Object);

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "relevanteanderezaken" });

        var relevanteAndereZaken = Assert.IsType<List<ZaakResponseDto>>(result);
        Assert.Equal([mappedRelevanteZaakA, mappedRelevanteZaakB], relevanteAndereZaken);
    }

    [Fact]
    public void Path_IsRelevanteanderezaken_AndHasNoParent_AndDeclaresNarrowerAdditionalPathsThanHoofdzaakDeelzaken()
    {
        var resolver = new ZaakRelevanteAndereZakenResolver(BuildServiceProvider(Mock.Of<IMediator>()), Mock.Of<IMapper>());

        Assert.Equal("relevanteanderezaken", resolver.Path);
        Assert.Null(resolver.Parent);
        Assert.Equal(
            [
                ("relevanteanderezaken.zaaktype", "relevanteanderezaken"),
                ("relevanteanderezaken.status", "relevanteanderezaken"),
                ("relevanteanderezaken.status.statustype", "relevanteanderezaken.status"),
                ("relevanteanderezaken.resultaat", "relevanteanderezaken"),
                ("relevanteanderezaken.resultaat.resultaattype", "relevanteanderezaken.resultaat"),
            ],
            resolver.AdditionalPaths
        );
    }
}
