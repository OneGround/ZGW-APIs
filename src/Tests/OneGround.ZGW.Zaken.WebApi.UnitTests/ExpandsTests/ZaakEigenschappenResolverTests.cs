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

public class ZaakEigenschappenResolverTests
{
    private static readonly System.Guid ZaakId = new("11111111-1111-1111-1111-111111111111");
    private const string ZaakUrl = "https://zrc.test/zaken/11111111-1111-1111-1111-111111111111";

    private static readonly ZaakResponseDto EntityWithTwoEigenschappen = new()
    {
        Url = ZaakUrl,
        Eigenschappen =
        [
            "https://zrc.test/zaken/11111111-1111-1111-1111-111111111111/zaakeigenschappen/1",
            "https://zrc.test/zaken/11111111-1111-1111-1111-111111111111/zaakeigenschappen/2",
        ],
    };

    // Unlike ZaakRollenResolver/ZaakZaakObjectenResolver/ZaakZaakContactmomentenResolver, this resolver
    // does NOT open a fresh DI scope per ZAAK: GetAllZaakEigenschappenQueryHandler authorizes with a
    // plain IsAuthorized(zaak) check, not the TempZaakAuthorization temp-table pattern those other
    // handlers use (see the class doc comment), so it can safely send directly on the injected
    // IMediator -- these tests exercise that mock directly rather than via a built ServiceProvider.
    [Theory]
    [InlineData(QueryStatus.NotFound)]
    [InlineData(QueryStatus.Forbidden)]
    public async Task ResolveAsync_ZaakNotAvailableToTheCaller_ResolvesToAnEmptyListInsteadOfFailingOnTheMissingResult(QueryStatus status)
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllZaakEigenschappenQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<IEnumerable<ZaakEigenschap>>(null, status));
        var resolver = new ZaakEigenschappenResolver(mediatorMock.Object, Mock.Of<IMapper>(), new ExpandEngine<ZaakEigenschapResponseDto>([]));

        var result = await resolver.ResolveAsync(
            EntityWithTwoEigenschappen,
            new Dictionary<string, object>(),
            new HashSet<string> { "eigenschappen" }
        );

        Assert.Empty(Assert.IsType<List<ZaakEigenschapResponseDto>>(result));
    }

    [Fact]
    public async Task ResolveAsync_QueryFails_ThrowsExpandInternalQueryHandlerException()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllZaakEigenschappenQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<IEnumerable<ZaakEigenschap>>(null, QueryStatus.Failed));
        var resolver = new ZaakEigenschappenResolver(mediatorMock.Object, Mock.Of<IMapper>(), new ExpandEngine<ZaakEigenschapResponseDto>([]));

        var exception = await Assert.ThrowsAsync<ExpandInternalQueryHandlerException>(() =>
            resolver.ResolveAsync(EntityWithTwoEigenschappen, new Dictionary<string, object>(), new HashSet<string> { "eigenschappen" })
        );

        Assert.Equal("eigenschappen", exception.Resource);
        Assert.Equal(QueryStatus.Failed, exception.StatusCode);
    }

    [Fact]
    public async Task ResolveAsync_ZaakHasEigenschappen_SendsGetAllZaakEigenschappenQueryForParsedZaakIdAndReturnsMappedList()
    {
        var zaakEigenschap1 = new ZaakEigenschap { Id = new("00000000-0000-0000-0000-000000000001") };
        var zaakEigenschap2 = new ZaakEigenschap { Id = new("00000000-0000-0000-0000-000000000002") };
        IEnumerable<ZaakEigenschap> zaakeigenschappen = [zaakEigenschap1, zaakEigenschap2];
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.Is<GetAllZaakEigenschappenQuery>(q => q.Zaak == ZaakId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<IEnumerable<ZaakEigenschap>>(zaakeigenschappen, QueryStatus.OK));
        var mappedZaakEigenschap1 = new ZaakEigenschapResponseDto { Uuid = zaakEigenschap1.Id.ToString() };
        var mappedZaakEigenschap2 = new ZaakEigenschapResponseDto { Uuid = zaakEigenschap2.Id.ToString() };
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<ZaakEigenschapResponseDto>>(zaakeigenschappen)).Returns([mappedZaakEigenschap1, mappedZaakEigenschap2]);
        var zaakEigenschapExpandEngine = new ExpandEngine<ZaakEigenschapResponseDto>([]);

        var resolver = new ZaakEigenschappenResolver(mediatorMock.Object, mapperMock.Object, zaakEigenschapExpandEngine);

        var result = await resolver.ResolveAsync(
            EntityWithTwoEigenschappen,
            new Dictionary<string, object>(),
            new HashSet<string> { "eigenschappen" }
        );

        var eigenschappen = Assert.IsType<List<ZaakEigenschapResponseDto>>(result);
        Assert.Equal([mappedZaakEigenschap1, mappedZaakEigenschap2], eigenschappen);
    }

    [Fact]
    public async Task ResolveAsync_NoEigenschappen_ReturnsEmptyListWithoutQuerying()
    {
        var mediatorMock = new Mock<IMediator>();

        var resolver = new ZaakEigenschappenResolver(mediatorMock.Object, Mock.Of<IMapper>(), new ExpandEngine<ZaakEigenschapResponseDto>([]));
        var entity = new ZaakResponseDto { Url = ZaakUrl, Eigenschappen = null };

        var result = await resolver.ResolveAsync(entity, new Dictionary<string, object>(), new HashSet<string> { "eigenschappen" });

        var eigenschappen = Assert.IsType<List<ZaakEigenschapResponseDto>>(result);
        Assert.Empty(eigenschappen);
        mediatorMock.Verify(m => m.Send(It.IsAny<GetAllZaakEigenschappenQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_EigenschappenEigenschapRequested_DelegatesToZaakEigenschapExpandEngineResolveListAsync()
    {
        var zaakEigenschap1 = new ZaakEigenschap { Id = new("00000000-0000-0000-0000-000000000001") };
        IEnumerable<ZaakEigenschap> zaakeigenschappen = [zaakEigenschap1];
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllZaakEigenschappenQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResult<IEnumerable<ZaakEigenschap>>(zaakeigenschappen, QueryStatus.OK));
        var mappedZaakEigenschap1 = new ZaakEigenschapResponseDto { Uuid = zaakEigenschap1.Id.ToString() };
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<ZaakEigenschapResponseDto>>(zaakeigenschappen)).Returns([mappedZaakEigenschap1]);

        var eigenschapResolverMock = new Mock<IExpandResolver<ZaakEigenschapResponseDto>>();
        eigenschapResolverMock.SetupGet(r => r.Path).Returns("eigenschap");
        eigenschapResolverMock.SetupGet(r => r.Parent).Returns((string)null);
        var zaakEigenschapExpandEngine = new ExpandEngine<ZaakEigenschapResponseDto>([eigenschapResolverMock.Object]);

        var entityWithOneEigenschap = new ZaakResponseDto
        {
            Url = ZaakUrl,
            Eigenschappen = ["https://zrc.test/zaken/11111111-1111-1111-1111-111111111111/zaakeigenschappen/1"],
        };
        var resolver = new ZaakEigenschappenResolver(mediatorMock.Object, mapperMock.Object, zaakEigenschapExpandEngine);

        await resolver.ResolveAsync(
            entityWithOneEigenschap,
            new Dictionary<string, object>(),
            new HashSet<string> { "eigenschappen", "eigenschappen.eigenschap" }
        );

        eigenschapResolverMock.Verify(
            r => r.ResolveAsync(mappedZaakEigenschap1, It.IsAny<IReadOnlyDictionary<string, object>>(), It.IsAny<IReadOnlySet<string>>()),
            Times.Once
        );
    }

    [Fact]
    public void Path_IsEigenschappen_AndHasNoParent_AndDeclaresEigenschappenEigenschapAsAdditionalPath()
    {
        var resolver = new ZaakEigenschappenResolver(Mock.Of<IMediator>(), Mock.Of<IMapper>(), new ExpandEngine<ZaakEigenschapResponseDto>([]));

        Assert.Equal("eigenschappen", resolver.Path);
        Assert.Null(resolver.Parent);
        Assert.Equal([("eigenschappen.eigenschap", "eigenschappen")], resolver.AdditionalPaths);
    }
}
