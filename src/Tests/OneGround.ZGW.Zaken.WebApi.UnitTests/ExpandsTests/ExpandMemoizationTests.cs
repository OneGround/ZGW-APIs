using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Documenten.ServiceAgent.v1._7;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;
using Xunit;
using QueryResult = OneGround.ZGW.Common.Handlers.QueryResult<OneGround.ZGW.Zaken.DataModel.Zaak>;
using QueryStatus = OneGround.ZGW.Common.Handlers.QueryStatus;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

/// <summary>
/// Per request, the same zaak / status / document / OIO list is fetched once, however many rows expand to it (a list filtered on one zaak or
/// one informatieobject has N rows that all expand to the same thing).
/// </summary>
public class ExpandMemoizationTests
{
    private static readonly Guid ZaakId = new("11111111-1111-1111-1111-111111111111");
    private const string ZaakUrl = "https://zrc.test/zaken/11111111-1111-1111-1111-111111111111";

    private static Mock<IMediator> MediatorReturning(QueryResult result)
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        return mediator;
    }

    // ---- ZaakLookup

    [Fact]
    public async Task ZaakLookup_SameZaakTwice_IsFetchedOnce()
    {
        var zaak = new Zaak { Id = ZaakId };
        var mediator = MediatorReturning(new QueryResult(zaak, QueryStatus.OK));
        var lookup = ExpandCaches.ZaakLookup(mediator.Object);

        var first = await lookup.GetAsync(ZaakId, "zaak");
        var second = await lookup.GetAsync(ZaakId, "zaak");

        Assert.Same(zaak, first);
        Assert.Same(zaak, second);
        mediator.Verify(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ZaakLookup_OtherZaak_IsAnotherFetch()
    {
        var mediator = MediatorReturning(new QueryResult(new Zaak { Id = ZaakId }, QueryStatus.OK));
        var lookup = ExpandCaches.ZaakLookup(mediator.Object);

        await lookup.GetAsync(ZaakId, "zaak");
        await lookup.GetAsync(new Guid("22222222-2222-2222-2222-222222222222"), "zaak");

        mediator.Verify(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ZaakLookup_QueryThrows_IsNotKept_SoTheNextCallTriesAgain()
    {
        var calls = 0;
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
                ++calls == 1
                    ? throw new InvalidOperationException("boom")
                    : Task.FromResult(new QueryResult(new Zaak { Id = ZaakId }, QueryStatus.OK))
            );
        var lookup = ExpandCaches.ZaakLookup(mediator.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => lookup.GetAsync(ZaakId, "zaak"));
        var zaak = await lookup.GetAsync(ZaakId, "zaak");

        Assert.NotNull(zaak);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task ZaakLookup_ConcurrentCallsForOneZaak_ShareOneQuery()
    {
        var release = new TaskCompletionSource();
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await release.Task;
                return new QueryResult(new Zaak { Id = ZaakId }, QueryStatus.OK);
            });
        var lookup = ExpandCaches.ZaakLookup(mediator.Object);

        var calls = Enumerable.Range(0, 8).Select(_ => lookup.GetAsync(ZaakId, "zaak")).ToArray();
        release.SetResult();
        await Task.WhenAll(calls);

        mediator.Verify(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ZaakLookup_SameZaakInOtherCrs_IsAnotherFetch()
    {
        var mediator = MediatorReturning(new QueryResult(new Zaak { Id = ZaakId }, QueryStatus.OK));
        var lookup = ExpandCaches.ZaakLookup(mediator.Object);

        await lookup.GetAsync(ZaakId, "zaak", srid: 28992);
        await lookup.GetAsync(ZaakId, "zaak", srid: 4326);

        mediator.Verify(m => m.Send(It.Is<GetZaakQuery>(q => q.SRID == 4326), It.IsAny<CancellationToken>()), Times.Once);
        mediator.Verify(m => m.Send(It.Is<GetZaakQuery>(q => q.SRID == 28992), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(QueryStatus.NotFound)]
    [InlineData(QueryStatus.Forbidden)]
    public async Task ZaakLookup_ZaakNotAvailableToTheCaller_IsNullAndNotAskedAgain(QueryStatus status)
    {
        var mediator = MediatorReturning(new QueryResult(null, status));
        var lookup = ExpandCaches.ZaakLookup(mediator.Object);

        Assert.Null(await lookup.GetAsync(ZaakId, "zaak"));
        Assert.Null(await lookup.GetAsync(ZaakId, "zaak"));

        mediator.Verify(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ZaakLookup_RealFailure_ThrowsWithTheResourceOfEveryCaller_WithoutAskingAgain()
    {
        var mediator = MediatorReturning(new QueryResult(null, QueryStatus.Failed));
        var lookup = ExpandCaches.ZaakLookup(mediator.Object);

        var exception = await Assert.ThrowsAsync<ExpandInternalQueryHandlerException>(() => lookup.GetAsync(ZaakId, "hoofdzaak"));
        var second = await Assert.ThrowsAsync<ExpandInternalQueryHandlerException>(() => lookup.GetAsync(ZaakId, "zaak"));

        Assert.Equal("hoofdzaak", exception.Resource);
        Assert.Equal("zaak", second.Resource);
        Assert.Equal(QueryStatus.Failed, second.StatusCode);
        mediator.Verify(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ZaakLookup_ResultIsKeptPerLookup_SoAnotherRequestFetchesAgain()
    {
        var mediator = MediatorReturning(new QueryResult(new Zaak { Id = ZaakId }, QueryStatus.OK));

        await ExpandCaches.ZaakLookup(mediator.Object).GetAsync(ZaakId, "zaak");
        await ExpandCaches.ZaakLookup(mediator.Object).GetAsync(ZaakId, "zaak");

        mediator.Verify(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // ---- a resolver on top of it: N rows of one zaak

    [Fact]
    public async Task ZaakResolver_ManyRowsOfOneZaak_FetchesTheZaakOnce_AndGivesEachRowItsOwnDto()
    {
        var zaak = new Zaak { Id = ZaakId };
        var mediator = MediatorReturning(new QueryResult(zaak, QueryStatus.OK));
        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<ZaakResponseDto>(zaak)).Returns(() => new ZaakResponseDto { Url = ZaakUrl });
        var resolver = new ZaakContactmomentZaakResolver(ExpandCaches.ZaakLookup(mediator.Object), mapper.Object);

        var results = new List<object>();
        for (var i = 0; i < 5; i++)
        {
            results.Add(
                await resolver.ResolveAsync(
                    new ZaakContactmomentResponseDto { Zaak = ZaakUrl },
                    new Dictionary<string, object>(),
                    new HashSet<string> { "zaak" }
                )
            );
        }

        mediator.Verify(m => m.Send(It.IsAny<GetZaakQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        // Note: the engine writes _expand into the dto, so every row must get a dto of its own
        Assert.Equal(5, new HashSet<object>(results, ReferenceEqualityComparer.Instance).Count);
    }

    // ---- DRC: the document of a ZIO

    [Fact]
    public async Task ZaakInformatieObjectInformatieObject_ManyZiosOfOneDocument_CallsDrcOnce()
    {
        const string documentUrl = "https://drc.test/enkelvoudiginformatieobjecten/22222222-2222-2222-2222-222222222222";
        var agent = new Mock<IUserAuthDocumentenServiceAgent>();
        agent
            .Setup(a => a.GetEnkelvoudigInformatieObjectByUrlAsync(documentUrl))
            .ReturnsAsync(new ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>(new EnkelvoudigInformatieObjectResponseDto()));
        var mapper = new Mock<IMapper>();
        mapper
            .Setup(m => m.Map<EnkelvoudigInformatieObjectGetResponseDto>(It.IsAny<EnkelvoudigInformatieObjectResponseDto>()))
            .Returns(() => new EnkelvoudigInformatieObjectGetResponseDto());
        var resolver = new ZaakInformatieObjectInformatieObjectResolver(
            agent.Object,
            mapper.Object,
            new ExpandEngine<EnkelvoudigInformatieObjectGetResponseDto>([]),
            ExpandCaches.Document()
        );

        for (var i = 0; i < 4; i++)
        {
            await resolver.ResolveAsync(
                new ZaakInformatieObjectResponseDto { InformatieObject = documentUrl },
                new Dictionary<string, object>(),
                new HashSet<string> { "informatieobject" }
            );
        }

        agent.Verify(a => a.GetEnkelvoudigInformatieObjectByUrlAsync(documentUrl), Times.Once);
    }

    // ---- DRC: the OIO list that tells which documents the caller may see

    [Fact]
    public async Task StatusZaakInformatieObjecten_ManyStatussenOfOneZaak_AsksDrcForTheObjectInformatieObjectenOnce()
    {
        var zio = new ZaakInformatieObject { Id = new("00000000-0000-0000-0000-000000000001"), InformatieObject = "https://drc.test/x/1" };
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<GetAllZaakInformatieObjectenQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OneGround.ZGW.Common.Handlers.QueryResult<IList<ZaakInformatieObject>>([zio], QueryStatus.OK));
        var agent = new Mock<IUserAuthDocumentenServiceAgent>();
        agent
            .Setup(a =>
                a.GetObjectInformatieObjectenAsync(
                    It.IsAny<OneGround.ZGW.Documenten.Contracts.v1._7.Queries.GetAllObjectInformatieObjectenQueryParameters>()
                )
            )
            .ReturnsAsync(new ServiceAgentResponse<IEnumerable<ObjectInformatieObjectResponseDto>>([]));
        var services = new ServiceCollection();
        services.AddScoped(_ => mediator.Object);
        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<List<ZaakInformatieObjectResponseDto>>(It.IsAny<List<ZaakInformatieObject>>())).Returns([]);
        var resolver = new StatusZaakInformatieObjectenResolver(
            services.BuildServiceProvider(),
            mapper.Object,
            agent.Object,
            new ExpandEngine<ZaakInformatieObjectResponseDto>([]),
            ExpandCaches.ObjectInformatieObjecten()
        );

        for (var i = 0; i < 3; i++)
        {
            await resolver.ResolveAsync(
                new StatusResponseDto { Url = $"https://zrc.test/statussen/0000000{i}-0000-0000-0000-000000000000", Zaak = ZaakUrl },
                new Dictionary<string, object>(),
                new HashSet<string> { "zaakinformatieobjecten" }
            );
        }

        agent.Verify(
            a =>
                a.GetObjectInformatieObjectenAsync(
                    It.IsAny<OneGround.ZGW.Documenten.Contracts.v1._7.Queries.GetAllObjectInformatieObjectenQueryParameters>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ZaakZaakInformatieObjecten_AndStatusZaakInformatieObjecten_ShareOneObjectInformatieObjectenEntryPerZaak()
    {
        var zio = new ZaakInformatieObject { Id = new("00000000-0000-0000-0000-000000000001"), InformatieObject = "https://drc.test/x/1" };
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<GetAllZaakInformatieObjectenQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OneGround.ZGW.Common.Handlers.QueryResult<IList<ZaakInformatieObject>>([zio], QueryStatus.OK));
        var agent = new Mock<IUserAuthDocumentenServiceAgent>();
        agent
            .Setup(a =>
                a.GetObjectInformatieObjectenAsync(
                    It.IsAny<OneGround.ZGW.Documenten.Contracts.v1._7.Queries.GetAllObjectInformatieObjectenQueryParameters>()
                )
            )
            .ReturnsAsync(new ServiceAgentResponse<IEnumerable<ObjectInformatieObjectResponseDto>>([]));
        var services = new ServiceCollection();
        services.AddScoped(_ => mediator.Object);
        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<List<ZaakInformatieObjectResponseDto>>(It.IsAny<List<ZaakInformatieObject>>())).Returns([]);
        var sharedCache = ExpandCaches.ObjectInformatieObjecten();
        var zaakResolver = new ZaakZaakInformatieObjectenResolver(
            services.BuildServiceProvider(),
            mapper.Object,
            agent.Object,
            new ExpandEngine<ZaakInformatieObjectResponseDto>([]),
            sharedCache
        );
        var statusResolver = new StatusZaakInformatieObjectenResolver(
            services.BuildServiceProvider(),
            mapper.Object,
            agent.Object,
            new ExpandEngine<ZaakInformatieObjectResponseDto>([]),
            sharedCache
        );

        await zaakResolver.ResolveAsync(
            // Note: the zaak must have zaakinformatieobjecten, or this resolver stops before it asks DRC anything
            new ZaakResponseDto { Url = ZaakUrl, ZaakInformatieObjecten = ["https://zrc.test/zaakinformatieobjecten/1"] },
            new Dictionary<string, object>(),
            new HashSet<string> { "zaakinformatieobjecten" }
        );
        await statusResolver.ResolveAsync(
            new StatusResponseDto { Url = "https://zrc.test/statussen/00000000-0000-0000-0000-000000000001", Zaak = ZaakUrl },
            new Dictionary<string, object>(),
            new HashSet<string> { "zaakinformatieobjecten" }
        );

        agent.Verify(
            a =>
                a.GetObjectInformatieObjectenAsync(
                    It.IsAny<OneGround.ZGW.Documenten.Contracts.v1._7.Queries.GetAllObjectInformatieObjectenQueryParameters>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ZaakInformatieObjectInformatieObject_OtherDocument_IsAnotherDrcCall()
    {
        var agent = new Mock<IUserAuthDocumentenServiceAgent>();
        agent
            .Setup(a => a.GetEnkelvoudigInformatieObjectByUrlAsync(It.IsAny<string>()))
            .ReturnsAsync(new ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>(new EnkelvoudigInformatieObjectResponseDto()));
        var mapper = new Mock<IMapper>();
        mapper
            .Setup(m => m.Map<EnkelvoudigInformatieObjectGetResponseDto>(It.IsAny<EnkelvoudigInformatieObjectResponseDto>()))
            .Returns(() => new EnkelvoudigInformatieObjectGetResponseDto());
        var resolver = new ZaakInformatieObjectInformatieObjectResolver(
            agent.Object,
            mapper.Object,
            new ExpandEngine<EnkelvoudigInformatieObjectGetResponseDto>([]),
            ExpandCaches.Document()
        );

        foreach (var url in new[] { "https://drc.test/eio/1", "https://drc.test/eio/2", "https://drc.test/eio/1" })
        {
            await resolver.ResolveAsync(
                new ZaakInformatieObjectResponseDto { InformatieObject = url },
                new Dictionary<string, object>(),
                new HashSet<string> { "informatieobject" }
            );
        }

        agent.Verify(a => a.GetEnkelvoudigInformatieObjectByUrlAsync("https://drc.test/eio/1"), Times.Once);
        agent.Verify(a => a.GetEnkelvoudigInformatieObjectByUrlAsync("https://drc.test/eio/2"), Times.Once);
    }

    [Fact]
    public async Task ZaakInformatieObjectInformatieObject_DocumentNotAvailable_IsAskedOnceAndStaysNothing()
    {
        const string documentUrl = "https://drc.test/eio/403";
        var agent = new Mock<IUserAuthDocumentenServiceAgent>();
        agent
            .Setup(a => a.GetEnkelvoudigInformatieObjectByUrlAsync(documentUrl))
            .ReturnsAsync(
                new ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>(new OneGround.ZGW.Common.Contracts.v1.ErrorResponse { Status = 403 })
            );
        var resolver = new ZaakInformatieObjectInformatieObjectResolver(
            agent.Object,
            Mock.Of<IMapper>(),
            new ExpandEngine<EnkelvoudigInformatieObjectGetResponseDto>([]),
            ExpandCaches.Document()
        );

        for (var i = 0; i < 3; i++)
        {
            Assert.Null(
                await resolver.ResolveAsync(
                    new ZaakInformatieObjectResponseDto { InformatieObject = documentUrl },
                    new Dictionary<string, object>(),
                    new HashSet<string> { "informatieobject" }
                )
            );
        }

        agent.Verify(a => a.GetEnkelvoudigInformatieObjectByUrlAsync(documentUrl), Times.Once);
    }

    // ---- the status of a ZIO

    [Fact]
    public async Task ZaakInformatieObjectStatus_ManyZiosAtOneStatus_QueriesTheStatusOnce()
    {
        const string statusUrl = "https://zrc.test/statussen/33333333-3333-3333-3333-333333333333";
        var status = new ZaakStatus { Id = new("33333333-3333-3333-3333-333333333333") };
        var mediator = new Mock<IMediator>();
        mediator
            .Setup(m => m.Send(It.IsAny<GetZaakStatusQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OneGround.ZGW.Common.Handlers.QueryResult<ZaakStatus>(status, QueryStatus.OK));
        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<StatusResponseDto>(status)).Returns(() => new StatusResponseDto());
        var resolver = new ZaakInformatieObjectStatusResolver(
            mediator.Object,
            mapper.Object,
            new Lazy<ExpandEngine<StatusResponseDto>>(() => new ExpandEngine<StatusResponseDto>([])),
            ExpandCaches.Status()
        );

        for (var i = 0; i < 4; i++)
        {
            await resolver.ResolveAsync(
                new ZaakInformatieObjectResponseDto { Status = statusUrl },
                new Dictionary<string, object>(),
                new HashSet<string> { "status" }
            );
        }

        mediator.Verify(m => m.Send(It.IsAny<GetZaakStatusQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
