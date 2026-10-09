using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Helpers;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "status" expand path on a ZAAKINFORMATIEOBJECT. Mirrors
/// <see cref="ZaakInformatieObjectZaakResolver"/>: the STATUS lives in this same service, so this goes
/// through the existing <c>GetZaakStatusQuery</c> via MediatR rather than a ServiceAgent. "status" is
/// an optional field on ZAAKINFORMATIEOBJECT, hence the null-guard. A STATUS that is not available to the caller (NotFound/Forbidden)
/// resolves to null (the ExpandEngine makes that an empty object) instead of failing the request, see
/// <see cref="ExpandQueryStatus"/>. "status.statustype" is forwarded to the already-registered <see cref="ExpandEngine{TEntity}"/> of
/// <see cref="StatusResponseDto"/> itself (the same <see cref="StatusStatusTypeResolver"/> used for the
/// top-level STATUS) -- no duplication.
/// <para>
/// Unlike the "zaak.zaaktype" resolvers (where <see cref="Lazy{T}"/> is purely a construction-cost
/// optimization, since no real cycle exists there), this one is genuinely circular and
/// <see cref="Lazy{T}"/> is REQUIRED, not optional: <see cref="ExpandEngine{TEntity}"/> of
/// <see cref="StatusResponseDto"/> depends on <see cref="StatusZaakInformatieObjectenResolver"/>, which
/// has an eager (non-<see cref="Lazy{T}"/>) dependency on <see cref="ExpandEngine{TEntity}"/> of
/// <see cref="ZaakInformatieObjectResponseDto"/>, which in turn depends on this resolver -- closing the
/// loop back to <see cref="StatusResponseDto"/>'s own engine. Changing this constructor parameter to a
/// plain (non-lazy) <c>ExpandEngine&lt;StatusResponseDto&gt;</c> would make that cycle real and throw a
/// DI "circular dependency" exception the first time either engine is resolved.
/// </para>
/// </summary>
public class ZaakInformatieObjectStatusResolver : IExpandResolver<ZaakInformatieObjectResponseDto>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly Lazy<ExpandEngine<StatusResponseDto>> _statusExpandEngine;
    private readonly IGenericCache<QueryResult<ZaakStatus>> _statusCache;

    public ZaakInformatieObjectStatusResolver(
        IMediator mediator,
        IMapper mapper,
        Lazy<ExpandEngine<StatusResponseDto>> statusExpandEngine,
        IGenericCache<QueryResult<ZaakStatus>> statusCache
    )
    {
        _statusCache = statusCache;
        _mediator = mediator;
        _mapper = mapper;
        _statusExpandEngine = statusExpandEngine;
    }

    public string Path => "status";
    public string Parent => null;
    public IEnumerable<(string Path, string Parent)> AdditionalPaths => [("status.statustype", "status")];

    public async Task<object> ResolveAsync(
        ZaakInformatieObjectResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        if (string.IsNullOrEmpty(entity.Status))
        {
            return null;
        }

        // Note: several documents are filed at the same status, and GetZaakStatusQuery also loads all documents of the zaak: once per request
        var statusId = UriHelper.GetResourceId(entity.Status);
        var result = await _statusCache.GetOrCacheAndGetAsync(
            $"status_{statusId}",
            () => _mediator.Send(new Handlers.v1._5.GetZaakStatusQuery { Id = statusId })
        );

        if (!ExpandQueryStatus.IsAvailable(result.Status, Path))
        {
            return null;
        }

        var status = _mapper.Map<StatusResponseDto>(result.Result);

        if (requestedPaths.Contains("status.statustype"))
        {
            await _statusExpandEngine.Value.ResolveAsync(status, ["statustype"]);
        }

        return status;
    }
}
