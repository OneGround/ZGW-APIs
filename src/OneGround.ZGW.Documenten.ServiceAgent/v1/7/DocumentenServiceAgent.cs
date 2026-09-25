using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Services;
using OneGround.ZGW.Documenten.Contracts.v1._7.Queries;
using OneGround.ZGW.Documenten.Contracts.v1._7.Requests;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Documenten.ServiceAgent.v1._7;

public class DocumentenServiceAgent : ZGWServiceAgent<DocumentenServiceAgent>, IDocumentenServiceAgent
{
    public DocumentenServiceAgent(
        ILogger<DocumentenServiceAgent> logger,
        HttpClient client,
        IServiceDiscovery serviceDiscovery,
        IServiceAgentResponseBuilder responseBuilder,
        IConfiguration configuration
    )
        : base(client, logger, serviceDiscovery, configuration, responseBuilder, ServiceRoleName.DRC, "v1")
    {
        Client.DefaultRequestHeaders.Add("Api-Version", "1.7");
    }

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

    public async Task<ServiceAgentResponse<IEnumerable<ObjectInformatieObjectResponseDto>>> GetObjectInformatieObjectenAsync(
        GetAllObjectInformatieObjectenQueryParameters parameters
    )
    {
        return await GetAsync<ObjectInformatieObjectResponseDto>("/objectinformatieobjecten", parameters);
    }

    public Task<ServiceAgentResponse<ObjectInformatieObjectResponseDto>> AddObjectInformatieObjectAsync(
        Contracts.v1.Requests.ObjectInformatieObjectRequestDto objectInformatieObject
    )
    {
        ArgumentNullException.ThrowIfNull(objectInformatieObject);

        Logger.LogDebug("Adding ObjectInformatieObject....");

        var url = new Uri("/objectinformatieobjecten", UriKind.Relative);

        return PostAsync<Contracts.v1.Requests.ObjectInformatieObjectRequestDto, ObjectInformatieObjectResponseDto>(url, objectInformatieObject);
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

    public async Task<
        ServiceAgentResponse<(EnkelvoudigInformatieObjectResponseDto enkelvoudigInformatieObject, object expandedEnkelvoudigInformatieObject)>
    > GetEnkelvoudigInformatieObjectByUrlAsync(string enkelvoudigInformatieObjectUrl, string expand)
    {
        if (!EnsureValidResource(ServiceRoleName.DRC, enkelvoudigInformatieObjectUrl, "enkelvoudiginformatieobjecten", out var errorResponse))
            return new ServiceAgentResponse<(
                EnkelvoudigInformatieObjectResponseDto enkelvoudigInformatieObject,
                object expandedEnkelvoudigInformatieObject
            )>(errorResponse);

        Logger.LogDebug("EnkelvoudigInformatieObject bevragen op '{enkelvoudigInformatieObjectUrl}'....", enkelvoudigInformatieObjectUrl);

        var url = new Uri(enkelvoudigInformatieObjectUrl);

        if (!string.IsNullOrEmpty(expand))
        {
            var result = await GetAsync<object>(url.AddQueryParameter("expand", expand));
            if (!result.Success)
            {
                return new ServiceAgentResponse<(EnkelvoudigInformatieObjectResponseDto, object)>(result.Error, null);
            }
            var enkelvoudiginformatieobjectBase = JsonConvert.DeserializeObject<EnkelvoudigInformatieObjectResponseDto>(result.Response.ToString());

            return new ServiceAgentResponse<(
                EnkelvoudigInformatieObjectResponseDto enkelvoudigInformatieObject,
                object expandedEnkelvoudigInformatieObject
            )>((enkelvoudiginformatieobjectBase, result.Response));
        }
        else
        {
            var result = await GetAsync<EnkelvoudigInformatieObjectResponseDto>(url);
            if (!result.Success)
            {
                return new ServiceAgentResponse<(EnkelvoudigInformatieObjectResponseDto, object)>(result.Error, null);
            }
            return new ServiceAgentResponse<(
                EnkelvoudigInformatieObjectResponseDto enkelvoudigInformatieObject,
                object expandedEnkelvoudigInformatieObject
            )>((result.Response, result.Response));
        }
    }

    public Task<ServiceAgentResponse<EnkelvoudigInformatieObjectResponseDto>> GetEnkelvoudigInformatieObjectAsync(Guid enkelvoudigInformatieObjectId)
    {
        if (enkelvoudigInformatieObjectId == Guid.Empty)
            throw new ArgumentNullException(nameof(enkelvoudigInformatieObjectId));

        Logger.LogDebug("Getting document by id: {enkelvoudigInformatieObjectId}", enkelvoudigInformatieObjectId);

        var url = new Uri($"/enkelvoudiginformatieobjecten/{enkelvoudigInformatieObjectId}", UriKind.Relative);

        return GetAsync<EnkelvoudigInformatieObjectResponseDto>(url);
    }

    public async Task<ServiceAgentResponse<Contracts.v1._1.Responses.BestandsDeelResponseDto>> AddBestandsdeelAsync(
        string bestandsdeelUrl,
        MultipartFormDataContent multipartFormDataContent
    )
    {
        return await PutAsync<Contracts.v1._1.Responses.BestandsDeelResponseDto>(new Uri(bestandsdeelUrl), multipartFormDataContent);
    }

    public async Task<ServiceAgentResponse> UnlockAsync(string enkelvoudigInformatieObjectUrl)
    {
        var unlockEnkelvoudigInformatieObjectUrl = enkelvoudigInformatieObjectUrl + "/unlock";

        return await PostAsync(new Uri(unlockEnkelvoudigInformatieObjectUrl));
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
