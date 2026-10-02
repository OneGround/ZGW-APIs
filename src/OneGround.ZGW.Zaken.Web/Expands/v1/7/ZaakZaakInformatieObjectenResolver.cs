using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.ServiceAgent.Expands;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Documenten.Contracts.v1._7.Queries;
using OneGround.ZGW.Documenten.ServiceAgent.v1._7;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "zaakinformatieobjecten" expand path on a ZAAK. Like
/// <see cref="ZaakRollenResolver"/>/<see cref="ZaakZaakObjectenResolver"/>, opens a fresh DI scope per
/// ZAAK before querying (its handler creates a PostgreSQL temp table for row-level authorization --
/// see ZaakRollenResolver's own remarks). Unlike every other list resolver this session, the raw
/// ZRC-side rows aren't the final answer: a ZAAKINFORMATIEOBJECT must also be confirmed by DRC's own
/// authorization model (<see cref="IUserAuthDocumentenServiceAgent.GetObjectInformatieObjectenAsync"/>),
/// replicating the old v1._5 ZaakInformatieObjectenExpander's filter. Uses the v1._7
/// UserAccount-authenticated agent (not the ServiceAccount-authenticated plain v1._7 one, nor the
/// obsolete v1._5 one) so that authorization check reflects the real calling client.
/// "...informatieobject.informatieobjecttype" is forwarded to
/// <see cref="ZaakInformatieObjectInformatieObjectResolver"/> via <see cref="AdditionalPaths"/>; no
/// "...catalogus" sibling (a 4th nesting level would exceed the VNG ZGW spec's 3-level expand cap).
/// </summary>
public class ZaakZaakInformatieObjectenResolver : IExpandResolver<ZaakResponseDto>
{
    private const string ServiceName = "DRC";
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;
    private readonly IUserAuthDocumentenServiceAgent _documentenServiceAgent;
    private readonly ExpandEngine<ZaakInformatieObjectResponseDto> _zaakInformatieObjectExpandEngine;

    public ZaakZaakInformatieObjectenResolver(
        IServiceProvider serviceProvider,
        IMapper mapper,
        IUserAuthDocumentenServiceAgent documentenServiceAgent,
        ExpandEngine<ZaakInformatieObjectResponseDto> zaakInformatieObjectExpandEngine
    )
    {
        _serviceProvider = serviceProvider;
        _mapper = mapper;
        _documentenServiceAgent = documentenServiceAgent;
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

        var objectInformatieObjecten = await _documentenServiceAgent.GetObjectInformatieObjectenAsync(
            new GetAllObjectInformatieObjectenQueryParameters { Object = entity.Url }
        );

        if (!objectInformatieObjecten.Success)
        {
            throw new ExpandExternalServiceException(ServiceName, entity.Url, null);
        }

        var authorizedInformatieObjecten = objectInformatieObjecten.Response.Select(oio => oio.InformatieObject).ToHashSet();

        var zaakinformatieobjecten = result.Result.Where(zio => authorizedInformatieObjecten.Contains(zio.InformatieObject)).ToList();

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
