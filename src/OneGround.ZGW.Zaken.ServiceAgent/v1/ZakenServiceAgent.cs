using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Services;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using V10 = OneGround.ZGW.Zaken.Contracts.v1;
using V15 = OneGround.ZGW.Zaken.Contracts.v1._5;

namespace OneGround.ZGW.Zaken.ServiceAgent.v1;

public class ZakenServiceAgent : ZGWServiceAgent<ZakenServiceAgent>, IZakenServiceAgent
{
    public ZakenServiceAgent(
        ILogger<ZakenServiceAgent> logger,
        HttpClient client,
        IServiceDiscovery serviceDiscovery,
        IServiceAgentResponseBuilder responseBuilder,
        IConfiguration configuration
    )
        : base(client, logger, serviceDiscovery, configuration, responseBuilder, ServiceRoleName.ZRC) { }

    public async Task<ServiceAgentResponse<ZaakResponseDto>> AddZaakAsync(V15.Requests.ZaakRequestDto request)
    {
        var url = new Uri("/zaken", UriKind.Relative);

        return await PostAsync<V15.Requests.ZaakRequestDto, ZaakResponseDto>(url, request, ("Accept-Crs", "EPSG:4326"), ("Content-Crs", "EPSG:4326"));
    }

    public async Task<ServiceAgentResponse<V10.Responses.ZaakBesluitResponseDto>> AddZaakBesluitByUrlAsync(Guid zaakId, string besluitUrl)
    {
        var url = new Uri($"/zaken/{zaakId}/besluiten", UriKind.Relative);

        Logger.LogDebug("Besluiten bevragen op '{url}'....", url);

        var request = new V10.Requests.ZaakBesluitRequestDto { Besluit = besluitUrl };

        return await PostAsync<V10.Requests.ZaakBesluitRequestDto, V10.Responses.ZaakBesluitResponseDto>(url, request);
    }

    public async Task<ServiceAgentResponse<V15.Responses.ZaakInformatieObjectResponseDto>> AddZaakInformatieObjectAsync(
        V15.Requests.ZaakInformatieObjectRequestDto request
    )
    {
        var url = new Uri("/zaakinformatieobjecten", UriKind.Relative);

        return await PostAsync<V15.Requests.ZaakInformatieObjectRequestDto, V15.Responses.ZaakInformatieObjectResponseDto>(url, request);
    }

    public async Task<ServiceAgentResponse> DeleteZaakBesluitByUrlAsync(string zaakBesluitUrl)
    {
        // Note: Sample of a zaakBesluitUri is https://zaken.user.local:5005/api/v1/zaken/bb9a0a7a-458a-4b55-9983-87ebeb5f9742/besluiten/5c3084ae-196d-44dd-8c37-e5a30fce8e0f
        if (!EnsureValidResource(ServiceRoleName.ZRC, zaakBesluitUrl, "zaken", out var errorResponse))
            return new ServiceAgentResponse(errorResponse);

        return await DeleteAsync(new Uri(zaakBesluitUrl));
    }

    public async Task<ServiceAgentResponse<ZaakResponseDto>> GetZaakByUrlAsync(string zaakUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZRC, zaakUrl, "zaken", out var errorResponse))
            return new ServiceAgentResponse<ZaakResponseDto>(errorResponse);

        return await GetAsync<ZaakResponseDto>(new Uri(zaakUrl), ("Accept-Crs", "EPSG:4326"));
    }

    public async Task<ServiceAgentResponse<IEnumerable<ZaakInformatieObjectResponseDto>>> GetZaakInformatieObjectenAsync(
        GetAllZaakInformatieObjectenQueryParameters parameters
    )
    {
        return await GetAsync<ZaakInformatieObjectResponseDto>("/zaakinformatieobjecten", parameters);
    }

    public async Task<ServiceAgentResponse> DeleteZaakInformatieObjectByIdAsync(Guid zaakInformatieObjectId)
    {
        var url = new Uri($"/zaakinformatieobjecten/{zaakInformatieObjectId}", UriKind.Relative);
        return await DeleteAsync(url);
    }

    public async Task<ServiceAgentResponse<IDictionary<string, object>>> GetZaakObjectByUrlAsync(string zaakObjectUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZRC, zaakObjectUrl, "zaakobjecten", out var errorResponse))
            return new ServiceAgentResponse<IDictionary<string, object>>(errorResponse);

        return await GetAsync<IDictionary<string, object>>(new Uri(zaakObjectUrl));
    }

    public async Task<ServiceAgentResponse<PagedResponse<JObjectZaakObjectResponseDto>>> GetZaakObjectenAsync(
        GetAllZaakObjectenQueryParameters queryParameters,
        int page = 1
    )
    {
        return await GetPagedResponseAsync<JObjectZaakObjectResponseDto>("/zaakobjecten", queryParameters, page);
    }

    public async Task<ServiceAgentResponse<StatusResponseDto>> GetZaakStatusByUrlAsync(string zaakStatusUri)
    {
        if (!EnsureValidResource(ServiceRoleName.ZRC, zaakStatusUri, "statussen", out var errorResponse))
            return new ServiceAgentResponse<StatusResponseDto>(errorResponse);

        return await GetAsync<StatusResponseDto>(new Uri(zaakStatusUri));
    }

    public async Task<ServiceAgentResponse<ResultaatResponseDto>> GetZaakResultaatByUrlAsync(string zaakResultaatUri)
    {
        if (!EnsureValidResource(ServiceRoleName.ZRC, zaakResultaatUri, "resultaten", out var errorResponse))
            return new ServiceAgentResponse<ResultaatResponseDto>(errorResponse);

        return await GetAsync<ResultaatResponseDto>(new Uri(zaakResultaatUri));
    }

    public async Task<ServiceAgentResponse<ZaakResponseDto>> PatchZaakByUrlAsync(string zaakUrl, JObject zaakPatchRequest)
    {
        if (!EnsureValidResource(ServiceRoleName.ZRC, zaakUrl, "zaken", out var errorResponse))
            return new ServiceAgentResponse<ZaakResponseDto>(errorResponse);

        Logger.LogDebug("Patch zaak '{Url}'....", zaakUrl);

        return await PatchAsync<ZaakResponseDto>(new Uri(zaakUrl), zaakPatchRequest, ("Accept-Crs", "EPSG:4326"), ("Content-Crs", "EPSG:4326"));
    }

    public async Task<ServiceAgentResponse<PagedResponse<JObjectZaakRolResponseDto>>> GetZaakRollenAsync(
        GetAllZaakRollenQueryParameters queryParameters,
        int page = 1
    )
    {
        return await GetPagedResponseAsync<JObjectZaakRolResponseDto>("/rollen", queryParameters, page);
    }

    public async Task<ServiceAgentResponse<IEnumerable<AuditTrailRegelDto>>> GetAuditTrailRegelsAsync(string zaakUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZRC, zaakUrl, "zaken", out var errorResponse))
            return new ServiceAgentResponse<IEnumerable<AuditTrailRegelDto>>(errorResponse);

        var url = new Uri($"{zaakUrl.TrimEnd('/')}/audittrail", UriKind.Absolute);

        return await GetAsync<IEnumerable<AuditTrailRegelDto>>(url);
    }

    public async Task<ServiceAgentResponse<V15.Responses.ZaakRol.ZaakRolResponseDto>> AddZaakRolAsync(V15.Requests.ZaakRol.ZaakRolRequestDto request)
    {
        var url = new Uri("/rollen", UriKind.Relative);

        return await PostAsync<V15.Requests.ZaakRol.ZaakRolRequestDto, V15.Responses.ZaakRol.ZaakRolResponseDto>(url, request);
    }
}
