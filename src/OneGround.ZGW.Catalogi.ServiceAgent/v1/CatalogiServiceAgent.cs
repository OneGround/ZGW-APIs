using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1.Queries;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Services;

namespace OneGround.ZGW.Catalogi.ServiceAgent.v1;

public class CatalogiServiceAgent : ZGWServiceAgent<CatalogiServiceAgent>, ICatalogiServiceAgent
{
    public CatalogiServiceAgent(
        ILogger<CatalogiServiceAgent> logger,
        HttpClient client,
        IServiceDiscovery serviceDiscovery,
        IServiceAgentResponseBuilder responseBuilder,
        IConfiguration configuration
    )
        : base(client, logger, serviceDiscovery, configuration, responseBuilder, ServiceRoleName.ZTC) { }

    public async Task<ServiceAgentResponse<CatalogusResponseDto>> GetCatalogusAsync(string catalogusUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZTC, catalogusUrl, "catalogussen", out var errorResponse))
            return new ServiceAgentResponse<CatalogusResponseDto>(errorResponse);

        Logger.LogDebug("Catalogus bevragen op '{catalogusUrl}'....", catalogusUrl);

        return await GetAsync<CatalogusResponseDto>(new Uri(catalogusUrl));
    }

    public Task<ServiceAgentResponse<PagedResponse<ZaakTypeResponseDto>>> GetZaakTypenAsync(GetAllZaakTypenQueryParameters parameters, int page = 1)
    {
        return GetPagedResponseAsync<ZaakTypeResponseDto>("/zaaktypen", parameters, page);
    }

    public async Task<ServiceAgentResponse<ZaakTypeResponseDto>> GetZaakTypeByUrlAsync(string zaakTypeUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZTC, zaakTypeUrl, "zaaktypen", out var errorResponse))
            return new ServiceAgentResponse<ZaakTypeResponseDto>(errorResponse);

        Logger.LogDebug("ZaakType bevragen op '{zaakTypeUrl}'....", zaakTypeUrl);

        return await GetAsync<ZaakTypeResponseDto>(new Uri(zaakTypeUrl));
    }

    public async Task<ServiceAgentResponse<RolTypeResponseDto>> GetRolTypeByUrlAsync(string rolTypeUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZTC, rolTypeUrl, "roltypen", out var errorResponse))
            return new ServiceAgentResponse<RolTypeResponseDto>(errorResponse);

        Logger.LogDebug("RolType bevragen op '{rolTypeUrl}'....", rolTypeUrl);

        return await GetAsync<RolTypeResponseDto>(new Uri(rolTypeUrl));
    }

    public async Task<ServiceAgentResponse<BesluitTypeResponseDto>> GetBesluitTypeByUrlAsync(string besluitTypeUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZTC, besluitTypeUrl, "besluittypen", out var errorResponse))
            return new ServiceAgentResponse<BesluitTypeResponseDto>(errorResponse);

        Logger.LogDebug("BesluitType bevragen op '{besluitTypeUrl}'....", besluitTypeUrl);

        return await GetAsync<BesluitTypeResponseDto>(new Uri(besluitTypeUrl));
    }

    public async Task<ServiceAgentResponse<EigenschapResponseDto>> GetEigenschapByUrlAsync(string eigenschapUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZTC, eigenschapUrl, "eigenschappen", out var errorResponse))
            return new ServiceAgentResponse<EigenschapResponseDto>(errorResponse);

        Logger.LogDebug("Eigenschap bevragen op '{eigenschapUrl}'....", eigenschapUrl);

        return await GetAsync<EigenschapResponseDto>(new Uri(eigenschapUrl));
    }

    public async Task<ServiceAgentResponse<ResultaatTypeResponseDto>> GetResultaatTypeByUrlAsync(string resultaatTypeUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZTC, resultaatTypeUrl, "resultaattypen", out var errorResponse))
            return new ServiceAgentResponse<ResultaatTypeResponseDto>(errorResponse);

        Logger.LogDebug("Resultaattypen bevragen op '{resultaatTypeUrl}'....", resultaatTypeUrl);

        return await GetAsync<ResultaatTypeResponseDto>(new Uri(resultaatTypeUrl));
    }

    public async Task<ServiceAgentResponse<InformatieObjectTypeResponseDto>> GetInformatieObjectTypeByUrlAsync(string informatieObjectTypeUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZTC, informatieObjectTypeUrl, "informatieobjecttypen", out var errorResponse))
            return new ServiceAgentResponse<InformatieObjectTypeResponseDto>(errorResponse);

        Logger.LogDebug("InformatieObjectType bevragen op '{informatieObjectTypeUrl}'....", informatieObjectTypeUrl);

        return await GetAsync<InformatieObjectTypeResponseDto>(new Uri(informatieObjectTypeUrl));
    }

    public async Task<ServiceAgentResponse<StatusTypeResponseDto>> GetStatusTypeByUrlAsync(string statusTypeUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZTC, statusTypeUrl, "statustypen", out var errorResponse))
            return new ServiceAgentResponse<StatusTypeResponseDto>(errorResponse);

        Logger.LogDebug("StatusType bevragen op '{statusTypeUrl}'....", statusTypeUrl);

        return await GetAsync<StatusTypeResponseDto>(new Uri(statusTypeUrl));
    }

    public Task<ServiceAgentResponse<PagedResponse<RolTypeResponseDto>>> GetRolTypenAsync(GetAllRolTypenQueryParameters parameters, int page = 1)
    {
        return GetPagedResponseAsync<RolTypeResponseDto>("/roltypen", parameters, page);
    }

    public Task<ServiceAgentResponse<PagedResponse<ZaakTypeInformatieObjectTypeResponseDto>>> GetZaakTypeInformatieObjectTypenAsync(
        GetAllZaakTypeInformatieObjectTypenQueryParameters parameters,
        int page = 1
    )
    {
        return GetPagedResponseAsync<ZaakTypeInformatieObjectTypeResponseDto>("/zaaktype-informatieobjecttypen", parameters, page);
    }
}
