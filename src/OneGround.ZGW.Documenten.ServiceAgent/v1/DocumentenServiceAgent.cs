using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Services;
using OneGround.ZGW.Documenten.Contracts.v1.Requests;
using OneGround.ZGW.Documenten.Contracts.v1.Responses;

namespace OneGround.ZGW.Documenten.ServiceAgent.v1;

public class DocumentenServiceAgent : ZGWServiceAgent<DocumentenServiceAgent>, IDocumentenServiceAgent
{
    public DocumentenServiceAgent(
        ILogger<DocumentenServiceAgent> logger,
        HttpClient client,
        IServiceDiscovery serviceDiscovery,
        IServiceAgentResponseBuilder responseBuilder,
        IConfiguration configuration
    )
        : base(client, logger, serviceDiscovery, configuration, responseBuilder, ServiceRoleName.DRC) { }

    public Task<ServiceAgentResponse<EnkelvoudigInformatieObjectCreateResponseDto>> AddEnkelvoudigInformatieObjectAsync(
        EnkelvoudigInformatieObjectCreateRequestDto enkelvoudigInformatieObject
    )
    {
        Logger.LogDebug("Adding ObjectInformatieObject....");

        var url = new Uri("/enkelvoudiginformatieobjecten", UriKind.Relative);

        return PostAsync<EnkelvoudigInformatieObjectCreateRequestDto, EnkelvoudigInformatieObjectCreateResponseDto>(url, enkelvoudigInformatieObject);
    }

    public async Task<
        ServiceAgentResponse<IEnumerable<ObjectInformatieObjectResponseDto>>
    > GetObjectInformatieObjectsByInformatieObjectAndObjectAsync(string informatieObject, string @object)
    {
        var url = new Uri("/objectinformatieobjecten", UriKind.Relative)
            .AddQueryParameter("page", 1)
            .AddQueryParameter("informatieObject", informatieObject)
            .AddQueryParameter("object", @object);

        Logger.LogDebug("Query ObjectInformatieObject {url}....", url);

        return await GetAsync<IEnumerable<ObjectInformatieObjectResponseDto>>(url);
    }

    public Task<ServiceAgentResponse<ObjectInformatieObjectResponseDto>> AddObjectInformatieObjectAsync(
        ObjectInformatieObjectRequestDto objectInformatieObject
    )
    {
        ArgumentNullException.ThrowIfNull(objectInformatieObject);

        Logger.LogDebug("Adding ObjectInformatieObject....");

        var url = new Uri("/objectinformatieobjecten", UriKind.Relative);

        return PostAsync<ObjectInformatieObjectRequestDto, ObjectInformatieObjectResponseDto>(url, objectInformatieObject);
    }

    public async Task<ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>> GetEnkelvoudigInformatieObjectByUrlAsync(
        string enkelvoudigInformatieObjectUrl
    )
    {
        if (!EnsureValidResource(ServiceRoleName.DRC, enkelvoudigInformatieObjectUrl, "enkelvoudiginformatieobjecten", out var errorResponse))
            return new ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>(errorResponse);

        Logger.LogDebug("Query EnkelvoudigInformatieObject {enkelvoudigInformatieObjectUrl}....", enkelvoudigInformatieObjectUrl);

        return await GetAsync<EnkelvoudigInformatieObjectResponseDto>(new Uri(enkelvoudigInformatieObjectUrl));
    }

    public async Task<ServiceAgentResponse> DeleteEnkelvoudigInformatieObjectByUrlAsync(string enkelvoudigInformatieObjectUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.DRC, enkelvoudigInformatieObjectUrl, "enkelvoudiginformatieobjecten", out var errorResponse))
            return new ServiceAgentResponse(errorResponse);

        Logger.LogDebug("Deleting EnkelvoudigInformatieObject {enkelvoudigInformatieObjectUrl}....", enkelvoudigInformatieObjectUrl);

        return await DeleteAsync(new Uri(enkelvoudigInformatieObjectUrl));
    }

    public async Task<ServiceAgentResponse> DeleteObjectInformatieObjectByUrlAsync(string objectInformatieObjectUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.DRC, objectInformatieObjectUrl, "objectinformatieobjecten", out var errorResponse))
            return new ServiceAgentResponse(errorResponse);

        Logger.LogDebug("Deleting ObjectInformatieObject '{objectInformatieObjectUrl}'....", objectInformatieObjectUrl);

        return await DeleteAsync(new Uri(objectInformatieObjectUrl));
    }

    public async Task<ServiceAgentResponse<Stream>> DownloadEnkelvoudigInformatieObjectByUrlAsync(
        string enkelvoudigInformatieObjectUrl,
        int? version = null
    )
    {
        if (!EnsureValidResource(ServiceRoleName.DRC, enkelvoudigInformatieObjectUrl, "enkelvoudiginformatieobjecten", out var errorResponse))
            return new ServiceAgentResponse<Stream>(errorResponse);

        var downloadEnkelvoudigInformatieObjectUrl = enkelvoudigInformatieObjectUrl + "/download";
        if (version.HasValue)
        {
            downloadEnkelvoudigInformatieObjectUrl += $"?version={version}";
        }

        Logger.LogDebug("Downloading EnkelvoudigInformatieObject {objectInformatieObjectUrl}....", downloadEnkelvoudigInformatieObjectUrl);

        var content = await GetStreamAsync(new Uri(downloadEnkelvoudigInformatieObjectUrl));

        return new ServiceAgentResponse<Stream>(content);
    }

    public async Task<ServiceAgentResponse<IEnumerable<AuditTrailRegelDto>>> GetAuditTrailRegelsAsync(string enkelvoudigInformatieObjectUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.DRC, enkelvoudigInformatieObjectUrl, "enkelvoudiginformatieobjecten", out var errorResponse))
            return new ServiceAgentResponse<IEnumerable<AuditTrailRegelDto>>(errorResponse);

        var url = new Uri($"{enkelvoudigInformatieObjectUrl.TrimEnd('/')}/audittrail", UriKind.Absolute);

        Logger.LogDebug("Query EnkelvoudigInformatieObject audittrail {enkelvoudigInformatieObjectUrl}....", enkelvoudigInformatieObjectUrl);

        return await GetAsync<IEnumerable<AuditTrailRegelDto>>(url);
    }
}
