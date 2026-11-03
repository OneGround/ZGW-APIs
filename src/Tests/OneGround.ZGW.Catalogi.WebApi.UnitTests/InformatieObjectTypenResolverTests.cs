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

public class InformatieObjectTypenResolverTests
{
    private const string InformatieObjectTypeUrl1 = "https://ztc.test/informatieobjecttypen/11111111-1111-1111-1111-111111111111";
    private const string InformatieObjectTypeUrl2 = "https://ztc.test/informatieobjecttypen/22222222-2222-2222-2222-222222222222";
    private static readonly Guid InformatieObjectTypeId1 = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid InformatieObjectTypeId2 = new("22222222-2222-2222-2222-222222222222");

    private static InformatieObjectTypen_Resolver CreateResolver(Dictionary<Guid, QueryResult<InformatieObjectType>> results, IMapper mapper = null)
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetInformatieObjectTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetInformatieObjectTypeQuery q, CancellationToken _) => results[q.Id]);

        var services = new ServiceCollection();
        services.AddScoped(_ => mediatorMock.Object);
        services.AddScoped(_ => mapper ?? Mock.Of<IMapper>());

        var uriServiceMock = new Mock<IEntityUriService>();
        uriServiceMock.Setup(u => u.GetId(InformatieObjectTypeUrl1)).Returns(InformatieObjectTypeId1);
        uriServiceMock.Setup(u => u.GetId(InformatieObjectTypeUrl2)).Returns(InformatieObjectTypeId2);

        return new InformatieObjectTypen_Resolver(services.BuildServiceProvider(), uriServiceMock.Object);
    }

    private static Task<object> Resolve(InformatieObjectTypen_Resolver resolver, params string[] informatieObjectTypeUrls) =>
        resolver.ResolveAsync(
            new CatalogusResponseDto { InformatieObjectTypen = informatieObjectTypeUrls },
            new Dictionary<string, object>(),
            new HashSet<string> { "informatieobjecttypen" }
        );

    [Fact]
    public async Task ResolveAsync_InformatieObjectTypenAvailable_ReturnsTheMappedInformatieObjectTypenInOrder()
    {
        var informatieObjectType1 = new InformatieObjectType();
        var informatieObjectType2 = new InformatieObjectType();
        var mapped1 = new InformatieObjectTypeResponseDto();
        var mapped2 = new InformatieObjectTypeResponseDto();
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<InformatieObjectTypeResponseDto>(informatieObjectType1)).Returns(mapped1);
        mapperMock.Setup(m => m.Map<InformatieObjectTypeResponseDto>(informatieObjectType2)).Returns(mapped2);
        var resolver = CreateResolver(
            new()
            {
                [InformatieObjectTypeId1] = new QueryResult<InformatieObjectType>(informatieObjectType1, QueryStatus.OK),
                [InformatieObjectTypeId2] = new QueryResult<InformatieObjectType>(informatieObjectType2, QueryStatus.OK),
            },
            mapperMock.Object
        );

        var result = (List<InformatieObjectTypeResponseDto>)await Resolve(resolver, InformatieObjectTypeUrl1, InformatieObjectTypeUrl2);

        Assert.Equal([mapped1, mapped2], result);
    }

    [Fact]
    public async Task ResolveAsync_CatalogusWithoutInformatieObjectTypen_ReturnsAnEmptyList()
    {
        var resolver = CreateResolver([]);

        var result = (List<InformatieObjectTypeResponseDto>)await Resolve(resolver);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(QueryStatus.NotFound)]
    [InlineData(QueryStatus.Forbidden)]
    public async Task ResolveAsync_InformatieObjectTypeNotAvailableToTheCaller_LeavesItOutInsteadOfFailingTheRequest(QueryStatus status)
    {
        var informatieObjectType2 = new InformatieObjectType();
        var mapped2 = new InformatieObjectTypeResponseDto();
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<InformatieObjectTypeResponseDto>(informatieObjectType2)).Returns(mapped2);
        var resolver = CreateResolver(
            new()
            {
                [InformatieObjectTypeId1] = new QueryResult<InformatieObjectType>(null, status),
                [InformatieObjectTypeId2] = new QueryResult<InformatieObjectType>(informatieObjectType2, QueryStatus.OK),
            },
            mapperMock.Object
        );

        var result = (List<InformatieObjectTypeResponseDto>)await Resolve(resolver, InformatieObjectTypeUrl1, InformatieObjectTypeUrl2);

        Assert.Same(mapped2, Assert.Single(result));
    }

    [Fact]
    public async Task ResolveAsync_QueryFails_ThrowsExpandInternalQueryHandlerException()
    {
        var resolver = CreateResolver(new() { [InformatieObjectTypeId1] = new QueryResult<InformatieObjectType>(null, QueryStatus.Failed) });

        var exception = await Assert.ThrowsAsync<ExpandInternalQueryHandlerException>(() => Resolve(resolver, InformatieObjectTypeUrl1));

        Assert.Equal("informatieobjecttype", exception.Resource);
        Assert.Equal(QueryStatus.Failed, exception.StatusCode);
    }

    [Fact]
    public async Task Engine_InformatieObjectTypenRequested_PutsTheListInExpandUnderInformatieobjecttypen()
    {
        var mapped = new InformatieObjectTypeResponseDto();
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<InformatieObjectTypeResponseDto>(It.IsAny<InformatieObjectType>())).Returns(mapped);
        var engine = new ExpandEngine<CatalogusResponseDto>([
            CreateResolver(
                new() { [InformatieObjectTypeId1] = new QueryResult<InformatieObjectType>(new InformatieObjectType(), QueryStatus.OK) },
                mapperMock.Object
            ),
        ]);
        var catalogus = new CatalogusResponseDto { InformatieObjectTypen = [InformatieObjectTypeUrl1] };

        await engine.ResolveAsync(catalogus, ["informatieobjecttypen"]);

        Assert.Same(mapped, Assert.Single((List<InformatieObjectTypeResponseDto>)catalogus.Expand["informatieobjecttypen"]));
    }
}
