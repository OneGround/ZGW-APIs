using System;
using System.Threading.Tasks;
using MediatR;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Fetches the ZAAK that an expand resolver needs (the HOOFDZAAK of a zaak, or the "zaak" of a status, rol, zaakobject, ...), at most once per
/// request: a list that is filtered on one zaak has N rows that all expand to the same zaak, and <c>GetZaakQuery</c> is a heavy split query.
/// <para>
/// What is kept is the query result (the entity or its NotFound/Forbidden status), keyed by id and SRID. The resolvers map it themselves, so no
/// two places share a DTO (the engine writes <c>_expand</c> into it). It is scoped, so it never outlives the request of one caller.
/// A query that throws is not kept (see <see cref="IGenericCache{T}"/>). A result with a failure status is, and every call that gets it
/// throws <see cref="ExpandInternalQueryHandlerException"/> with its own resource: the request ends with that failure anyway.
/// </para>
/// <para>
/// A ZAAK that is not available to the caller resolves to null, see <see cref="ExpandQueryStatus"/>.
/// </para>
/// </summary>
public interface IZaakLookup
{
    /// <param name="id">The id of the zaak.</param>
    /// <param name="resource">The name that a failure is reported with (<see cref="ExpandInternalQueryHandlerException.Resource"/>).</param>
    /// <param name="srid">The SRID in which the geometry of the zaak is returned.</param>
    /// <returns>The zaak, or null when it is not available to the caller (NotFound/Forbidden).</returns>
    Task<Zaak> GetAsync(Guid id, string resource, int srid = ExpandSrid.Default);
}

public sealed class ZaakLookup : IZaakLookup
{
    private readonly IMediator _mediator;
    private readonly IGenericCache<QueryResult<Zaak>> _cache;

    public ZaakLookup(IMediator mediator, IGenericCache<QueryResult<Zaak>> cache)
    {
        _mediator = mediator;
        _cache = cache;
    }

    public async Task<Zaak> GetAsync(Guid id, string resource, int srid = ExpandSrid.Default)
    {
        var result = await _cache.GetOrCacheAndGetAsync(
            $"zaak_{id}_{srid}",
            () => _mediator.Send(new Handlers.v1._5.GetZaakQuery { Id = id, SRID = srid })
        );

        return ExpandQueryStatus.IsAvailable(result.Status, resource) ? result.Result : null;
    }
}
