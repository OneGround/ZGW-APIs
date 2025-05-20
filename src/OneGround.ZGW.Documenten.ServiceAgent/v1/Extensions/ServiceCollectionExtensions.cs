using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.ServiceAgent.Extensions;
using OneGround.ZGW.Documenten.Contracts.v1.Requests;
using OneGround.ZGW.Documenten.Contracts.v1.Responses;

namespace OneGround.ZGW.Documenten.ServiceAgent.v1.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddDocumentenServiceAgent(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceAgent<IDocumentenServiceAgent, DocumentenServiceAgent>(ServiceRoleName.DRC, configuration);
        services.AddScoped<ICachedDocumentenServiceAgent, CachedDocumentServiceAgent>();
    }
}

public interface ICachedDocumentenServiceAgent : IDocumentenServiceAgent { }

class CachedDocumentServiceAgent : ICachedDocumentenServiceAgent
{
    private readonly IDocumentenServiceAgent _agent;

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
        ObjectInformatieObjectRequestDto objectInformatieObject
    )
    {
        // Note: Pass through agent (so no cache)
        return _agent.AddObjectInformatieObjectAsync(objectInformatieObject);
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
        _cachedEnkelvoudigInformatieObjectUrl[enkelvoudigInformatieObjectUrl] = await _agent.GetEnkelvoudigInformatieObjectByUrlAsync(
            enkelvoudigInformatieObjectUrl
        );

        return _cachedEnkelvoudigInformatieObjectUrl[enkelvoudigInformatieObjectUrl];
    }

    private readonly Dictionary<string, ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>> _cachedEnkelvoudigInformatieObjectUrl = [];

    public Task<ServiceAgentResponse<IEnumerable<ObjectInformatieObjectResponseDto>>> GetObjectInformatieObjectsByInformatieObjectAndObjectAsync(
        string informatieObject,
        string @object
    )
    {
        // Note: Pass through agent for now (so no cache yet due to too many memory resources kept in cache; or limit amount of cache-entries)
        return _agent.GetObjectInformatieObjectsByInformatieObjectAndObjectAsync(informatieObject, @object);
    }
}
