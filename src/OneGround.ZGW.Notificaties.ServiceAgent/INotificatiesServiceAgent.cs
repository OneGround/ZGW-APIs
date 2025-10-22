using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Notificaties.Contracts.v1;
using OneGround.ZGW.Notificaties.Contracts.v1.Responses;

namespace OneGround.ZGW.Notificaties.ServiceAgent;

public interface INotificatiesServiceAgent
{
    Task<ServiceAgentResponse<KanaalResponseDto>> GetKanaalByUrl(string url);
    Task<ServiceAgentResponse<IEnumerable<KanaalResponseDto>>> GetAllKanalenAsync();
    Task<ServiceAgentResponse> DeleteAbonnementByIdAsync(Guid abonnementId);
    Task<ServiceAgentResponse<NotificatieDto>> NotifyAsync(NotificatieDto request, CancellationToken ct = default);

    Task<ServiceAgentResponse<IEnumerable<AbonnementResponseDto>>> GetAllAbonnementenAsync();
    Task<ServiceAgentResponse<AbonnementDto>> AddAbonnementAsync(AbonnementDto abonnement);
    Task<ServiceAgentResponse<AbonnementDto>> PatchAbonnementByUrlAsync(string url, JObject abonnementPatchRequest);
}
