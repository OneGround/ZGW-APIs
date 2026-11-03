using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

/// <summary>
/// The core of the expand resolvers that expand the types a parent (a Catalogus, a ZaakType, ...) refers to by URL. It loads the types, a type at a
/// time. What differs is the shape of the reference: see <see cref="ChildTypen_ResolverBase{TParent,TEntity,TResponseDto}"/> for a list of URLs
/// and <see cref="ChildType_ResolverBase{TParent,TEntity,TResponseDto}"/> for one URL.
/// ZTC owns these types itself, so they are queried locally through MediatR (like <see cref="Catalogus_Resolver"/>) and not through a ServiceAgent.
/// A type that is not available to the caller (NotFound/Forbidden) is left out instead of failing the request, see <see cref="ExpandQueryStatus"/>.
/// </summary>
/// <typeparam name="TParent">The (v1.4) response that is expanded.</typeparam>
/// <typeparam name="TEntity">The entity of the type to expand.</typeparam>
/// <typeparam name="TResponseDto">The response of that entity.</typeparam>
public abstract class Types_ResolverBase<TParent, TEntity, TResponseDto> : IExpandResolver<TParent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEntityUriService _uriService;

    protected Types_ResolverBase(IServiceProvider serviceProvider, IEntityUriService uriService)
    {
        _serviceProvider = serviceProvider;
        _uriService = uriService;
    }

    public abstract string Path { get; }
    public string Parent => null;

    /// <summary>The name of the resource, used when the query of one of the types fails.</summary>
    protected abstract string Resource { get; }

    protected abstract Task<QueryResult<TEntity>> GetAsync(IMediator mediator, Guid id);

    public abstract Task<object> ResolveAsync(TParent entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths);

    /// <summary>Loads the types with the given URLs, those that are available to the caller and each only once.</summary>
    protected async Task<List<TResponseDto>> LoadTypesAsync(IEnumerable<string> urls)
    {
        // Note: The cache belongs to the request (the scope of this resolver, not the scope that is made below for the queries) and is shared by all resolvers
        // of the same response type: a type that several parents in a list refer to is only queried once.
        // That DTO then appears in several parents. This is safe as long as the embedded types are not expanded themselves (the engine would
        // otherwise write an _expand into the shared object): for nested paths the DTO must therefore be copied first.
        var cache = _serviceProvider.GetRequiredService<IGenericCache<TResponseDto>>();

        // Note: A scope of its own (so a DbContext of its own) per call, which is only made when a type is not in the cache yet. The types are
        // queried one after the other, so that one DbContext is never used concurrently.
        IServiceScope scope = null;
        try
        {
            var types = new List<TResponseDto>();

            // Note: By id, not by URL: two URLs to the same type (other case, a trailing slash, a query string) are one type.
            foreach (var id in (urls ?? []).Select(url => _uriService.GetId(url)).Distinct())
            {
                // Note: A type that is not available (null) is remembered too, a failed query is not (it is tried again).
                var type = await cache.GetOrCacheAndGetAsync(id.ToString(), () => GetTypeAsync(scope ??= _serviceProvider.CreateScope(), id));
                if (type is not null)
                {
                    types.Add(type);
                }
            }

            return types;
        }
        finally
        {
            scope?.Dispose();
        }
    }

    private async Task<TResponseDto> GetTypeAsync(IServiceScope scope, Guid id)
    {
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result = await GetAsync(mediator, id);
        if (!ExpandQueryStatus.IsAvailable(result.Status, Resource))
        {
            return default;
        }

        return mapper.Map<TResponseDto>(result.Result);
    }
}
