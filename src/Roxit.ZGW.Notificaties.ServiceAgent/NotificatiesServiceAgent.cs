using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.ServiceAgent;
using Roxit.ZGW.Common.Services;
using Roxit.ZGW.Notificaties.Contracts.v1.Responses;

namespace Roxit.ZGW.Notificaties.ServiceAgent;

public class NotificatiesServiceAgent : ZGWServiceAgent<NotificatiesServiceAgent>, INotificatiesServiceAgent
{
    public NotificatiesServiceAgent(
        ILogger<NotificatiesServiceAgent> logger,
        HttpClient client,
        IServiceDiscovery serviceDiscovery,
        IServiceAgentResponseBuilder responseBuilder,
        IConfiguration configuration
    )
        : base(client, logger, serviceDiscovery, configuration, responseBuilder, ServiceRoleName.NRC) { }

    public Task<ServiceAgentResponse<KanaalResponseDto>> GetKanaalByUrl(string url)
    {
        ArgumentNullException.ThrowIfNull(url);

        Logger.LogDebug("Getting kanaal: '{url}'....", url);

        return GetAsync<KanaalResponseDto>(new Uri(url));
    }

    public Task<ServiceAgentResponse<IEnumerable<KanaalResponseDto>>> GetAllKanalenAsync()
    {
        Logger.LogDebug("Getting all kanalen");

        var url = new Uri("/kanaal", UriKind.Relative);

        return GetAsync<IEnumerable<KanaalResponseDto>>(url);
    }

    public async Task<ServiceAgentResponse> DeleteAbonnementByIdAsync(Guid abonnementId)
    {
        var url = new Uri($"/abonnement/{abonnementId}", UriKind.Relative);
        return await DeleteAsync(url);
    }
}
