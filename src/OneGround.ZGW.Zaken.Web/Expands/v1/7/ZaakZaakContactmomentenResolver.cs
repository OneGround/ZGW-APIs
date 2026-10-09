using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "zaakcontactmomenten" expand path on a ZAAK. Unlike
/// <see cref="ZaakRollenResolver"/>/<see cref="ZaakZaakObjectenResolver"/>, there is no
/// "Rollen"/"ZaakObjecten"-style URL-list field on <see cref="ZaakResponseDto"/> to short-circuit on
/// (ZAAK has no navigation back to its ZAAKCONTACTMOMENTen at all) -- this always queries, exactly
/// like the old (v1._5) ZaakContactmomentenExpander already does for this identical query. No nested
/// expand is requested (no "zaakcontactmomenten.X"), so there are no <see cref="AdditionalPaths"/>.
/// <para>
/// GetAllZaakContactmomentenQuery's handler, when the caller lacks HasAllAuthorizations, creates a
/// PostgreSQL TEMPORARY TABLE on the DbContext's connection to apply row-level authorization (see
/// ZaakAuthorizationTempTableService) and never drops it. ResolveListAsync invokes this resolver once
/// per ZAAK in a page via the SAME request-scoped IMediator/DbContext, so sending the query directly
/// on the injected IMediator would try to CREATE the same temp table again for the second ZAAK and
/// crash with a PostgreSQL "already exists" error. Opening a fresh DI scope per ZAAK (a new
/// DbContext/connection each time) avoids the collision -- exactly the workaround
/// ZaakRollenResolver/ZaakZaakObjectenResolver already use for the identical pattern.
/// </para>
/// </summary>
public class ZaakZaakContactmomentenResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;

    public ZaakZaakContactmomentenResolver(IServiceProvider serviceProvider, IMapper mapper)
    {
        _serviceProvider = serviceProvider;
        _mapper = mapper;
    }

    public string Path => "zaakcontactmomenten";
    public string Parent => null;

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new Handlers.v1._5.GetAllZaakContactmomentenQuery
            {
                GetAllZaakContactmomentenFilter = new Models.v1._5.GetAllZaakContactmomentenFilter { Zaak = entity.Url },
            }
        );

        return _mapper.Map<List<ZaakContactmomentResponseDto>>(result.Result);
    }
}
