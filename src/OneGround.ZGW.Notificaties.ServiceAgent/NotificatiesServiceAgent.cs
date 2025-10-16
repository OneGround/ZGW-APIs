using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Services;
using OneGround.ZGW.Notificaties.Contracts.v1;
using OneGround.ZGW.Notificaties.Contracts.v1.Responses;

namespace OneGround.ZGW.Notificaties.ServiceAgent;

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

    public async Task<ServiceAgentResponse<NotificatieDto>> NotifyAsync(NotificatieDto request, CancellationToken ct = default)
    {
        var url = new Uri("/notificaties", UriKind.Relative);
        return await PostAsync<NotificatieDto, NotificatieDto>(url, request);
    }

    public async Task<ServiceAgentResponse<IEnumerable<AbonnementResponseDto>>> GetAllAbonnementenAsync()
    {
        Logger.LogDebug("Getting all abonnementen");

        var url = new Uri("/abonnement", UriKind.Relative);

        return await GetAsync<IEnumerable<AbonnementResponseDto>>(url);
    }

    public async Task<ServiceAgentResponse<AbonnementDto>> GetAbonnementByUrlAsync(string url)
    {
        ArgumentNullException.ThrowIfNull(url);

        if (!EnsureValidResource(ServiceRoleName.NRC, url, "notificaties", out var errorResponse))
            return new ServiceAgentResponse<AbonnementDto>(errorResponse);

        Logger.LogDebug("Getting abonnement: '{url}'....", url);

        return await GetAsync<AbonnementDto>(new Uri(url));
    }

    public Task<ServiceAgentResponse<AbonnementDto>> AddAbonnementAsync(AbonnementDto abonnement)
    {
        var url = new Uri("/abonnement", UriKind.Relative);
        return PostAsync<AbonnementDto, AbonnementDto>(url, abonnement);
    }

    public async Task<ServiceAgentResponse<AbonnementDto>> PatchAbonnementByUrlAsync(string url, JObject abonnementPatchRequest)
    {
        ArgumentNullException.ThrowIfNull(url);

        if (!EnsureValidResource(ServiceRoleName.NRC, url, "abonnement", out var errorResponse))
            return new ServiceAgentResponse<AbonnementDto>(errorResponse);

        Logger.LogDebug("Patching abonnement: '{abonnementUrl}'....", url);

        return await PatchAsync<AbonnementDto>(new Uri(url), abonnementPatchRequest);
    }
}
