using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Expands.v1._4;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests;

public class BesluitTypenResolverTests
{
    private const string BesluitTypeUrl1 = "https://ztc.test/besluittypen/11111111-1111-1111-1111-111111111111";
    private const string BesluitTypeUrl2 = "https://ztc.test/besluittypen/22222222-2222-2222-2222-222222222222";
    private static readonly Guid BesluitTypeId1 = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BesluitTypeId2 = new("22222222-2222-2222-2222-222222222222");

    private static BesluitTypen_Resolver CreateResolver(Dictionary<Guid, QueryResult<BesluitType>> results, IMapper mapper = null)
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetBesluitTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetBesluitTypeQuery q, CancellationToken _) => results[q.Id]);

        var services = new ServiceCollection();
        services.AddScoped(_ => mediatorMock.Object);
        services.AddScoped(_ => mapper ?? Mock.Of<IMapper>());

        var uriServiceMock = new Mock<IEntityUriService>();
        uriServiceMock.Setup(u => u.GetId(BesluitTypeUrl1)).Returns(BesluitTypeId1);
        uriServiceMock.Setup(u => u.GetId(BesluitTypeUrl2)).Returns(BesluitTypeId2);

        return new BesluitTypen_Resolver(services.BuildServiceProvider(), uriServiceMock.Object);
    }

    private static Task<object> Resolve(BesluitTypen_Resolver resolver, params string[] besluitTypeUrls) =>
        resolver.ResolveAsync(
            new CatalogusResponseDto { BesluitTypen = besluitTypeUrls },
            new Dictionary<string, object>(),
            new HashSet<string> { "besluittypen" }
        );

    [Fact]
    public async Task ResolveAsync_BesluitTypenAvailable_ReturnsTheMappedBesluitTypenInOrder()
    {
        var besluitType1 = new BesluitType();
        var besluitType2 = new BesluitType();
        var mapped1 = new BesluitTypeResponseDto();
        var mapped2 = new BesluitTypeResponseDto();
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<BesluitTypeResponseDto>(besluitType1)).Returns(mapped1);
        mapperMock.Setup(m => m.Map<BesluitTypeResponseDto>(besluitType2)).Returns(mapped2);
        var resolver = CreateResolver(
            new()
            {
                [BesluitTypeId1] = new QueryResult<BesluitType>(besluitType1, QueryStatus.OK),
                [BesluitTypeId2] = new QueryResult<BesluitType>(besluitType2, QueryStatus.OK),
            },
            mapperMock.Object
        );

        var result = (List<BesluitTypeResponseDto>)await Resolve(resolver, BesluitTypeUrl1, BesluitTypeUrl2);

        Assert.Equal([mapped1, mapped2], result);
    }

    [Fact]
    public async Task ResolveAsync_CatalogusWithoutBesluitTypen_ReturnsAnEmptyList()
    {
        var resolver = CreateResolver([]);

        var result = (List<BesluitTypeResponseDto>)await Resolve(resolver);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(QueryStatus.NotFound)]
    [InlineData(QueryStatus.Forbidden)]
    public async Task ResolveAsync_BesluitTypeNotAvailableToTheCaller_LeavesItOutInsteadOfFailingTheRequest(QueryStatus status)
    {
        var besluitType2 = new BesluitType();
        var mapped2 = new BesluitTypeResponseDto();
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<BesluitTypeResponseDto>(besluitType2)).Returns(mapped2);
        var resolver = CreateResolver(
            new()
            {
                [BesluitTypeId1] = new QueryResult<BesluitType>(null, status),
                [BesluitTypeId2] = new QueryResult<BesluitType>(besluitType2, QueryStatus.OK),
            },
            mapperMock.Object
        );

        var result = (List<BesluitTypeResponseDto>)await Resolve(resolver, BesluitTypeUrl1, BesluitTypeUrl2);

        Assert.Same(mapped2, Assert.Single(result));
    }

    [Fact]
    public async Task ResolveAsync_QueryFails_ThrowsExpandInternalQueryHandlerException()
    {
        var resolver = CreateResolver(new() { [BesluitTypeId1] = new QueryResult<BesluitType>(null, QueryStatus.Failed) });

        var exception = await Assert.ThrowsAsync<ExpandInternalQueryHandlerException>(() => Resolve(resolver, BesluitTypeUrl1));

        Assert.Equal("besluittype", exception.Resource);
        Assert.Equal(QueryStatus.Failed, exception.StatusCode);
    }

    [Fact]
    public async Task Engine_BesluitTypenRequested_PutsTheListInExpandUnderBesluittypen()
    {
        var mapped = new BesluitTypeResponseDto();
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<BesluitTypeResponseDto>(It.IsAny<BesluitType>())).Returns(mapped);
        var engine = new ExpandEngine<CatalogusResponseDto>([
            CreateResolver(new() { [BesluitTypeId1] = new QueryResult<BesluitType>(new BesluitType(), QueryStatus.OK) }, mapperMock.Object),
        ]);
        var catalogus = new CatalogusResponseDto { BesluitTypen = [BesluitTypeUrl1] };

        await engine.ResolveAsync(catalogus, ["besluittypen"]);

        Assert.Same(mapped, Assert.Single((List<BesluitTypeResponseDto>)catalogus.Expand["besluittypen"]));
    }
}
