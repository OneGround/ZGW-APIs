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
/// Resolves the "relevanteanderezaken" expand path on a ZAAK. Like <see cref="ZaakDeelzakenResolver"/> (see its remarks), the referenced
/// ZAAKs are fetched in one batched query and an unavailable one is omitted. The urls are projected out of the <c>{ url, aardRelatie }</c>
/// wrappers first; <c>aardRelatie</c> stays on the un-expanded field.
/// <para>
/// The nested scope is deliberately narrower than that of hoofdzaak/deelzaken (the v1.5 list), so <see cref="AdditionalPaths"/> is its own list.
/// </para>
/// <para>
/// Unlike deelzaken, the same ZAAK can be referenced more than once (with another <c>aardRelatie</c>), while <c>Uuid__in</c> deduplicates.
/// The fetched rows are therefore expanded back against the original url list, so every reference keeps its entry. Because omitted
/// references leave no placeholder, match an expanded ZAAK to its relation by <c>url</c>, never by index.
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
