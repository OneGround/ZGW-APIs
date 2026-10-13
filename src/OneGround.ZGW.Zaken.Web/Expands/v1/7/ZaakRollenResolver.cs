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

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the "rollen" expand path on a ZAAK. All ROLs of the ZAAK are fetched in one <c>GetAllZaakRolQuery</c> (sized to the number of role
/// urls on the ZAAK) instead of one query per url.
/// <para>
/// "rollen.roltype" is an <see cref="AdditionalPaths"/> entry: the engine only embeds a child into a single parent, not into every item of a
/// list. So the built ROL list is handed to the registered <see cref="ExpandEngine{TEntity}"/> of <c>RolResponseDto</c> (<c>ResolveListAsync</c>).
/// </para>
/// <para>
/// Note: when the caller lacks HasAllAuthorizations, the query handler creates a PostgreSQL TEMPORARY TABLE on the connection (see
/// ZaakAuthorizationTempTableService) and never drops it. Reusing the request-scoped IMediator for a second ZAAK of a page would create it
/// again and fail with "already exists", so every call opens its own DI scope (a new DbContext/connection), like the v1.5 RollenExpander.
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
                GetAllZaakRolFilter = new Models.v1.GetAllZaakRollenFilter { Zaak = entity.Url },
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
