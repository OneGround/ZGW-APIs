using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Helpers;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Web.Models.v1._5;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "deelzaken" expand path on a ZAAK -- the inverse of
/// <see cref="ZaakHoofdzaakResolver"/> (a list of ZAAK references instead of one). Like
/// <see cref="ZaakRollenResolver"/>, this is a collection: fetches every deelzaak in one query (the
/// existing <c>GetAllZakenQuery</c>, filtered by <c>Uuid__in</c> on the deelzaak urls already on the
/// ZAAK, no artificial page limit -- Size is set to the exact deelzaak count) rather than one
/// <c>GetZaakQuery</c> per url, which would compound badly across a page of ZAKEN that each have
/// their own deelzaken.
/// <para>
/// <see cref="OneGround.ZGW.Zaken.Web.Models.v1._5.GetAllZakenFilter"/> is normally only ever
/// constructed by Mapster from <c>GetAllZakenQueryParameters</c>, which guarantees its <c>IList&lt;T&gt;</c>
/// properties are never null; built directly here instead, so <c>Zaaktype__in</c>/<c>Archiefnominatie__in</c>/
/// <c>Archiefstatus__in</c>/<c>Bronorganisatie__in</c> must be explicitly initialized to empty lists --
/// <c>GetAllZakenQueryHandler</c>'s own filter-predicate chain calls <c>.Any()</c> on each of them
/// unconditionally and would NullReferenceException otherwise. This can't be caught by a resolver unit
/// test (which mocks IMediator), so take care if this filter's shape ever changes.
/// </para>
/// <para>
/// GetAllZakenQuery's handler, when the caller lacks HasAllAuthorizations, creates a PostgreSQL
/// TEMPORARY TABLE on the DbContext's connection for row-level authorization (see
/// ZaakRollenResolver's own remarks for why this means a fresh DI scope per ZAAK is required here too).
/// That same authorization join also means a deelzaak the caller can't see (wrong RSIN, too
/// confidential) is silently absent from the result -- deliberately, unlike
/// <see cref="ZaakHoofdzaakResolver"/>'s single reference, which throws instead. A list naturally
/// tolerates a missing/inaccessible entry (same as ZaakRollenResolver/ZaakZaakObjectenResolver/
/// ZaakZaakInformatieObjectenResolver, which never throw per-row either); failing the whole expand
/// over one unreadable deelzaak would not.
/// </para>
/// <para>
/// Nested "deelzaken.*" paths are resolved via the ALREADY-REGISTERED <see cref="ExpandEngine{TEntity}"/>
/// of <see cref="ZaakResponseDto"/> itself (same resolvers used for the top-level ZAAK and for
/// "hoofdzaak.*"). Resolved lazily here via <see cref="IServiceProvider"/> rather than a constructor
/// dependency, for the same circular-construction reason as <see cref="ZaakHoofdzaakResolver"/> --
/// which instead takes an explicit <c>Lazy&lt;ExpandEngine{ZaakResponseDto}&gt;</c> constructor
/// dependency, since it has no other need for <see cref="IServiceProvider"/>; this resolver keeps the
/// service-locator style instead because it already needs <see cref="IServiceProvider"/> for
/// <c>CreateScope()</c> above. The nested "deelzaken.*" tuples themselves come from
/// <see cref="ZaakSelfReferenceExpandPaths"/>, shared with ZaakHoofdzaakResolver.
/// </para>
/// <para>
/// GetAllZakenQuery's handler always also runs a cached COUNT(*) query (for the paging metadata this
/// resolver never reads) under a cache key that includes the (per-ZAAK-unique) Uuid__in filter, so
/// that cache practically never hits here -- an accepted cost of reusing the general-purpose query
/// rather than building a narrower one just for this.
/// </para>
/// </summary>
public class ZaakDeelzakenResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;

    public ZaakDeelzakenResolver(IServiceProvider serviceProvider, IMapper mapper)
    {
        _serviceProvider = serviceProvider;
        _mapper = mapper;
    }

    public string Path => "deelzaken";
    public string Parent => null;
    public IEnumerable<(string Path, string Parent)> AdditionalPaths => ZaakSelfReferenceExpandPaths.Build(Path);

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        var deelzaakUrls = entity.Deelzaken?.ToList() ?? [];
        if (deelzaakUrls.Count == 0)
        {
            return new List<ZaakResponseDto>();
        }

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var uuids = deelzaakUrls.Select(UriHelper.GetResourceId).ToList();

        var result = await mediator.Send(
            new Handlers.v1._5.GetAllZakenQuery
            {
                GetAllZakenFilter = new GetAllZakenFilter
                {
                    Uuid__in = uuids,
                    Zaaktype__in = [],
                    Archiefnominatie__in = [],
                    Archiefstatus__in = [],
                    Bronorganisatie__in = [],
                },
                Pagination = new PaginationFilter { Page = 1, Size = deelzaakUrls.Count },
            }
        );

        var deelzaken = _mapper.Map<List<ZaakResponseDto>>(result.Result.PageResult);

        var nestedPaths = ZaakSelfReferenceExpandPaths.ExtractNestedPaths(requestedPaths, Path);

        if (nestedPaths.Count > 0)
        {
            var zaakExpandEngine = _serviceProvider.GetRequiredService<ExpandEngine<ZaakResponseDto>>();
            await zaakExpandEngine.ResolveListAsync(deelzaken, nestedPaths);
        }

        return deelzaken;
    }
}
