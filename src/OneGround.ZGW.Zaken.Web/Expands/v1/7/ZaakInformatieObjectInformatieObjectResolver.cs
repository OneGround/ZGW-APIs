using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.ServiceAgent.Expands;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Documenten.ServiceAgent.v1._7;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the "informatieobject" expand path on a ZAAKINFORMATIEOBJECT from DRC, through the user-authenticated
/// <see cref="IUserAuthDocumentenServiceAgent"/> so DRC evaluates the request as the real calling client. A failed call throws
/// <see cref="ExpandExternalServiceException"/>. "informatieobjecttype" is resolved by
/// <see cref="EnkelvoudigInformatieObjectInformatieObjectTypeResolver"/>, not by the expand of DRC: its raw JSON breaks the fields projection (a
/// JObject reads as IEnumerable, so a single informatieobject is misread as a list).
/// </summary>
public class ZaakInformatieObjectInformatieObjectResolver : IExpandResolver<ZaakInformatieObjectResponseDto>
{
    private const string ServiceName = "DRC";
    private readonly IUserAuthDocumentenServiceAgent _documentenServiceAgent;
    private readonly IMapper _mapper;
    private readonly ExpandEngine<EnkelvoudigInformatieObjectGetResponseDto> _informatieObjectExpandEngine;
    private readonly IGenericCache<ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>> _documentCache;

    public ZaakInformatieObjectInformatieObjectResolver(
        IUserAuthDocumentenServiceAgent documentenServiceAgent,
        IMapper mapper,
        ExpandEngine<EnkelvoudigInformatieObjectGetResponseDto> informatieObjectExpandEngine,
        IGenericCache<ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>> documentCache
    )
    {
        _documentCache = documentCache;
        _documentenServiceAgent = documentenServiceAgent;
        _mapper = mapper;
        _informatieObjectExpandEngine = informatieObjectExpandEngine;
    }

    public string Path => "informatieobject";
    public string Parent => null;
    public IEnumerable<(string Path, string Parent)> AdditionalPaths => [($"{Path}.informatieobjecttype", Path)];

    public async Task<object> ResolveAsync(
        ZaakInformatieObjectResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        // Note: once per request and url (the user-authenticated agent has no cache of its own): a list filtered on one informatieobject, or a
        // document that is linked more than once, would otherwise call DRC for every row. Only an exception is not kept; a response that is not
        // a success is, so every caller must handle it as below (403/404 => nothing to expand, anything else => the request fails).
        var result = await _documentCache.GetOrCacheAndGetAsync(
            $"informatieobject_{entity.InformatieObject}",
            () => _documentenServiceAgent.GetEnkelvoudigInformatieObjectByUrlAsync(entity.InformatieObject)
        );

        if (!result.Success || result.Response == null)
        {
            // Note: DRC evaluates the request as the caller. A document that DRC says is not available to the caller (403/404) is left out of the
            // expand, like an entity that the same-service query does not return (see ExpandQueryStatus), instead of failing the request.
            if (ExpandExternalServiceException.IsNotAvailableToCaller(result))
            {
                return null;
            }

            throw ExpandExternalServiceException.ForFailedResponse(ServiceName, entity.InformatieObject, result);
        }

        var informatieobject = _mapper.Map<EnkelvoudigInformatieObjectGetResponseDto>(result.Response);

        if (requestedPaths.Contains($"{Path}.informatieobjecttype"))
        {
            await _informatieObjectExpandEngine.ResolveAsync(informatieobject, ["informatieobjecttype"]);
        }

        return informatieobject;
    }
}
