using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Expands.v1._4;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;
using Xunit;
using V4 = OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.ExpandsTests;

/// <summary>
/// The expand of the informatieobjecttype of a zaaktype-informatieobjecttype is no reference by URL but a soft relation on the omschrijving, see the resolver.
/// </summary>
public class ZaakTypeInformatieObjectType_InformatieObjectTypeResolverTests
{
    private const string CatalogusUrl = "https://ztc.test/catalogussen/11111111-1111-1111-1111-111111111111";
    private static readonly Guid CatalogusId = new("11111111-1111-1111-1111-111111111111");

    private readonly List<GetAllInformatieObjectTypenQuery> _queries = [];

    private ZaakTypeInformatieObjectType_InformatieObjectType_Resolver CreateResolver(
        IReadOnlyList<InformatieObjectType> found,
        Func<InformatieObjectType, V4.InformatieObjectTypeResponseDto> map = null,
        ILogger<ZaakTypeInformatieObjectType_InformatieObjectType_Resolver> logger = null
    )
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetAllInformatieObjectTypenQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (GetAllInformatieObjectTypenQuery q, CancellationToken _) =>
                {
                    _queries.Add(q);
                    // Note: Like the real query: the requested page of all the matches, and the number of all the matches
                    var page = found.Skip((q.Pagination.Page - 1) * q.Pagination.Size).Take(q.Pagination.Size).ToList();
                    return new QueryResult<PagedResult<InformatieObjectType>>(
                        new PagedResult<InformatieObjectType> { PageResult = page, Count = found.Count },
                        QueryStatus.OK
                    );
                }
            );

        var mapperMock = new Mock<IMapper>();
        mapperMock
            .Setup(m => m.Map<V4.InformatieObjectTypeResponseDto>(It.IsAny<InformatieObjectType>()))
            .Returns((InformatieObjectType i) => (map ?? (_ => new V4.InformatieObjectTypeResponseDto()))(i));

        var services = new ServiceCollection();
        services.AddScoped(_ => mediatorMock.Object);
        services.AddScoped(_ => mapperMock.Object);
        services.AddScoped(typeof(IGenericCache<>), typeof(GenericCache<>));
        if (logger is not null)
        {
            services.AddSingleton(logger);
        }

        var uriServiceMock = new Mock<IEntityUriService>();
        uriServiceMock.Setup(u => u.GetId(CatalogusUrl)).Returns(CatalogusId);

        return new ZaakTypeInformatieObjectType_InformatieObjectType_Resolver(services.BuildServiceProvider(), uriServiceMock.Object);
    }

    private static Task<object> Resolve(
        ZaakTypeInformatieObjectType_InformatieObjectType_Resolver resolver,
        string omschrijving = "Aanvraag",
        string catalogus = CatalogusUrl
    ) =>
        resolver.ResolveAsync(
            new V4.ZaakTypeInformatieObjectTypeResponseDto { InformatieObjectType = omschrijving, Catalogus = catalogus },
            new Dictionary<string, object>(),
            new HashSet<string> { "informatieobjecttype" }
        );

    private static InformatieObjectType Version(int year) => new() { Omschrijving = "Aanvraag", BeginGeldigheid = new DateOnly(year, 1, 1) };

    [Fact]
    public void Path_IsInformatieObjectType() => Assert.Equal("informatieobjecttype", CreateResolver([]).Path);

    [Fact]
    public async Task ResolveAsync_ASingleValidVersion_ReturnsItMapped()
    {
        var version = Version(2020);
        var mapped = new V4.InformatieObjectTypeResponseDto();
        var resolver = CreateResolver([version], i => i == version ? mapped : null);

        Assert.Same(mapped, await Resolve(resolver));
    }

    [Fact]
    public async Task ResolveAsync_SeveralVersionsAreValidToday_ReturnsTheNewestOne()
    {
        var older = Version(2018);
        var newest = Version(2022);
        var middle = Version(2020);
        var mappedNewest = new V4.InformatieObjectTypeResponseDto();
        var resolver = CreateResolver([older, newest, middle], i => i == newest ? mappedNewest : new V4.InformatieObjectTypeResponseDto());

        Assert.Same(mappedNewest, await Resolve(resolver));
    }

    [Fact]
    public async Task ResolveAsync_MoreVersionsThanFitOnOnePage_ReturnsTheNewestOfTheFirstPageAndLogsAWarning()
    {
        // Note: the query has no order on the geldigheid, so with more matches than the page holds the newest one can be missed; that is logged
        var versions = Enumerable.Range(0, 150).Select(i => Version(1900 + i % 100)).ToList();
        var newestOfFirstPage = versions.Take(100).MaxBy(v => v.BeginGeldigheid);
        var mapped = new V4.InformatieObjectTypeResponseDto();
        var logger = new Mock<ILogger<ZaakTypeInformatieObjectType_InformatieObjectType_Resolver>>();
        var resolver = CreateResolver(versions, i => i == newestOfFirstPage ? mapped : new V4.InformatieObjectTypeResponseDto(), logger.Object);

        Assert.Same(mapped, await Resolve(resolver));
        Assert.Single(_queries);
        logger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), null, It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ResolveAsync_NotMoreVersionsThanFitOnOnePage_LogsNoWarning()
    {
        var logger = new Mock<ILogger<ZaakTypeInformatieObjectType_InformatieObjectType_Resolver>>();
        var resolver = CreateResolver([Version(2020), Version(2022)], logger: logger.Object);

        await Resolve(resolver);

        logger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), null, It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ResolveAsync_NoVersionIsValid_ResolvesToNull()
    {
        Assert.Null(await Resolve(CreateResolver([])));
    }

    [Fact]
    public async Task ResolveAsync_AsksForTheDefinitiefVersionsWithThatOmschrijvingInTheCatalogusThatAreValidToday()
    {
        var resolver = CreateResolver([Version(2020)]);

        await Resolve(resolver, "Aanvraag");

        var filter = Assert.Single(_queries).GetAllInformatieObjectTypenFilter;
        Assert.Equal(CatalogusUrl, filter.Catalogus);
        Assert.Equal("Aanvraag", filter.Omschrijving);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today), filter.DatumGeldigheid);
        Assert.Equal(ConceptStatus.definitief, filter.Status);
    }

    [Theory]
    [InlineData(null, CatalogusUrl)]
    [InlineData("", CatalogusUrl)]
    [InlineData(" ", CatalogusUrl)]
    [InlineData("Aanvraag", null)]
    [InlineData("Aanvraag", "")]
    public async Task ResolveAsync_WithoutAnOmschrijvingOrACatalogus_ResolvesToNullWithoutQuerying(string omschrijving, string catalogus)
    {
        var resolver = CreateResolver([Version(2020)]);

        Assert.Null(await Resolve(resolver, omschrijving, catalogus));
        Assert.Empty(_queries);
    }

    [Fact]
    public async Task ResolveAsync_TwoParentsWithTheSameOmschrijving_QueriesOnceAndSharesTheResult()
    {
        var resolver = CreateResolver([Version(2020)]);

        var first = await Resolve(resolver);
        var second = await Resolve(resolver);

        Assert.Single(_queries);
        Assert.Same(first, second);
    }

    [Fact]
    public async Task ResolveAsync_ADifferentOmschrijving_IsQueriedAgain()
    {
        var resolver = CreateResolver([Version(2020)]);

        await Resolve(resolver, "Aanvraag");
        await Resolve(resolver, "Besluit");

        Assert.Equal(2, _queries.Count);
    }

    [Fact]
    public async Task Engine_NoVersionIsValid_PutsAnEmptyObjectInExpand()
    {
        var engine = new ExpandEngine<V4.ZaakTypeInformatieObjectTypeResponseDto>([CreateResolver([])]);
        var parent = new V4.ZaakTypeInformatieObjectTypeResponseDto { InformatieObjectType = "Aanvraag", Catalogus = CatalogusUrl };

        await engine.ResolveAsync(parent, ["informatieobjecttype"]);

        var expanded = parent.Expand["informatieobjecttype"];
        Assert.NotNull(expanded);
        Assert.Equal(typeof(object), expanded.GetType());
    }
}
