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
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class StatusZaakInformatieObjectenResolverTests
{
    private const string StatusUrl = "https://zrc.test/statussen/33333333-3333-3333-3333-333333333333";
    private const string ZaakUrl = "https://zrc.test/zaken/11111111-1111-1111-1111-111111111111";
    private const string InformatieObjectUrl1 = "https://drc.test/enkelvoudiginformatieobjecten/1";
    private const string InformatieObjectUrl2 = "https://drc.test/enkelvoudiginformatieobjecten/2";

    private static readonly StatusResponseDto Entity = new() { Url = StatusUrl, Zaak = ZaakUrl };

    // StatusZaakInformatieObjectenResolver resolves IMediator from a fresh DI scope per call (see the
    // class doc comment: GetAllZaakInformatieObjectenQuery's handler can create a session-scoped
    // PostgreSQL temp table). A real ServiceCollection exercises that CreateScope() call for real,
    // unlike a bare IMediator mock would.
    private static IServiceProvider BuildServiceProvider(IMediator mediator)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => mediator);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task ResolveAsync_StatusHasTwoRelations_ReturnsBothMapped()
    {
        var zio1 = new ZaakInformatieObject { Id = new("00000000-0000-0000-0000-000000000001"), InformatieObject = InformatieObjectUrl1 };
        var zio2 = new ZaakInformatieObject { Id = new("00000000-0000-0000-0000-000000000002"), InformatieObject = InformatieObjectUrl2 };
        IList<ZaakInformatieObject> zrcResult = [zio1, zio2];
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m =>
                m.Send(
                    It.Is<GetAllZaakInformatieObjectenQuery>(q =>
                        q.GetAllZaakInformatieObjectenFilter.Status == StatusUrl && q.GetAllZaakInformatieObjectenFilter.Zaak == ZaakUrl
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new QueryResult<IList<ZaakInformatieObject>>(zrcResult, QueryStatus.OK));

        var mappedZio1 = new ZaakInformatieObjectResponseDto { Uuid = zio1.Id.ToString() };
        var mappedZio2 = new ZaakInformatieObjectResponseDto { Uuid = zio2.Id.ToString() };
        var mapperMock = new Mock<IMapper>();
        mapperMock
            .Setup(m => m.Map<List<ZaakInformatieObjectResponseDto>>(It.Is<List<ZaakInformatieObject>>(l => l.Count == 2)))
            .Returns([mappedZio1, mappedZio2]);

        var resolver = new StatusZaakInformatieObjectenResolver(
            BuildServiceProvider(mediatorMock.Object),
            mapperMock.Object,
            new ExpandEngine<ZaakInformatieObjectResponseDto>([])
        );

        var result = await resolver.ResolveAsync(Entity, new Dictionary<string, object>(), new HashSet<string> { "zaakinformatieobjecten" });

        var response = Assert.IsType<List<ZaakInformatieObjectResponseDto>>(result);
        Assert.Equal([mappedZio1, mappedZio2], response);
    }

    [Fact]
    public async Task ResolveAsync_NoZaakInformatieObjecten_ReturnsEmptyListWithoutMapping()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m =>
                m.Send(
                    It.Is<GetAllZaakInformatieObjectenQuery>(q =>
                        q.GetAllZaakInformatieObjectenFilter.Status == StatusUrl && q.GetAllZaakInformatieObjectenFilter.Zaak == ZaakUrl
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new QueryResult<IList<ZaakInformatieObject>>(new List<ZaakInformatieObject>(), QueryStatus.OK));
        var mapperMock = new Mock<IMapper>();

        var resolver = new StatusZaakInformatieObjectenResolver(
            BuildServiceProvider(mediatorMock.Object),
            mapperMock.Object,
            new ExpandEngine<ZaakInformatieObjectResponseDto>([])
        );

        var result = await resolver.ResolveAsync(Entity, new Dictionary<string, object>(), new HashSet<string> { "zaakinformatieobjecten" });

        var response = Assert.IsType<List<ZaakInformatieObjectResponseDto>>(result);
        Assert.Empty(response);
        mapperMock.Verify(m => m.Map<List<ZaakInformatieObjectResponseDto>>(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_ZaakinformatieobjectenInformatieobjectRequested_DelegatesToExpandEngineResolveListAsync()
    {
        var zio1 = new ZaakInformatieObject { Id = new("00000000-0000-0000-0000-000000000001"), InformatieObject = InformatieObjectUrl1 };
        IList<ZaakInformatieObject> zrcResult = [zio1];
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllZaakInformatieObjectenQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<IList<ZaakInformatieObject>>(zrcResult, QueryStatus.OK));

        var mappedZio1 = new ZaakInformatieObjectResponseDto { Uuid = zio1.Id.ToString() };
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<ZaakInformatieObjectResponseDto>>(It.IsAny<List<ZaakInformatieObject>>())).Returns([mappedZio1]);

        var informatieobjectResolverMock = new Mock<IExpandResolver<ZaakInformatieObjectResponseDto>>();
        informatieobjectResolverMock.SetupGet(r => r.Path).Returns("informatieobject");
        informatieobjectResolverMock.SetupGet(r => r.Parent).Returns((string)null);
        var expandEngine = new ExpandEngine<ZaakInformatieObjectResponseDto>([informatieobjectResolverMock.Object]);

        var resolver = new StatusZaakInformatieObjectenResolver(BuildServiceProvider(mediatorMock.Object), mapperMock.Object, expandEngine);

        await resolver.ResolveAsync(
            Entity,
            new Dictionary<string, object>(),
            new HashSet<string> { "zaakinformatieobjecten", "zaakinformatieobjecten.informatieobject" }
        );

        informatieobjectResolverMock.Verify(
            r => r.ResolveAsync(mappedZio1, It.IsAny<IReadOnlyDictionary<string, object>>(), It.IsAny<IReadOnlySet<string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ResolveAsync_ZaakinformatieobjectenInformatieobjectInformatieobjecttypeRequested_ForwardsNestedPathToExpandEngine()
    {
        var zio1 = new ZaakInformatieObject { Id = new("00000000-0000-0000-0000-000000000001"), InformatieObject = InformatieObjectUrl1 };
        IList<ZaakInformatieObject> zrcResult = [zio1];
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllZaakInformatieObjectenQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<IList<ZaakInformatieObject>>(zrcResult, QueryStatus.OK));

        var mappedZio1 = new ZaakInformatieObjectResponseDto { Uuid = zio1.Id.ToString() };
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<ZaakInformatieObjectResponseDto>>(It.IsAny<List<ZaakInformatieObject>>())).Returns([mappedZio1]);

        var informatieobjectResolverMock = new Mock<IExpandResolver<ZaakInformatieObjectResponseDto>>();
        informatieobjectResolverMock.SetupGet(r => r.Path).Returns("informatieobject");
        informatieobjectResolverMock.SetupGet(r => r.Parent).Returns((string)null);
        var expandEngine = new ExpandEngine<ZaakInformatieObjectResponseDto>([informatieobjectResolverMock.Object]);

        var resolver = new StatusZaakInformatieObjectenResolver(BuildServiceProvider(mediatorMock.Object), mapperMock.Object, expandEngine);

        await resolver.ResolveAsync(
            Entity,
            new Dictionary<string, object>(),
            new HashSet<string>
            {
                "zaakinformatieobjecten",
                "zaakinformatieobjecten.informatieobject",
                "zaakinformatieobjecten.informatieobject.informatieobjecttype",
            }
        );

        informatieobjectResolverMock.Verify(
            r =>
                r.ResolveAsync(
                    mappedZio1,
                    It.IsAny<IReadOnlyDictionary<string, object>>(),
                    It.Is<IReadOnlySet<string>>(p => p.Contains("informatieobject.informatieobjecttype"))
                ),
            Times.Once
        );
    }

    [Fact]
    public void Path_IsZaakinformatieobjecten_AndHasNoParent_AndDeclaresZaakinformatieobjectenInformatieobjectAsAdditionalPath()
    {
        var resolver = new StatusZaakInformatieObjectenResolver(
            BuildServiceProvider(Mock.Of<IMediator>()),
            Mock.Of<IMapper>(),
            new ExpandEngine<ZaakInformatieObjectResponseDto>([])
        );

        Assert.Equal("zaakinformatieobjecten", resolver.Path);
        Assert.Null(resolver.Parent);
        Assert.Equal(
            [
                ("zaakinformatieobjecten.informatieobject", "zaakinformatieobjecten"),
                ("zaakinformatieobjecten.informatieobject.informatieobjecttype", "zaakinformatieobjecten.informatieobject"),
            ],
            resolver.AdditionalPaths
        );
    }
}
