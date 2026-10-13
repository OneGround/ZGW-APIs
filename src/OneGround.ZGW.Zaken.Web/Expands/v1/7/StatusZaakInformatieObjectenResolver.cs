using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.ServiceAgent.Expands;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Documenten.Contracts.v1._7.Queries;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Documenten.ServiceAgent.v1._7;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the "zaakinformatieobjecten" expand path on a STATUS. Same shape as <see cref="ZaakZaakInformatieObjectenResolver"/> (DI scope per
/// row, cross-check with the DRC authorization, see its remarks), with two differences. The query is filtered on this STATUS and on its
/// <c>Zaak</c>: the request validators do not enforce that a ZAAKINFORMATIEOBJECT's status belongs to the same ZAAK, and a mismatch is left out.
/// And STATUS has no url-count field, so ZRC is always queried first; DRC is skipped when that returns nothing.
/// "...informatieobject.informatieobjecttype" is forwarded to the shared engine, a 4th nesting level is not offered (3-level expand cap of the
/// VNG ZGW spec).
/// </summary>
public class StatusZaakInformatieObjectenResolver : IExpandResolver<StatusResponseDto>
{
    private const string ServiceName = ServiceRoleName.DRC;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;
    private readonly IUserAuthDocumentenServiceAgent _documentenServiceAgent;
    private readonly ExpandEngine<ZaakInformatieObjectResponseDto> _zaakInformatieObjectExpandEngine;
    private readonly IGenericCache<ServiceAgentResponse<IEnumerable<ObjectInformatieObjectResponseDto>>> _objectInformatieObjectenCache;

    public StatusZaakInformatieObjectenResolver(
        IServiceProvider serviceProvider,
        IMapper mapper,
        IUserAuthDocumentenServiceAgent documentenServiceAgent,
        ExpandEngine<ZaakInformatieObjectResponseDto> zaakInformatieObjectExpandEngine,
        IGenericCache<ServiceAgentResponse<IEnumerable<ObjectInformatieObjectResponseDto>>> objectInformatieObjectenCache
    )
    {
        _objectInformatieObjectenCache = objectInformatieObjectenCache;
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

        // Note: once per request and zaak, shared with ZaakZaakInformatieObjectenResolver (see ExpandCacheKeys): all statuses of one zaak ask
        // DRC the same thing
        var objectInformatieObjecten = await _objectInformatieObjectenCache.GetOrCacheAndGetAsync(
            ExpandCacheKeys.ObjectInformatieObjecten(entity.Zaak),
            () => _documentenServiceAgent.GetObjectInformatieObjectenAsync(new GetAllObjectInformatieObjectenQueryParameters { Object = entity.Zaak })
        );

        if (!objectInformatieObjecten.Success || objectInformatieObjecten.Response == null)
        {
            // Note: DRC decides as the caller. When it answers that the caller may not read the objectinformatieobjecten (403/404), no document of
            // this zaak is visible to the caller: nothing to expand, instead of failing the request (see ExpandQueryStatus).
            if (ExpandExternalServiceException.IsNotAvailableToCaller(objectInformatieObjecten))
            {
                return new List<ZaakInformatieObjectResponseDto>();
            }

            // Note: the url of what was asked of DRC (not the url of the zaak the expand is about)
            throw ExpandExternalServiceException.ForFailedResponse(
                ServiceName,
                $"objectinformatieobjecten?object={entity.Zaak}",
                objectInformatieObjecten
            );
        }

        var mirroredInformatieObjecten = objectInformatieObjecten.Response.Select(oio => oio.InformatieObject).ToHashSet();

        var zaakinformatieobjecten = result.Result.Where(zio => mirroredInformatieObjecten.Contains(zio.InformatieObject)).ToList();

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
