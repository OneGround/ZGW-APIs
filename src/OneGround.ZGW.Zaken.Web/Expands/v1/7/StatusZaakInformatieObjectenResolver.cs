using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the "zaakinformatieobjecten" expand path on a STATUS. Same shape as <see cref="ZaakZaakInformatieObjectenResolver"/> (DI scope per
/// row, ZRC rows are leading and not cross-checked against DRC, see its remarks), with two differences. The query is filtered on this STATUS and on its
/// <c>Zaak</c>: the request validators do not enforce that a ZAAKINFORMATIEOBJECT's status belongs to the same ZAAK, and a mismatch is left out.
/// And STATUS has no url-count field, so ZRC is always queried, also when the STATUS has no ZAAKINFORMATIEOBJECTEN.
/// "...informatieobject.informatieobjecttype" is forwarded to the shared engine, a 4th nesting level is not offered (3-level expand cap of the
/// VNG ZGW spec).
/// </summary>
public class StatusZaakInformatieObjectenResolver : IExpandResolver<StatusResponseDto>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;

    private readonly ExpandEngine<ZaakInformatieObjectResponseDto> _zaakInformatieObjectExpandEngine;

    public StatusZaakInformatieObjectenResolver(
        IServiceProvider serviceProvider,
        IMapper mapper,
        ExpandEngine<ZaakInformatieObjectResponseDto> zaakInformatieObjectExpandEngine
    )
    {
        _serviceProvider = serviceProvider;
        _mapper = mapper;
        _zaakInformatieObjectExpandEngine = zaakInformatieObjectExpandEngine;
    }

    public string Path => "zaakinformatieobjecten";
    public string Parent => null;
    public IEnumerable<(string Path, string Parent)> AdditionalPaths =>
        [
            ("zaakinformatieobjecten.informatieobject", "zaakinformatieobjecten"),
            ("zaakinformatieobjecten.informatieobject.informatieobjecttype", "zaakinformatieobjecten.informatieobject"),
        ];

    public async Task<object> ResolveAsync(
        StatusResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new Handlers.v1._5.GetAllZaakInformatieObjectenQuery
            {
                GetAllZaakInformatieObjectenFilter = new Models.v1.GetAllZaakInformatieObjectenFilter { Status = entity.Url, Zaak = entity.Zaak },
            }
        );

        if (result.Result.Count == 0)
        {
            return new List<ZaakInformatieObjectResponseDto>();
        }

        var zaakinformatieobjecten = result.Result.ToList();

        var response = _mapper.Map<List<ZaakInformatieObjectResponseDto>>(zaakinformatieobjecten);

        if (requestedPaths.Contains("zaakinformatieobjecten.informatieobject"))
        {
            List<string> nestedPaths = requestedPaths.Contains("zaakinformatieobjecten.informatieobject.informatieobjecttype")
                ? ["informatieobject", "informatieobject.informatieobjecttype"]
                : ["informatieobject"];

            await _zaakInformatieObjectExpandEngine.ResolveListAsync(response, nestedPaths);
        }

        return response;
    }
}
