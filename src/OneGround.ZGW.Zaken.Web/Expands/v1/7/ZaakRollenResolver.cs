using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakRol;
using OneGround.ZGW.Zaken.Web.Models.v1;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "rollen" expand path on a ZAAK. Unlike Status/Resultaat, this field is a
/// collection: fetches every ROL for this ZAAK in one query (the existing GetAllZaakRolQuery,
/// filtered by zaak url, no artificial page limit -- Size is set to the exact number of role urls
/// already on the ZAAK) rather than one query per url.
/// <para>
/// "rollen.roltype" is declared via <see cref="AdditionalPaths"/> rather than as its own dispatched
/// resolver: the generic per-entity parent/child wiring in <see cref="ExpandEngine{TEntity}"/> only
/// auto-embeds a child into a single IExpandable parent, not into every item of a list. Instead, once
/// the ROL list is built, this resolver hands it to the already-registered
/// <see cref="ExpandEngine{TEntity}"/> (for <c>RolResponseDto</c> -- the same instance that powers
/// GET /rollen?expand=roltype) via its <c>ResolveListAsync</c>, which embeds "roltype" into each
/// ROL's own <c>_expand</c> -- exactly the batch operation that engine already exists for.
/// </para>
/// <para>
/// GetAllZaakRolQuery's handler, when the caller lacks HasAllAuthorizations, creates a PostgreSQL
/// TEMPORARY TABLE on the DbContext's connection to apply row-level authorization (see
/// ZaakAuthorizationTempTableService) and never drops it. ResolveListAsync invokes this resolver once
/// per ZAAK in a page via the SAME request-scoped IMediator/DbContext, so sending the query directly
/// on the injected IMediator would try to CREATE the same temp table again for the second ZAAK and
/// crash with a PostgreSQL "already exists" error. Opening a fresh DI scope per ZAAK (a new
/// DbContext/connection each time) avoids the collision -- exactly the workaround the old (v1._5)
/// RollenExpander already uses for this identical query.
/// </para>
/// </summary>
public class ZaakRollenResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;
    private readonly ExpandEngine<RolResponseDto> _rolExpandEngine;

    public ZaakRollenResolver(IServiceProvider serviceProvider, IMapper mapper, ExpandEngine<RolResponseDto> rolExpandEngine)
    {
        _serviceProvider = serviceProvider;
        _mapper = mapper;
        _rolExpandEngine = rolExpandEngine;
    }

    public string Path => "rollen";
    public string Parent => null;
    public IEnumerable<(string Path, string Parent)> AdditionalPaths => [("rollen.roltype", "rollen")];

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        var rolCount = entity.Rollen?.Count() ?? 0;
        if (rolCount == 0)
        {
            return new List<RolResponseDto>();
        }

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new Handlers.v1._5.GetAllZaakRolQuery
            {
                GetAllZaakRolFilter = new GetAllZaakRollenFilter { Zaak = entity.Url },
                Pagination = new PaginationFilter { Page = 1, Size = rolCount },
            }
        );

        var rollen = _mapper.Map<List<RolResponseDto>>(result.Result.PageResult);

        if (requestedPaths.Contains("rollen.roltype"))
        {
            await _rolExpandEngine.ResolveListAsync(rollen, ["roltype"]);
        }

        return rollen;
    }
}
