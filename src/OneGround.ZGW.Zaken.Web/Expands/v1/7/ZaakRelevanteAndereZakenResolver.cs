using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Helpers;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "relevanteanderezaken" expand path on a ZAAK. Like
/// <see cref="ZaakDeelzakenResolver"/>, this is a collection of ZAAK references resolved in one
/// batched <c>GetAllZakenQuery</c> call (filtered by <c>Uuid__in</c>, sized to the exact count, fresh
/// DI scope per ZAAK, silently-omits-inaccessible-entries semantics) -- see that resolver's own
/// remarks, all of which apply here identically. The only structural difference:
/// <see cref="ZaakResponseDto.RelevanteAndereZaken"/> is a list of <c>{ url, aardRelatie }</c> wrapper
/// objects, not plain URL strings, so the urls are projected out first; <c>aardRelatie</c> itself is
/// never embedded in the expand (matching the old v1._5 RelevanteAndereZakenExpander, which discarded
/// it the same way -- it stays visible on the un-expanded "relevanteAndereZaken" field regardless).
/// <para>
/// Unlike <see cref="ZaakHoofdzaakResolver"/>/<see cref="ZaakDeelzakenResolver"/>, the nested scope
/// here is deliberately narrower -- only "zaaktype", "status"/"status.statustype", and
/// "resultaat"/"resultaat.resultaattype" (no "zaaktype.catalogus", "rollen", "zaakobjecten", or
/// "zaakinformatieobjecten") -- matching the old v1._5 SupportedExpands list for this resource exactly,
/// a deliberate scope decision rather than an oversight. So <see cref="AdditionalPaths"/> is its own list here, not
/// the shared <see cref="ZaakSelfReferenceExpandPaths.Build"/> used by the other two (though
/// <see cref="ZaakSelfReferenceExpandPaths.ExtractNestedPaths"/> is still reused, since that part is
/// scope-agnostic).
/// </para>
/// <para>
/// Unlike deelzaken (a navigation collection derived from a child's own foreign key, so it can never
/// contain the same ZAAK twice), relevanteAndereZaken is a freestanding list of relations and CAN
/// legitimately reference the same ZAAK more than once with a different <c>aardRelatie</c> each time.
/// <c>Uuid__in</c> naturally deduplicates (a SQL <c>IN</c> list only ever returns one row per matching
/// id), so the fetched rows are re-expanded back against the ORIGINAL (possibly-duplicated,
/// order-preserved) url list below, rather than returned as-is -- otherwise a duplicate reference would
/// silently collapse to one expanded entry. Every reference therefore keeps its own entry, in the order
/// of the un-expanded "relevanteAndereZaken" field.
/// </para>
/// <para>
/// A referenced ZAAK that the query does not return -- it does not exist, the caller may not see it, or
/// it lives in another ZRC -- is simply omitted, exactly as for <see cref="ZaakDeelzakenResolver"/>;
/// there is no placeholder. The expanded list is therefore NOT positionally aligned with the un-expanded
/// "relevanteAndereZaken" field in general: a caller must match an expanded ZAAK to its relation by the
/// <c>url</c> of the expanded ZAAK, never by index.
/// </para>
/// </summary>
public class ZaakRelevanteAndereZakenResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ZaakRelevanteAndereZakenResolver(IServiceProvider serviceProvider, IMapper mapper, IHttpContextAccessor httpContextAccessor = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
        _mapper = mapper;
    }

    public string Path => "relevanteanderezaken";
    public string Parent => null;
    public IEnumerable<(string Path, string Parent)> AdditionalPaths =>
        [
            ($"{Path}.zaaktype", Path),
            ($"{Path}.status", Path),
            ($"{Path}.status.statustype", $"{Path}.status"),
            ($"{Path}.resultaat", Path),
            ($"{Path}.resultaat.resultaattype", $"{Path}.resultaat"),
        ];

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        var relevanteAndereZaakUrls = entity.RelevanteAndereZaken?.Select(r => r.Url).ToList() ?? [];
        if (relevanteAndereZaakUrls.Count == 0)
        {
            return new List<ZaakResponseDto>();
        }

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var uuids = relevanteAndereZaakUrls.Select(UriHelper.GetResourceId).ToList();

        var result = await mediator.Send(
            new Handlers.v1._5.GetAllZakenQuery
            {
                SRID = ExpandSrid.From(_httpContextAccessor),
                GetAllZakenFilter = new Models.v1._5.GetAllZakenFilter
                {
                    Uuid__in = uuids,
                    Zaaktype__in = [],
                    Archiefnominatie__in = [],
                    Archiefstatus__in = [],
                    Bronorganisatie__in = [],
                },
                Pagination = new PaginationFilter { Page = 1, Size = relevanteAndereZaakUrls.Count },
            }
        );

        var zakenByUuid = result.Result.PageResult.ToDictionary(z => z.Id);
        // Note: references that were not returned are omitted (no placeholder), see the class remarks
        var orderedZaken = uuids.Where(zakenByUuid.ContainsKey).Select(id => zakenByUuid[id]).ToList();
        var relevanteAndereZaken = _mapper.Map<List<ZaakResponseDto>>(orderedZaken);

        var nestedPaths = ZaakSelfReferenceExpandPaths.ExtractNestedPaths(requestedPaths, Path);

        if (nestedPaths.Count > 0)
        {
            var zaakExpandEngine = _serviceProvider.GetRequiredService<ExpandEngine<ZaakResponseDto>>();
            await zaakExpandEngine.ResolveListAsync(relevanteAndereZaken, nestedPaths);
        }

        return relevanteAndereZaken;
    }
}
