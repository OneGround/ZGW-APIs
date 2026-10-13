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
/// Resolves the "deelzaken" expand path on a ZAAK: the inverse of <see cref="ZaakHoofdzaakResolver"/>, a list of ZAAK references.
/// <para>
/// All deelzaken are fetched in one <c>GetAllZakenQuery</c> (<c>Uuid__in</c>, sized to the exact count) instead of one query per url. A
/// deelzaak that the query does not return (the authorization join leaves it out) is omitted, see <see cref="ExpandQueryStatus"/>.
/// </para>
/// <para>
/// Note: <c>GetAllZakenFilter</c> is built here, not by Mapster, so its <c>*__in</c> lists must be initialized: the handler calls
/// <c>.Any()</c> on each of them. A resolver unit test (which mocks IMediator) cannot catch a missing one.
/// </para>
/// <para>
/// Note: the handler can create a PostgreSQL TEMPORARY TABLE on the connection, so every call opens its own DI scope, like
/// <see cref="ZaakRollenResolver"/>. The nested "deelzaken.*" paths are resolved by the registered <see cref="ExpandEngine{TEntity}"/> of
/// <see cref="ZaakResponseDto"/> (taken from the <see cref="IServiceProvider"/> to avoid a circular construction, see
/// <see cref="ZaakHoofdzaakResolver"/>); the paths themselves come from <see cref="ZaakSelfReferenceExpandPaths"/>.
/// </para>
/// </summary>
public class ZaakDeelzakenResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ZaakDeelzakenResolver(IServiceProvider serviceProvider, IMapper mapper, IHttpContextAccessor httpContextAccessor = null)
    {
        _httpContextAccessor = httpContextAccessor;
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
                SRID = ExpandSrid.From(_httpContextAccessor),
                GetAllZakenFilter = new Models.v1._5.GetAllZakenFilter
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
