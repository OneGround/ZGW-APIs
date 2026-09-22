using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Documenten.Contracts.v1._5.Responses;
using OneGround.ZGW.Documenten.Contracts.v1.Queries;

namespace OneGround.ZGW.Documenten.ServiceAgent.v1._5;

class CachedDocumentServiceAgent : ICachedDocumentenServiceAgent
{
    private readonly IDocumentenServiceAgent _agent;

    private readonly ConcurrentDictionary<string, ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>> _cachedEnkelvoudigInformatieObjectUrl = new();

    public CachedDocumentServiceAgent(IDocumentenServiceAgent agent)
    {
        _agent = agent;
    }

    public async Task<ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>> GetEnkelvoudigInformatieObjectByUrlAsync(string enkelvoudigInformatieObjectUrl)
    {
        if (_cachedEnkelvoudigInformatieObjectUrl.TryGetValue(enkelvoudigInformatieObjectUrl, out var cachedEnkelvoudigInformatieObject))
        {
            return cachedEnkelvoudigInformatieObject;
        }

        var result = await _agent.GetEnkelvoudigInformatieObjectByUrlAsync(enkelvoudigInformatieObjectUrl);
        if (result.Success)
        {
            // Note: Only cache successful responses, so a transient failure isn't replayed for the rest of the scope
            _cachedEnkelvoudigInformatieObjectUrl[enkelvoudigInformatieObjectUrl] = result;
        }

        return result;
    }

    public Task<ServiceAgentResponse<(EnkelvoudigInformatieObjectResponseDto enkelvoudigInformatieObject, object expandedEnkelvoudigInformatieObject)>> GetEnkelvoudigInformatieObjectByUrlAsync(string enkelvoudigInformatieObjectUrl, string expand)
    {
        // Note: Pass through agent (so no cache)
        return _agent.GetEnkelvoudigInformatieObjectByUrlAsync(enkelvoudigInformatieObjectUrl, expand);
    }

    public Task<ServiceAgentResponse<IEnumerable<Contracts.v1.Responses.ObjectInformatieObjectResponseDto>>> GetObjectInformatieObjectenAsync(GetAllObjectInformatieObjectenQueryParameters parameters)
    {
        // Note: Pass through agent (so no cache)
        return _agent.GetObjectInformatieObjectenAsync(parameters);
    }
}
