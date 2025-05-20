using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Services;
using OneGround.ZGW.Referentielijsten.Contracts.v1.Responses;

namespace OneGround.ZGW.Referentielijsten.ServiceAgent;

public class ReferentielijstenServiceAgent : ZGWServiceAgent<ReferentielijstenServiceAgent>, IReferentielijstenServiceAgent
{
    public ReferentielijstenServiceAgent(
        ILogger<ReferentielijstenServiceAgent> logger,
        HttpClient client,
        IServiceDiscovery serviceDiscovery,
        IServiceAgentResponseBuilder responseBuilder,
        IConfiguration configuration
    )
        : base(client, logger, serviceDiscovery, configuration, responseBuilder, ServiceRoleName.RL) { }

    public Task<ServiceAgentResponse<ProcesTypeResponseDto>> GetProcesTypeByUrlAsync(string procesTypeUrl)
    {
        ArgumentNullException.ThrowIfNull(procesTypeUrl);

        Logger.LogDebug("ProcesType bevragen op '{procesTypeUrl}'....", procesTypeUrl);

        return GetAsync<ProcesTypeResponseDto>(new Uri(procesTypeUrl));
    }

    public Task<ServiceAgentResponse<ResultaatResponseDto>> GetResultaatByUrl(string resultaatUrl)
    {
        ArgumentNullException.ThrowIfNull(resultaatUrl);

        Logger.LogDebug("Resultaat bevragen op '{resultaatUrl}'....", resultaatUrl);

        return GetAsync<ResultaatResponseDto>(new Uri(resultaatUrl));
    }

    public Task<ServiceAgentResponse<PagedResponse<ResultaatResponseDto>>> GetResultaten(int page)
    {
        var url = new Uri("/resultaten", UriKind.Relative).AddQueryParameter("page", page);

        return GetAsync<PagedResponse<ResultaatResponseDto>>(url);
    }

    public Task<ServiceAgentResponse<ResultaatTypeOmschrijvingResponseDto>> GetResultaatTypeOmschrijvingByUrlAsync(
        string resultaatTypeOmschrijvingUrl
    )
    {
        ArgumentNullException.ThrowIfNull(resultaatTypeOmschrijvingUrl);

        Logger.LogDebug("ResultaatTypeOmschrijving bevragen op '{resultaatTypeOmschrijvingUrl}'....", resultaatTypeOmschrijvingUrl);

        return GetAsync<ResultaatTypeOmschrijvingResponseDto>(new Uri(resultaatTypeOmschrijvingUrl));
    }
}
