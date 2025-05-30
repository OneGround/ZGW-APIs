using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Roxit.ZGW.Autorisaties.Contracts.v1.Responses;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Extensions;
using Roxit.ZGW.Common.ServiceAgent;
using Roxit.ZGW.Common.Services;

namespace Roxit.ZGW.Autorisaties.ServiceAgent;

public class AutorisatiesServiceAgent : ZGWServiceAgent<AutorisatiesServiceAgent>, IAutorisatiesServiceAgent
{
    public AutorisatiesServiceAgent(
        ILogger<AutorisatiesServiceAgent> logger,
        HttpClient client,
        IServiceDiscovery serviceDiscovery,
        IServiceAgentResponseBuilder responseBuilder,
        IConfiguration configuration
    )
        : base(client, logger, serviceDiscovery, configuration, responseBuilder, ServiceRoleName.AC) { }

    public async Task<ServiceAgentResponse<ApplicatieResponseDto>> GetApplicatieByClientIdAsync(string clientId)
    {
        ArgumentNullException.ThrowIfNull(clientId);

        Logger.LogDebug("Getting app authorization by clientId: '{clientId}'....", clientId);

        var url = new Uri("/applicaties", UriKind.Relative).Combine("consumer").AddQueryParameter("clientId", clientId);

        Logger.LogDebug("Authorization url: '{url}'", url);

        return await GetAsync<ApplicatieResponseDto>(url);
    }

    public async Task<ServiceAgentResponse<ApplicatieResponseDto>> PatchApplicatieByUrlAsync(string applicatieUrl, JObject applicatiePatchRequest)
    {
        if (!EnsureValidResource(ServiceRoleName.AC, applicatieUrl, "applicaties", out var errorResponse))
            return new ServiceAgentResponse<ApplicatieResponseDto>(errorResponse);

        Logger.LogDebug("Patch applicatie '{applicatieUrl}'....", applicatieUrl);

        return await PatchAsync<ApplicatieResponseDto>(new Uri(applicatieUrl), applicatiePatchRequest);
    }
}
