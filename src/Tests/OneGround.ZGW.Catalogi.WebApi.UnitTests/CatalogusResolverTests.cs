using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Expands.v1._3;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests;

public class CatalogusResolverTests
{
    private const string CatalogusUrl = "https://ztc.test/catalogussen/11111111-1111-1111-1111-111111111111";
    private static readonly Guid CatalogusId = new("11111111-1111-1111-1111-111111111111");

    private static Catalogus_Resolver CreateResolver(QueryResult<Catalogus> queryResult, IMapper mapper = null)
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetCatalogusQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(queryResult);

        var services = new ServiceCollection();
        services.AddScoped(_ => mediatorMock.Object);
        services.AddScoped(_ => mapper ?? Mock.Of<IMapper>());

        var uriServiceMock = new Mock<IEntityUriService>();
        uriServiceMock.Setup(u => u.GetId(CatalogusUrl)).Returns(CatalogusId);

        return new Catalogus_Resolver(services.BuildServiceProvider(), uriServiceMock.Object, new GenericCache<CatalogusResponseDto>());
    }

    private static Task<object> Resolve(Catalogus_Resolver resolver) =>
        resolver.ResolveAsync(
            new ZaakTypeResponseDto { Catalogus = CatalogusUrl },
            new Dictionary<string, object>(),
            new HashSet<string> { "catalogus" }
        );

    [Fact]
    public async Task ResolveAsync_CatalogusAvailable_ReturnsTheMappedCatalogus()
    {
        var catalogus = new Catalogus();
        var mapped = new CatalogusResponseDto();
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<CatalogusResponseDto>(catalogus)).Returns(mapped);
        var resolver = CreateResolver(new QueryResult<Catalogus>(catalogus, QueryStatus.OK), mapperMock.Object);

        Assert.Same(mapped, await Resolve(resolver));
    }

    [Theory]
    [InlineData(QueryStatus.NotFound)]
    [InlineData(QueryStatus.Forbidden)]
    public async Task ResolveAsync_CatalogusNotAvailableToTheCaller_ResolvesToNullInsteadOfFailingTheRequest(QueryStatus status)
    {
        var resolver = CreateResolver(new QueryResult<Catalogus>(null, status));

        Assert.Null(await Resolve(resolver));
    }

    [Theory]
    [InlineData(QueryStatus.NotFound)]
    [InlineData(QueryStatus.Forbidden)]
    public async Task Engine_CatalogusNotAvailableToTheCaller_PutsAnEmptyObjectInExpand(QueryStatus status)
    {
        // Note: the resolver itself returns null; it is the engine that turns that into the empty object in _expand
        var engine = new ExpandEngine<ZaakTypeResponseDto>([CreateResolver(new QueryResult<Catalogus>(null, status))]);
        var zaakType = new ZaakTypeResponseDto { Catalogus = CatalogusUrl };

        await engine.ResolveAsync(zaakType, ["catalogus"]);

        var catalogus = zaakType.Expand["catalogus"];
        Assert.NotNull(catalogus);
        Assert.Equal(typeof(object), catalogus.GetType());
    }

    [Fact]
    public async Task ResolveAsync_QueryFails_ThrowsExpandInternalQueryHandlerException()
    {
        var resolver = CreateResolver(new QueryResult<Catalogus>(null, QueryStatus.Failed));

        var exception = await Assert.ThrowsAsync<ExpandInternalQueryHandlerException>(() => Resolve(resolver));

        Assert.Equal("catalogus", exception.Resource);
        Assert.Equal(QueryStatus.Failed, exception.StatusCode);
    }
}
