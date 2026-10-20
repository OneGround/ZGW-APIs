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
/// Resolves the "zaakinformatieobjecten" expand path on a ZAAK. Like <see cref="ZaakRollenResolver"/>, every call opens its own DI scope (the
/// handler can create a PostgreSQL TEMPORARY TABLE). The ZAAKINFORMATIEOBJECT rows of ZRC are leading: they are authorized exactly like
/// GET /zaakinformatieobjecten (zaaktype and vertrouwelijkheidaanduiding of the zaak) and are not cross-checked against DRC, unlike the v1.5
/// expander. The document itself ("...informatieobject") is fetched from DRC as the real calling client (user-authenticated agent), so DRC
/// still decides what the caller may see of it.
/// "...informatieobject.informatieobjecttype" is forwarded to <see cref="ZaakInformatieObjectInformatieObjectResolver"/>; "...catalogus" is not
/// offered (3-level expand cap of the VNG ZGW spec).
/// </summary>
public class ZaakZaakInformatieObjectenResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;
    private readonly ExpandEngine<ZaakInformatieObjectResponseDto> _zaakInformatieObjectExpandEngine;

    public ZaakZaakInformatieObjectenResolver(
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

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        var zaakInformatieObjectCount = entity.ZaakInformatieObjecten?.Count() ?? 0;
        if (zaakInformatieObjectCount == 0)
        {
            return new List<ZaakInformatieObjectResponseDto>();
        }

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new Handlers.v1._5.GetAllZaakInformatieObjectenQuery
            {
                GetAllZaakInformatieObjectenFilter = new Models.v1.GetAllZaakInformatieObjectenFilter { Zaak = entity.Url },
            }
        );

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
