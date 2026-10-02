using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using OneGround.ZGW.Common.ServiceAgent.Expands;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Documenten.ServiceAgent.v1._7;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "informatieobject" expand path on a ZAAKINFORMATIEOBJECT. Unlike other
/// "type" resolvers this session, reads from DRC, via the v1._7 UserAccount-authenticated
/// <see cref="IUserAuthDocumentenServiceAgent"/> (not the ServiceAccount-authenticated plain v1._7
/// agent, nor the obsolete v1._5 one) so DRC evaluates the request as the real calling client, not
/// this backend's own credential. No WrapAsync equivalent exists for DRC, so a failed call throws
/// <see cref="ExpandExternalServiceException"/> directly. "informatieobjecttype" is resolved by a
/// separate ZRC-side ZTC resolver (<see cref="EnkelvoudigInformatieObjectInformatieObjectTypeResolver"/>)
/// rather than DRC's own expand -- forwarding DRC's raw JSON here instead breaks the fields/FieldProjector
/// mechanism (a JObject reads as IEnumerable, so a single informatieobject gets misread as a list).
/// </summary>
public class ZaakInformatieObjectInformatieObjectResolver : IExpandResolver<ZaakInformatieObjectResponseDto>
{
    private const string ServiceName = "DRC";
    private readonly IUserAuthDocumentenServiceAgent _documentenServiceAgent;
    private readonly IMapper _mapper;
    private readonly ExpandEngine<EnkelvoudigInformatieObjectGetResponseDto> _informatieObjectExpandEngine;

    public ZaakInformatieObjectInformatieObjectResolver(
        IUserAuthDocumentenServiceAgent documentenServiceAgent,
        IMapper mapper,
        ExpandEngine<EnkelvoudigInformatieObjectGetResponseDto> informatieObjectExpandEngine
    )
    {
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
        var result = await _documentenServiceAgent.GetEnkelvoudigInformatieObjectByUrlAsync(entity.InformatieObject);

        if (!result.Success || result.Response == null)
        {
            throw new ExpandExternalServiceException(ServiceName, entity.InformatieObject, null);
        }

        var informatieobject = _mapper.Map<EnkelvoudigInformatieObjectGetResponseDto>(result.Response);

        if (requestedPaths.Contains($"{Path}.informatieobjecttype"))
        {
            await _informatieObjectExpandEngine.ResolveAsync(informatieobject, ["informatieobjecttype"]);
        }

        return informatieobject;
    }
}
