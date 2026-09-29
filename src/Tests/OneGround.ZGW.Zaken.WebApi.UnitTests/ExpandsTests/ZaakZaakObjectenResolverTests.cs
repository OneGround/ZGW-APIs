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
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakObject;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakZaakObjectenResolverTests
{
    private const string ZaakUrl = "https://zrc.test/zaken/11111111-1111-1111-1111-111111111111";

    private static readonly ZaakResponseDto EntityWithTwoZaakObjecten = new()
    {
        Url = ZaakUrl,
        ZaakObjecten = ["https://zrc.test/zaakobjecten/1", "https://zrc.test/zaakobjecten/2"],
    };

    // ZaakZaakObjectenResolver resolves IMediator from a fresh DI scope per call (see the class doc
    // comment: it must not reuse the request-scoped DbContext/connection, since
    // GetAllZaakObjectenQuery's handler can create a session-scoped PostgreSQL temp table). A real
    // ServiceCollection exercises that CreateScope() call for real, unlike a bare IMediator mock would.
    private static IServiceProvider BuildServiceProvider(IMediator mediator)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => mediator);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task ResolveAsync_ZaakHasZaakObjecten_SendsGetAllZaakObjectenQueryFilteredByZaakAndSizedToCountAndReturnsMappedList()
    {
        var zaakObject1 = new ZaakObject { Id = new("00000000-0000-0000-0000-000000000001") };
        var zaakObject2 = new ZaakObject { Id = new("00000000-0000-0000-0000-000000000002") };
        var pagedResult = new PagedResult<ZaakObject> { PageResult = [zaakObject1, zaakObject2], Count = 2 };
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m =>
                m.Send(
                    It.Is<GetAllZaakObjectenQuery>(q =>
                        q.GetAllZaakObjectenFilter.Zaak == ZaakUrl && q.Pagination.Page == 1 && q.Pagination.Size == 2
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new QueryResult<PagedResult<ZaakObject>>(pagedResult, QueryStatus.OK));
        var mappedZaakObject1 = new ZaakObjectResponseDto { Uuid = zaakObject1.Id };
        var mappedZaakObject2 = new ZaakObjectResponseDto { Uuid = zaakObject2.Id };
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<ZaakObjectResponseDto>>(pagedResult.PageResult)).Returns([mappedZaakObject1, mappedZaakObject2]);
        var zaakObjectExpandEngine = new ExpandEngine<ZaakObjectResponseDto>([]);

        var resolver = new ZaakZaakObjectenResolver(BuildServiceProvider(mediatorMock.Object), mapperMock.Object, zaakObjectExpandEngine);

        var result = await resolver.ResolveAsync(EntityWithTwoZaakObjecten, new Dictionary<string, object>(), new HashSet<string> { "zaakobjecten" });

        var zaakobjecten = Assert.IsType<List<ZaakObjectResponseDto>>(result);
        Assert.Equal([mappedZaakObject1, mappedZaakObject2], zaakobjecten);
    }

    [Fact]
    public async Task ResolveAsync_NoZaakObjecten_ReturnsEmptyListWithoutQuerying()
    {
        var mediatorMock = new Mock<IMediator>();

        var resolver = new ZaakZaakObjectenResolver(
            BuildServiceProvider(mediatorMock.Object),
            Mock.Of<IMapper>(),
            new ExpandEngine<ZaakObjectResponseDto>([])
        );
        var entity = new ZaakResponseDto { Url = ZaakUrl, ZaakObjecten = null };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "zaakobjecten" });

        var zaakobjecten = Assert.IsType<List<ZaakObjectResponseDto>>(result);
        Assert.Empty(zaakobjecten);
        mediatorMock.Verify(m => m.Send(It.IsAny<GetAllZaakObjectenQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_ZaakObjectenZaakobjecttypeRequested_DelegatesToZaakObjectExpandEngineResolveListAsync()
    {
        var zaakObject1 = new ZaakObject { Id = new("00000000-0000-0000-0000-000000000001") };
        var pagedResult = new PagedResult<ZaakObject> { PageResult = [zaakObject1], Count = 1 };
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllZaakObjectenQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<PagedResult<ZaakObject>>(pagedResult, QueryStatus.OK));
        var mappedZaakObject1 = new ZaakObjectResponseDto { Uuid = zaakObject1.Id };
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<ZaakObjectResponseDto>>(pagedResult.PageResult)).Returns([mappedZaakObject1]);

        var zaakobjecttypeResolverMock = new Mock<IExpandResolver<ZaakObjectResponseDto>>();
        zaakobjecttypeResolverMock.SetupGet(r => r.Path).Returns("zaakobjecttype");
        zaakobjecttypeResolverMock.SetupGet(r => r.Parent).Returns((string)null);
        var zaakObjectExpandEngine = new ExpandEngine<ZaakObjectResponseDto>([zaakobjecttypeResolverMock.Object]);

        var entityWithOneZaakObject = new ZaakResponseDto { Url = ZaakUrl, ZaakObjecten = ["https://zrc.test/zaakobjecten/1"] };
        var resolver = new ZaakZaakObjectenResolver(BuildServiceProvider(mediatorMock.Object), mapperMock.Object, zaakObjectExpandEngine);

        await resolver.ResolveAsync(
            entityWithOneZaakObject,
            new Dictionary<string, object>(),
            new HashSet<string> { "zaakobjecten", "zaakobjecten.zaakobjecttype" }
        );

        zaakobjecttypeResolverMock.Verify(
            r => r.ResolveAsync(mappedZaakObject1, It.IsAny<IReadOnlyDictionary<string, object>>(), It.IsAny<IReadOnlySet<string>>()),
            Times.Once
        );
    }

    [Fact]
    public void Path_IsZaakobjecten_AndHasNoParent_AndDeclaresZaakobjectenZaakobjecttypeAsAdditionalPath()
    {
        var resolver = new ZaakZaakObjectenResolver(
            BuildServiceProvider(Mock.Of<IMediator>()),
            Mock.Of<IMapper>(),
            new ExpandEngine<ZaakObjectResponseDto>([])
        );

        Assert.Equal("zaakobjecten", resolver.Path);
        Assert.Null(resolver.Parent);
        Assert.Equal([("zaakobjecten.zaakobjecttype", "zaakobjecten")], resolver.AdditionalPaths);
    }
}
