using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Documenten.Contracts.v1._7.Queries;
using OneGround.ZGW.Documenten.Contracts.v1._7.Requests;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Documenten.ServiceAgent.v1._7;

class CachedDocumentServiceAgent : ICachedDocumentenServiceAgent
{
    private readonly IDocumentenServiceAgent _agent;
    private readonly ConcurrentDictionary<
        string,
        ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>
    > _cachedEnkelvoudigInformatieObjectUrl = new();

    public CachedDocumentServiceAgent(IDocumentenServiceAgent agent)
    {
        _agent = agent;
    }

    public Task<ServiceAgentResponse<EnkelvoudigInformatieObjectCreateResponseDto>> AddEnkelvoudigInformatieObjectAsync(
        EnkelvoudigInformatieObjectCreateRequestDto enkelvoudigInformatieObject
    )
    {
        // Note: Pass through agent (so no cache)
        return _agent.AddEnkelvoudigInformatieObjectAsync(enkelvoudigInformatieObject);
    }

    public Task<ServiceAgentResponse<ObjectInformatieObjectResponseDto>> AddObjectInformatieObjectAsync(
        Contracts.v1.Requests.ObjectInformatieObjectRequestDto objectInformatieObject
    )
    {
        // Note: Pass through agent (so no cache)
        return _agent.AddObjectInformatieObjectAsync(objectInformatieObject);
    }

    public Task<ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>> GetEnkelvoudigInformatieObjectAsync(Guid enkelvoudigInformatieObjectId)
    {
        // Note: Pass through agent (so no cache)
        return _agent.GetEnkelvoudigInformatieObjectAsync(enkelvoudigInformatieObjectId);
    }

    public Task<ServiceAgentResponse<Contracts.v1._1.Responses.BestandsDeelResponseDto>> AddBestandsdeelAsync(
        string bestandsdeelUrl,
        MultipartFormDataContent multipartFormDataContent
    )
    {
        // Note: Pass through agent (so no cache)
        return _agent.AddBestandsdeelAsync(bestandsdeelUrl, multipartFormDataContent);
    }

    public Task<ServiceAgentResponse> UnlockAsync(string enkelvoudigInformatieObjectUrl)
    {
        // Note: Pass through agent (so no cache)
        return _agent.UnlockAsync(enkelvoudigInformatieObjectUrl);
    }

    public Task<ServiceAgentResponse> DeleteEnkelvoudigInformatieObjectByUrlAsync(string enkelvoudigInformatieObjectUrl)
    {
        // Note: Pass through agent (so no cache)
        return _agent.DeleteEnkelvoudigInformatieObjectByUrlAsync(enkelvoudigInformatieObjectUrl);
    }

    public Task<ServiceAgentResponse> DeleteObjectInformatieObjectByUrlAsync(string objectInformatieObjectUrl)
    {
        // Note: Pass through agent (so no cache)
        return _agent.DeleteObjectInformatieObjectByUrlAsync(objectInformatieObjectUrl);
    }

    public Task<ServiceAgentResponse<Stream>> DownloadEnkelvoudigInformatieObjectByUrlAsync(
        string enkelvoudigInformatieObjectUrl,
        int? version = null
    )
    {
        // Note: Pass through agent (so no cache)
        return _agent.DownloadEnkelvoudigInformatieObjectByUrlAsync(enkelvoudigInformatieObjectUrl, version);
    }

    public Task<ServiceAgentResponse<IEnumerable<AuditTrailRegelDto>>> GetAuditTrailRegelsAsync(string enkelvoudigInformatieObjectUrl)
    {
        // Note: Pass through agent for now (so no cache yet due to too many memory resources kept in cache; or limit amount of cache-entries)
        return _agent.GetAuditTrailRegelsAsync(enkelvoudigInformatieObjectUrl);
    }

    public async Task<ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>> GetEnkelvoudigInformatieObjectByUrlAsync(
        string enkelvoudigInformatieObjectUrl
    )
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

    public Task<
        ServiceAgentResponse<(EnkelvoudigInformatieObjectResponseDto enkelvoudigInformatieObject, object expandedEnkelvoudigInformatieObject)>
    > GetEnkelvoudigInformatieObjectByUrlAsync(string enkelvoudigInformatieObjectUrl, string expand)
    {
        // Note: Pass through agent (so no cache)
        return _agent.GetEnkelvoudigInformatieObjectByUrlAsync(enkelvoudigInformatieObjectUrl, expand);
    }

    public Task<ServiceAgentResponse<IEnumerable<ObjectInformatieObjectResponseDto>>> GetObjectInformatieObjectsByInformatieObjectAndObjectAsync(
        string informatieObject,
        string @object
    )
    {
        // Note: Pass through agent for now (so no cache yet due to too many memory resources kept in cache; or limit amount of cache-entries)
        return _agent.GetObjectInformatieObjectsByInformatieObjectAndObjectAsync(informatieObject, @object);
    }

    public Task<ServiceAgentResponse<IEnumerable<ObjectInformatieObjectResponseDto>>> GetObjectInformatieObjectenAsync(
        GetAllObjectInformatieObjectenQueryParameters parameters
    )
    {
        // Note: Pass through agent (so no cache)
        return _agent.GetObjectInformatieObjectenAsync(parameters);
    }
}
