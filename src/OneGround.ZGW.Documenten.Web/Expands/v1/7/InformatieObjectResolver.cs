using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Handlers.v1._7;

namespace OneGround.ZGW.Documenten.Web.Expands.v1._7;

/// <summary>
/// Resolves the "informatieobject" expand path from an entity that references it by URL
/// (GebruiksRecht, ObjectInformatieObject, Verzending). Shared by all three, since they only
/// differ in how the URL is read off the entity. An INFORMATIEOBJECT that is not available to the caller
/// (NotFound/Forbidden) resolves to an empty object instead of failing the request, see <see cref="ExpandQueryStatus"/>.
/// </summary>
public class InformatieObjectResolver<TEntity> : IExpandResolver<TEntity>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEntityUriService _uriService;
    private readonly Func<TEntity, string> _informatieObjectUrl;
    private readonly IGenericCache<QueryResult<EnkelvoudigInformatieObject>> _informatieObjectCache;

    public InformatieObjectResolver(
        IServiceProvider serviceProvider,
        IEntityUriService uriService,
        Func<TEntity, string> informatieObjectUrl,
        IGenericCache<QueryResult<EnkelvoudigInformatieObject>> informatieObjectCache
    )
    {
        _informatieObjectCache = informatieObjectCache;
        _serviceProvider = serviceProvider;
        _uriService = uriService;
        _informatieObjectUrl = informatieObjectUrl;
    }

    public string Path => "informatieobject";
    public string Parent => null;

    public async Task<object> ResolveAsync(TEntity entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        // Note: once per request: a list filtered on one informatieobject has N rows that all expand to the same document
        var id = _uriService.GetId(_informatieObjectUrl(entity));
        var result = await _informatieObjectCache.GetOrCacheAndGetAsync(
            $"informatieobject_{id}",
            async () =>
            {
                // Note: Eigen scope (dus eigen DbContext) per query, zodat concurrente resolves voor verschillende entities uit
                // dezelfde lijst nooit dezelfde scoped DbContext-instantie delen. Alleen een cache-miss heeft een scope nodig, en die
                // wordt weer vrijgegeven zodra de query klaar is.
                using var scope = _serviceProvider.CreateScope();

                return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new GetEnkelvoudigInformatieObjectQuery { Id = id });
            }
        );
        if (!ExpandQueryStatus.IsAvailable(result.Status, "informatieobject"))
        {
            return null;
        }

        return _serviceProvider.GetRequiredService<IMapper>().Map<EnkelvoudigInformatieObjectGetResponseDto>(result.Result);
    }
}
