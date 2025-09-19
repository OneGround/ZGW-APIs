using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Queries;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Requests;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Services;

namespace OneGround.ZGW.Catalogi.ServiceAgent.v1._3;

public class CatalogiServiceAgent : ZGWServiceAgent<CatalogiServiceAgent>, ICatalogiServiceAgent
{
    public CatalogiServiceAgent(
        ILogger<CatalogiServiceAgent> logger,
        HttpClient client,
        IServiceDiscovery serviceDiscovery,
        IServiceAgentResponseBuilder responseBuilder,
        IConfiguration configuration
    )
        : base(client, logger, serviceDiscovery, configuration, responseBuilder, ServiceRoleName.ZTC, "v1")
    {
        Client.DefaultRequestHeaders.Add("Api-Version", "1.3");
    }

    public Task<ServiceAgentResponse<CatalogusResponseDto>> GetCatalogusAsync(Guid catalogusId)
    {
        if (catalogusId == Guid.Empty)
            throw new ArgumentNullException(nameof(catalogusId));

        Logger.LogDebug("Getting catalog by id: {catalogusId}", catalogusId);

        var url = new Uri($"/catalogussen/{catalogusId}", UriKind.Relative);

        return GetAsync<CatalogusResponseDto>(url);
    }

    public Task<ServiceAgentResponse<CatalogusResponseDto>> GetCatalogusAsync(string catalogusUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZTC, catalogusUrl, "catalogussen", out var errorResponse))
            return Task.FromResult(new ServiceAgentResponse<CatalogusResponseDto>(errorResponse));

        Logger.LogDebug("Catalogus bevragen op '{catalogusUrl}'....", catalogusUrl);

        return GetAsync<CatalogusResponseDto>(new Uri(catalogusUrl));
    }

    public Task<ServiceAgentResponse<PagedResponse<CatalogusResponseDto>>> GetCatalogussenAsync(
        Contracts.v1.Queries.GetAllCatalogussenQueryParameters parameters,
        int page = 1
    )
    {
        return GetPagedResponseAsync<CatalogusResponseDto>("/catalogussen", parameters, page);
    }

    public async Task<ServiceAgentResponse<CatalogusResponseDto>> AddCatalogusAsync(CatalogusRequestDto request)
    {
        var url = new Uri("/catalogussen", UriKind.Relative);

        return await PostAsync<CatalogusRequestDto, CatalogusResponseDto>(url, request);
    }

    public Task<ServiceAgentResponse<PagedResponse<ZaakTypeResponseDto>>> GetZaakTypenAsync(
        Contracts.v1.Queries.GetAllZaakTypenQueryParameters parameters,
        int page = 1
    )
    {
        return GetPagedResponseAsync<ZaakTypeResponseDto>("/zaaktypen", parameters, page);
    }

    public Task<ServiceAgentResponse<ZaakTypeResponseDto>> GetZaakTypeByUrlAsync(string zaakTypeUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZTC, zaakTypeUrl, "zaaktypen", out var errorResponse))
            return Task.FromResult(new ServiceAgentResponse<ZaakTypeResponseDto>(errorResponse));

        Logger.LogDebug("ZaakType bevragen op '{zaakTypeUrl}'....", zaakTypeUrl);

        return GetAsync<ZaakTypeResponseDto>(new Uri(zaakTypeUrl));
    }

    public Task<ServiceAgentResponse<RolTypeResponseDto>> GetRolTypeByUrlAsync(string rolTypeUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZTC, rolTypeUrl, "roltypen", out var errorResponse))
            return Task.FromResult(new ServiceAgentResponse<RolTypeResponseDto>(errorResponse));

        Logger.LogDebug("RolType bevragen op '{rolTypeUrl}'....", rolTypeUrl);

        return GetAsync<RolTypeResponseDto>(new Uri(rolTypeUrl));
    }

    public Task<ServiceAgentResponse<BesluitTypeResponseDto>> GetBesluitTypeByUrlAsync(string besluitTypeUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZTC, besluitTypeUrl, "besluittypen", out var errorResponse))
            return Task.FromResult(new ServiceAgentResponse<BesluitTypeResponseDto>(errorResponse));

        Logger.LogDebug("BesluitType bevragen op '{besluitTypeUrl}'....", besluitTypeUrl);

        return GetAsync<BesluitTypeResponseDto>(new Uri(besluitTypeUrl));
    }

    public Task<ServiceAgentResponse<EigenschapResponseDto>> GetEigenschapByUrlAsync(string eigenschapUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZTC, eigenschapUrl, "eigenschappen", out var errorResponse))
            return Task.FromResult(new ServiceAgentResponse<EigenschapResponseDto>(errorResponse));

        Logger.LogDebug("Eigenschap bevragen op '{eigenschapUrl}'....", eigenschapUrl);

        return GetAsync<EigenschapResponseDto>(new Uri(eigenschapUrl));
    }

    public Task<ServiceAgentResponse<ResultaatTypeResponseDto>> GetResultaatTypeByUrlAsync(string resultaatTypeUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZTC, resultaatTypeUrl, "resultaattypen", out var errorResponse))
            return Task.FromResult(new ServiceAgentResponse<ResultaatTypeResponseDto>(errorResponse));

        Logger.LogDebug("Resultaattypen bevragen op '{resultaatTypeUrl}'....", resultaatTypeUrl);

        return GetAsync<ResultaatTypeResponseDto>(new Uri(resultaatTypeUrl));
    }

    public Task<ServiceAgentResponse<InformatieObjectTypeResponseDto>> GetInformatieObjectTypeByUrlAsync(string informatieObjectTypeUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZTC, informatieObjectTypeUrl, "informatieobjecttypen", out var errorResponse))
            return Task.FromResult(new ServiceAgentResponse<InformatieObjectTypeResponseDto>(errorResponse));

        Logger.LogDebug("InformatieObjectType bevragen op '{informatieObjectTypeUrl}'....", informatieObjectTypeUrl);

        return GetAsync<InformatieObjectTypeResponseDto>(new Uri(informatieObjectTypeUrl));
    }

    public Task<ServiceAgentResponse<PagedResponse<InformatieObjectTypeResponseDto>>> GetInformatieObjectTypenAsync(
        Contracts.v1._2.Queries.GetAllInformatieObjectTypenQueryParameters parameters,
        int page = 1
    )
    {
        return GetPagedResponseAsync<InformatieObjectTypeResponseDto>("/informatieobjecttypen", parameters, page);
    }

    public Task<ServiceAgentResponse<StatusTypeResponseDto>> GetStatusTypeByUrlAsync(string statusTypeUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZTC, statusTypeUrl, "statustypen", out var errorResponse))
            return Task.FromResult(new ServiceAgentResponse<StatusTypeResponseDto>(errorResponse));

        Logger.LogDebug("StatusType bevragen op '{statusTypeUrl}'....", statusTypeUrl);

        return GetAsync<StatusTypeResponseDto>(new Uri(statusTypeUrl));
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

    public Task<ServiceAgentResponse<ZaakObjectTypeResponseDto>> GetZaakObjectTypeByUrlAsync(string zaakObjectTypeUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.ZTC, zaakObjectTypeUrl, "zaakobjecttypen", out var errorResponse))
            return Task.FromResult(new ServiceAgentResponse<ZaakObjectTypeResponseDto>(errorResponse));

        Logger.LogDebug("ZaakObjectType bevragen op '{zaakObjectTypeUrl}'....", zaakObjectTypeUrl);

        return GetAsync<ZaakObjectTypeResponseDto>(new Uri(zaakObjectTypeUrl));
    }
}
