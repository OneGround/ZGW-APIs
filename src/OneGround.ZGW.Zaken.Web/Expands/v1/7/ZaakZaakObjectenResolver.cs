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
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakObject;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "zaakobjecten" expand path on a ZAAK. Mirrors <see cref="ZaakRollenResolver"/>
/// exactly (same reasoning applies to both members of its class doc comment: the collection-fetch
/// shape, the "zaakobjecten.zaakobjecttype" AdditionalPaths batch-embed via the resource's own
/// ExpandEngine, and -- most importantly -- the fresh-DI-scope-per-ZAAK requirement, since
/// GetAllZaakObjectenQuery's handler has the same TempZaakAuthorization temp-table behavior as
/// GetAllZaakRolQuery's).
/// </summary>
public class ZaakZaakObjectenResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;
    private readonly ExpandEngine<ZaakObjectResponseDto> _zaakObjectExpandEngine;

    public ZaakZaakObjectenResolver(IServiceProvider serviceProvider, IMapper mapper, ExpandEngine<ZaakObjectResponseDto> zaakObjectExpandEngine)
    {
        _serviceProvider = serviceProvider;
        _mapper = mapper;
        _zaakObjectExpandEngine = zaakObjectExpandEngine;
    }

    public string Path => "zaakobjecten";
    public string Parent => null;
    public IEnumerable<(string Path, string Parent)> AdditionalPaths => [("zaakobjecten.zaakobjecttype", "zaakobjecten")];

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        var zaakObjectCount = entity.ZaakObjecten?.Count() ?? 0;
        if (zaakObjectCount == 0)
        {
            return new List<ZaakObjectResponseDto>();
        }

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new Handlers.v1._5.GetAllZaakObjectenQuery
            {
                GetAllZaakObjectenFilter = new Models.v1.GetAllZaakObjectenFilter { Zaak = entity.Url },
                Pagination = new PaginationFilter { Page = 1, Size = zaakObjectCount },
            }
        );

        var zaakobjecten = _mapper.Map<List<ZaakObjectResponseDto>>(result.Result.PageResult);

        if (requestedPaths.Contains("zaakobjecten.zaakobjecttype"))
        {
            await _zaakObjectExpandEngine.ResolveListAsync(zaakobjecten, ["zaakobjecttype"]);
        }

        return zaakobjecten;
    }
}
