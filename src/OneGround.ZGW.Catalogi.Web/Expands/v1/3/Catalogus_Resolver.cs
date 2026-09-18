using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._3;

/// <summary>
/// Resolves the "catalogus" expand path for entities that reference a Catalogus by URL
/// (InformatieObjectType, ZaakType, BesluitType, ...). Unlike DRC/ZRC -- where the Catalogus lives
/// in another service and is fetched through a ServiceAgent -- ZTC owns the Catalogus itself, so
/// this queries it locally through MediatR, like DRC's own-service InformatieObjectResolver does.
/// The result is cached per request (keyed by catalogus URL, like DRC's InformatieObjectTypeCatalogusResolver),
/// since a list of InformatieObjectType/ZaakType/BesluitType typically references only a handful of
/// distinct catalogi.
///
/// One non-generic class implements IExpandResolver&lt;TEntity&gt; for every supported entity type,
/// with one ResolveAsync overload per type: the entity types don't share a common interface exposing
/// "Catalogus", so there's no single TEntity to be generic over -- only the fetch-by-id logic
/// (ResolveCatalogusAsync) is actually shared.
/// </summary>
public class Catalogus_Resolver
    : IExpandResolver<InformatieObjectTypeResponseDto>,
        IExpandResolver<ZaakTypeResponseDto>,
        IExpandResolver<BesluitTypeResponseDto>
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

    private async Task<object> ResolveCatalogusAsync(string catalogusUrl) =>
        await _catalogusCache.GetOrCacheAndGetAsync(
            $"key_{catalogusUrl}",
            async () =>
            {
                // Note: Eigen scope (dus eigen DbContext) per aanroep, zodat concurrente resolves voor
                // verschillende entities uit dezelfde lijst nooit dezelfde scoped DbContext-instantie delen.
                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

                var result = await mediator.Send(new GetCatalogusQuery { Id = _uriService.GetId(catalogusUrl) });
                if (result.Status != QueryStatus.OK)
                {
                    throw new ExpandInternalQueryHandlerException("catalogus", result.Status);
                }

                return mapper.Map<CatalogusResponseDto>(result.Result);
            }
        );
}
