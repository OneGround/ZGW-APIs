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

public class ZaakTypenResolverTests
{
    private const string ZaakTypeUrl1 = "https://ztc.test/zaaktypen/11111111-1111-1111-1111-111111111111";
    private const string ZaakTypeUrl2 = "https://ztc.test/zaaktypen/22222222-2222-2222-2222-222222222222";
    private static readonly Guid ZaakTypeId1 = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ZaakTypeId2 = new("22222222-2222-2222-2222-222222222222");

    private static ZaakTypen_Resolver CreateResolver(Dictionary<Guid, QueryResult<ZaakType>> results, IMapper mapper = null)
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetZaakTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetZaakTypeQuery q, CancellationToken _) => results[q.Id]);

        var services = new ServiceCollection();
        services.AddScoped(_ => mediatorMock.Object);
        services.AddScoped(_ => mapper ?? Mock.Of<IMapper>());

        var uriServiceMock = new Mock<IEntityUriService>();
        uriServiceMock.Setup(u => u.GetId(ZaakTypeUrl1)).Returns(ZaakTypeId1);
        uriServiceMock.Setup(u => u.GetId(ZaakTypeUrl2)).Returns(ZaakTypeId2);

        return new ZaakTypen_Resolver(services.BuildServiceProvider(), uriServiceMock.Object);
    }

    private static Task<object> Resolve(ZaakTypen_Resolver resolver, params string[] zaakTypeUrls) =>
        resolver.ResolveAsync(
            new CatalogusResponseDto { ZaakTypen = zaakTypeUrls },
            new Dictionary<string, object>(),
            new HashSet<string> { "zaaktypen" }
        );

    [Fact]
    public async Task ResolveAsync_ZaakTypenAvailable_ReturnsTheMappedZaakTypenInOrder()
    {
        var zaakType1 = new ZaakType();
        var zaakType2 = new ZaakType();
        var mapped1 = new ZaakTypeResponseDto();
        var mapped2 = new ZaakTypeResponseDto();
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<ZaakTypeResponseDto>(zaakType1)).Returns(mapped1);
        mapperMock.Setup(m => m.Map<ZaakTypeResponseDto>(zaakType2)).Returns(mapped2);
        var resolver = CreateResolver(
            new()
            {
                [ZaakTypeId1] = new QueryResult<ZaakType>(zaakType1, QueryStatus.OK),
                [ZaakTypeId2] = new QueryResult<ZaakType>(zaakType2, QueryStatus.OK),
            },
            mapperMock.Object
        );

        var result = (List<ZaakTypeResponseDto>)await Resolve(resolver, ZaakTypeUrl1, ZaakTypeUrl2);

        Assert.Equal([mapped1, mapped2], result);
    }

    [Fact]
    public async Task ResolveAsync_CatalogusWithoutZaakTypen_ReturnsAnEmptyList()
    {
        var resolver = CreateResolver([]);

        var result = (List<ZaakTypeResponseDto>)await Resolve(resolver);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(QueryStatus.NotFound)]
    [InlineData(QueryStatus.Forbidden)]
    public async Task ResolveAsync_ZaakTypeNotAvailableToTheCaller_LeavesItOutInsteadOfFailingTheRequest(QueryStatus status)
    {
        var zaakType2 = new ZaakType();
        var mapped2 = new ZaakTypeResponseDto();
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<ZaakTypeResponseDto>(zaakType2)).Returns(mapped2);
        var resolver = CreateResolver(
            new() { [ZaakTypeId1] = new QueryResult<ZaakType>(null, status), [ZaakTypeId2] = new QueryResult<ZaakType>(zaakType2, QueryStatus.OK) },
            mapperMock.Object
        );

        var result = (List<ZaakTypeResponseDto>)await Resolve(resolver, ZaakTypeUrl1, ZaakTypeUrl2);

        Assert.Same(mapped2, Assert.Single(result));
    }

    [Fact]
    public async Task ResolveAsync_QueryFails_ThrowsExpandInternalQueryHandlerException()
    {
        var resolver = CreateResolver(new() { [ZaakTypeId1] = new QueryResult<ZaakType>(null, QueryStatus.Failed) });

        var exception = await Assert.ThrowsAsync<ExpandInternalQueryHandlerException>(() => Resolve(resolver, ZaakTypeUrl1));

        Assert.Equal("zaaktype", exception.Resource);
        Assert.Equal(QueryStatus.Failed, exception.StatusCode);
    }

    [Fact]
    public async Task Engine_ZaakTypenRequested_PutsTheListInExpandUnderZaaktypen()
    {
        var mapped = new ZaakTypeResponseDto();
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<ZaakTypeResponseDto>(It.IsAny<ZaakType>())).Returns(mapped);
        var engine = new ExpandEngine<CatalogusResponseDto>([
            CreateResolver(new() { [ZaakTypeId1] = new QueryResult<ZaakType>(new ZaakType(), QueryStatus.OK) }, mapperMock.Object),
        ]);
        var catalogus = new CatalogusResponseDto { ZaakTypen = [ZaakTypeUrl1] };

        await engine.ResolveAsync(catalogus, ["zaaktypen"]);

        Assert.Same(mapped, Assert.Single((List<ZaakTypeResponseDto>)catalogus.Expand["zaaktypen"]));
    }
}
