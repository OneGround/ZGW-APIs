using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Contracts;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.ExpandsTests;

/// <summary>
/// The behaviour that every resolver deriving from <c>ChildType_ResolverBase</c> (a parent that refers to a type by one URL) has in common. A resolver
/// only differs in its path, the URL of the parent it expands and the query it sends, which is all the derived test class has to say.
/// </summary>
public abstract class ChildTypeResolverTestsBase<TParent, TEntity, TResponseDto>
    where TParent : class, IExpandable
    where TEntity : class, new()
    where TResponseDto : class, new()
{
    private const string Url = "https://ztc.test/types/11111111-1111-1111-1111-111111111111";
    private static readonly Guid Id = new("11111111-1111-1111-1111-111111111111");

    /// <summary>The expand path of the resolver.</summary>
    protected abstract string Path { get; }

    /// <summary>The resource named when the query of the type fails.</summary>
    protected abstract string Resource { get; }

    /// <summary>A parent that refers to the type by the given URL (null: it has none).</summary>
    protected abstract TParent CreateParent(string url);

    protected abstract IExpandResolver<TParent> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService);

    /// <summary>Lets the mediator answer the query of the resolver with the result for the id in the query.</summary>
    protected abstract void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<TEntity>> results);

    /// <summary>The ids that the resolver has queried, in order.</summary>
    private readonly List<Guid> _queried = [];

    private IExpandResolver<TParent> CreateResolver(QueryResult<TEntity> result, IMapper mapper = null)
    {
        var mediatorMock = new Mock<IMediator>();
        SetupQuery(
            mediatorMock,
            id =>
            {
                _queried.Add(id);
                return result;
            }
        );

        var services = new ServiceCollection();
        services.AddScoped(_ => mediatorMock.Object);
        services.AddScoped(_ => mapper ?? Mock.Of<IMapper>());
        services.AddScoped(typeof(IGenericCache<>), typeof(GenericCache<>));

        var uriServiceMock = new Mock<IEntityUriService>();
        uriServiceMock.Setup(u => u.GetId(Url)).Returns(Id);

        return CreateResolver(services.BuildServiceProvider(), uriServiceMock.Object);
    }

    private Task<object> Resolve(IExpandResolver<TParent> resolver, string url) =>
        resolver.ResolveAsync(CreateParent(url), new Dictionary<string, object>(), new HashSet<string> { Path });

    private static Mock<IMapper> MapperReturning(TEntity entity, TResponseDto dto)
    {
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<TResponseDto>(entity)).Returns(dto);
        return mapperMock;
    }

    [Fact]
    public void Path_IsTheExpectedExpandPath() => Assert.Equal(Path, CreateResolver(new QueryResult<TEntity>(null, QueryStatus.NotFound)).Path);

    [Fact]
    public async Task ResolveAsync_TypeAvailable_ReturnsTheMappedType()
    {
        var entity = new TEntity();
        var mapped = new TResponseDto();
        var resolver = CreateResolver(new QueryResult<TEntity>(entity, QueryStatus.OK), MapperReturning(entity, mapped).Object);

        Assert.Same(mapped, await Resolve(resolver, Url));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ResolveAsync_ParentWithoutUrl_ResolvesToNullWithoutQuerying(string url)
    {
        var resolver = CreateResolver(new QueryResult<TEntity>(new TEntity(), QueryStatus.OK));

        Assert.Null(await Resolve(resolver, url));
        Assert.Empty(_queried);
    }

    [Theory]
    [InlineData(QueryStatus.NotFound)]
    [InlineData(QueryStatus.Forbidden)]
    public async Task ResolveAsync_TypeNotAvailableToTheCaller_ResolvesToNullInsteadOfFailingTheRequest(QueryStatus status)
    {
        var resolver = CreateResolver(new QueryResult<TEntity>(null, status));

        Assert.Null(await Resolve(resolver, Url));
    }

    [Fact]
    public async Task ResolveAsync_QueryFails_ThrowsExpandInternalQueryHandlerException()
    {
        var resolver = CreateResolver(new QueryResult<TEntity>(null, QueryStatus.Failed));

        var exception = await Assert.ThrowsAsync<ExpandInternalQueryHandlerException>(() => Resolve(resolver, Url));

        Assert.Equal(Resource, exception.Resource);
        Assert.Equal(QueryStatus.Failed, exception.StatusCode);
    }

    [Fact]
    public async Task ResolveAsync_TwoParentsReferToTheSameType_QueriesItOnceAndSharesTheResult()
    {
        var entity = new TEntity();
        var mapped = new TResponseDto();
        var resolver = CreateResolver(new QueryResult<TEntity>(entity, QueryStatus.OK), MapperReturning(entity, mapped).Object);

        var first = await Resolve(resolver, Url);
        var second = await Resolve(resolver, Url);

        Assert.Equal([Id], _queried);
        Assert.Same(mapped, first);
        Assert.Same(mapped, second);
    }

    [Fact]
    public async Task Engine_TypeRequested_PutsTheTypeInExpandUnderThePath()
    {
        var entity = new TEntity();
        var mapped = new TResponseDto();
        var engine = new ExpandEngine<TParent>([
            CreateResolver(new QueryResult<TEntity>(entity, QueryStatus.OK), MapperReturning(entity, mapped).Object),
        ]);
        var parent = CreateParent(Url);

        await engine.ResolveAsync(parent, [Path]);

        Assert.Same(mapped, parent.Expand[Path]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(QueryStatus.NotFound)]
    public async Task Engine_NoTypeToExpand_PutsAnEmptyObjectInExpand(QueryStatus? status)
    {
        // Note: the resolver itself resolves to null; it is the engine that turns that into the empty object in _expand (like the API specification says)
        var resolver = CreateResolver(new QueryResult<TEntity>(null, status ?? QueryStatus.NotFound));
        var engine = new ExpandEngine<TParent>([resolver]);
        var parent = CreateParent(status is null ? null : Url);

        await engine.ResolveAsync(parent, [Path]);

        var expanded = parent.Expand[Path];
        Assert.NotNull(expanded);
        Assert.Equal(typeof(object), expanded.GetType());
    }
}
