using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

/// <summary>
/// Resolves the "catalogus" expand path for entities that reference a Catalogus by URL
/// (InformatieObjectType, ZaakType, BesluitType, ...). Unlike DRC/ZRC -- where the Catalogus lives
/// in another service and is fetched through a ServiceAgent -- ZTC owns the Catalogus itself, so
/// this queries it locally through MediatR, like DRC's own-service InformatieObjectResolver does.
/// The result is cached per request (keyed by the id of the catalogus, so two spellings of its URL are one catalogus),
/// since a list of InformatieObjectType/ZaakType/BesluitType/Eigenschap/ResultaatType/RolType/StatusType/ZaakObjectType/ZaakTypeInformatieObjectType typically references only a handful of
/// distinct catalogi. A CATALOGUS that is not available to the caller (NotFound/Forbidden) resolves to null,
/// which the ExpandEngine stores in <c>_expand</c> as an empty object (<c>{}</c>), instead of failing the request, see
/// <see cref="ExpandQueryStatus"/>.
///
/// One non-generic class implements IExpandResolver&lt;TEntity&gt; for every supported entity type,
/// with one ResolveAsync overload per type: the entity types don't share a common interface exposing
/// "Catalogus", so there's no single TEntity to be generic over -- only the fetch-by-id logic
/// (ResolveCatalogusAsync) is actually shared.
/// </summary>
public class Catalogus_Resolver
    : IExpandResolver<InformatieObjectTypeResponseDto>,
        IExpandResolver<ZaakTypeResponseDto>,
        IExpandResolver<BesluitTypeResponseDto>,
        IExpandResolver<EigenschapResponseDto>,
        IExpandResolver<ResultaatTypeResponseDto>,
        IExpandResolver<RolTypeResponseDto>,
        IExpandResolver<StatusTypeResponseDto>,
        IExpandResolver<ZaakObjectTypeResponseDto>,
        IExpandResolver<ZaakTypeInformatieObjectTypeResponseDto>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEntityUriService _uriService;
    private readonly IGenericCache<CatalogusResponseDto> _catalogusCache;

    public Catalogus_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService, IGenericCache<CatalogusResponseDto> catalogusCache)
    {
        _serviceProvider = serviceProvider;
        _uriService = uriService;
        _catalogusCache = catalogusCache;
    }

    public string Path => "catalogus";
    public string Parent => null;

    public Task<object> ResolveAsync(
        InformatieObjectTypeResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    ) => ResolveCatalogusAsync(entity.Catalogus);

    public Task<object> ResolveAsync(ZaakTypeResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths) =>
        ResolveCatalogusAsync(entity.Catalogus);

    public Task<object> ResolveAsync(
        BesluitTypeResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    ) => ResolveCatalogusAsync(entity.Catalogus);

    public Task<object> ResolveAsync(
        EigenschapResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    ) => ResolveCatalogusAsync(entity.Catalogus);

    public Task<object> ResolveAsync(
        ResultaatTypeResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    ) => ResolveCatalogusAsync(entity.Catalogus);

    public Task<object> ResolveAsync(RolTypeResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths) =>
        ResolveCatalogusAsync(entity.Catalogus);

    public Task<object> ResolveAsync(
        StatusTypeResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    ) => ResolveCatalogusAsync(entity.Catalogus);

    public Task<object> ResolveAsync(
        ZaakObjectTypeResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    ) => ResolveCatalogusAsync(entity.Catalogus);

    public Task<object> ResolveAsync(
        ZaakTypeInformatieObjectTypeResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    ) => ResolveCatalogusAsync(entity.Catalogus);

    // Note: A parent without a catalogus URL (e.g. a ResultaatType whose mapping has no ZaakType) has nothing to expand: null, which the ExpandEngine
    // stores in _expand as an empty object.
    private Task<object> ResolveCatalogusAsync(string catalogusUrl) =>
        string.IsNullOrWhiteSpace(catalogusUrl) ? Task.FromResult<object>(null) : LoadCatalogusAsync(catalogusUrl);

    private async Task<object> LoadCatalogusAsync(string catalogusUrl)
    {
        // Note: By id, not by URL: two URLs to the same catalogus (other case, a trailing slash, a query string) are one catalogus, like in
        // Types_ResolverBase.
        var id = _uriService.GetId(catalogusUrl);

        return await _catalogusCache.GetOrCacheAndGetAsync(
            id.ToString(),
            async () =>
            {
                // Note: A scope of its own (so a DbContext of its own) per query, which is only made when the catalogus is not in the cache yet.
                // The cache belongs to the request, the scope does not.
                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

                var result = await mediator.Send(new GetCatalogusQuery { Id = id });
                if (!ExpandQueryStatus.IsAvailable(result.Status, "catalogus"))
                {
                    return null;
                }

                return mapper.Map<CatalogusResponseDto>(result.Result);
            }
        );
    }
}
