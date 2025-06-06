using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Besluiten.Contracts.v1.Queries;
using OneGround.ZGW.Besluiten.Contracts.v1.Responses;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Services;

namespace OneGround.ZGW.Besluiten.ServiceAgent.v1;

public class BesluitenServiceAgent : ZGWServiceAgent<BesluitenServiceAgent>, IBesluitenServiceAgent
{
    public BesluitenServiceAgent(
        ILogger<BesluitenServiceAgent> logger,
        HttpClient client,
        IServiceDiscovery serviceDiscovery,
        IServiceAgentResponseBuilder responseBuilder,
        IConfiguration configuration
    )
        : base(client, logger, serviceDiscovery, configuration, responseBuilder, ServiceRoleName.BRC) { }

    public async Task<ServiceAgentResponse<BesluitResponseDto>> GetBesluitByUrlAsync(string besluitUrl)
    {
        if (!EnsureValidResource(ServiceRoleName.BRC, besluitUrl, "besluiten", out var errorResponse))
            return new ServiceAgentResponse<BesluitResponseDto>(errorResponse);

        return await GetAsync<BesluitResponseDto>(new Uri(besluitUrl));
    }

    public async Task<ServiceAgentResponse<IEnumerable<BesluitInformatieObjectResponseDto>>> GetBesluitInformatieObjectenAsync(
        GetAllBesluitInformatieObjectenQueryParameters parameters
    )
    {
        return await GetAsync<BesluitInformatieObjectResponseDto>("/besluitinformatieobjecten", parameters);
    }
}
