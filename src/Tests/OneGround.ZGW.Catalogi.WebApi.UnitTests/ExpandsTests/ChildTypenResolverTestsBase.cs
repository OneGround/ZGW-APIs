using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Common.Contracts;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.ExpandsTests;

/// <summary>
/// The behaviour that every resolver deriving from <c>ChildTypen_ResolverBase</c> has in common. A resolver only differs in its path, the URLs of
/// the parent it expands and the query it sends, which is all the derived test class has to say.
/// </summary>
public abstract class ChildTypenResolverTestsBase<TParent, TEntity, TResponseDto>
    where TParent : class, IExpandable
    where TEntity : class, new()
    where TResponseDto : class, new()
{
    private const string Url1 = "https://ztc.test/types/11111111-1111-1111-1111-111111111111";
    private const string Url2 = "https://ztc.test/types/22222222-2222-2222-2222-222222222222";
    private static readonly Guid Id1 = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Id2 = new("22222222-2222-2222-2222-222222222222");

    /// <summary>The expand path of the resolver.</summary>
    protected abstract string Path { get; }

    /// <summary>The resource named when the query of one of the types fails.</summary>
    protected abstract string Resource { get; }

    /// <summary>A parent that refers to the given types by URL.</summary>
    protected abstract TParent CreateParent(params string[] urls);

    protected abstract IExpandResolver<TParent> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService);

    /// <summary>Lets the mediator answer the query of the resolver with the result for the id in the query.</summary>
    protected abstract void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<TEntity>> results);

    private IExpandResolver<TParent> CreateResolver(Dictionary<Guid, QueryResult<TEntity>> results, IMapper mapper = null)
    {
        var mediatorMock = new Mock<IMediator>();
        SetupQuery(mediatorMock, id => results[id]);

        var services = new ServiceCollection();
        services.AddScoped(_ => mediatorMock.Object);
        services.AddScoped(_ => mapper ?? Mock.Of<IMapper>());

        var uriServiceMock = new Mock<IEntityUriService>();
        uriServiceMock.Setup(u => u.GetId(Url1)).Returns(Id1);
        uriServiceMock.Setup(u => u.GetId(Url2)).Returns(Id2);

        return CreateResolver(services.BuildServiceProvider(), uriServiceMock.Object);
    }

    private Task<object> Resolve(IExpandResolver<TParent> resolver, params string[] urls) =>
        resolver.ResolveAsync(CreateParent(urls), new Dictionary<string, object>(), new HashSet<string> { Path });

    private static Mock<IMapper> MapperReturning(params (TEntity Entity, TResponseDto Dto)[] mapped)
    {
        var mapperMock = new Mock<IMapper>();
        foreach (var (entity, dto) in mapped)
        {
            mapperMock.Setup(m => m.Map<TResponseDto>(entity)).Returns(dto);
        }
        return mapperMock;
    }

    [Fact]
    public void Path_IsTheExpectedExpandPath() => Assert.Equal(Path, CreateResolver([]).Path);

    [Fact]
    public async Task ResolveAsync_TypesAvailable_ReturnsTheMappedTypesInOrder()
    {
        var entity1 = new TEntity();
        var entity2 = new TEntity();
        var mapped1 = new TResponseDto();
        var mapped2 = new TResponseDto();
        var resolver = CreateResolver(
            new() { [Id1] = new QueryResult<TEntity>(entity1, QueryStatus.OK), [Id2] = new QueryResult<TEntity>(entity2, QueryStatus.OK) },
            MapperReturning((entity1, mapped1), (entity2, mapped2)).Object
        );

        var result = (List<TResponseDto>)await Resolve(resolver, Url1, Url2);

        Assert.Equal([mapped1, mapped2], result);
    }

    [Fact]
    public async Task ResolveAsync_ParentWithoutTypes_ReturnsAnEmptyList()
    {
        var result = (List<TResponseDto>)await Resolve(CreateResolver([]));

        Assert.Empty(result);
    }

    [Fact]
    public async Task ResolveAsync_ParentWithoutTypeList_ReturnsAnEmptyListInsteadOfFailing()
    {
        // Note: the mapping leaves some of these lists null on purpose, e.g. besluittypen and informatieobjecttypen of a zaaktype
        var result = (List<TResponseDto>)await Resolve(CreateResolver([]), null);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(QueryStatus.NotFound)]
    [InlineData(QueryStatus.Forbidden)]
    public async Task ResolveAsync_TypeNotAvailableToTheCaller_LeavesItOutInsteadOfFailingTheRequest(QueryStatus status)
    {
        var entity2 = new TEntity();
        var mapped2 = new TResponseDto();
        var resolver = CreateResolver(
            new() { [Id1] = new QueryResult<TEntity>(null, status), [Id2] = new QueryResult<TEntity>(entity2, QueryStatus.OK) },
            MapperReturning((entity2, mapped2)).Object
        );

        var result = (List<TResponseDto>)await Resolve(resolver, Url1, Url2);

        Assert.Same(mapped2, Assert.Single(result));
    }

    [Fact]
    public async Task ResolveAsync_QueryFails_ThrowsExpandInternalQueryHandlerException()
    {
        var resolver = CreateResolver(new() { [Id1] = new QueryResult<TEntity>(null, QueryStatus.Failed) });

        var exception = await Assert.ThrowsAsync<ExpandInternalQueryHandlerException>(() => Resolve(resolver, Url1));

        Assert.Equal(Resource, exception.Resource);
        Assert.Equal(QueryStatus.Failed, exception.StatusCode);
    }

    [Fact]
    public async Task Engine_TypesRequested_PutsTheListInExpandUnderThePath()
    {
        var entity = new TEntity();
        var mapped = new TResponseDto();
        var engine = new ExpandEngine<TParent>([
            CreateResolver(new() { [Id1] = new QueryResult<TEntity>(entity, QueryStatus.OK) }, MapperReturning((entity, mapped)).Object),
        ]);
        var parent = CreateParent(Url1);

        await engine.ResolveAsync(parent, [Path]);

        Assert.Same(mapped, Assert.Single((List<TResponseDto>)parent.Expand[Path]));
    }
}
