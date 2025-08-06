using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Notificaties.Contracts.v1;
using OneGround.ZGW.Notificaties.Contracts.v1.Responses;

namespace OneGround.ZGW.Notificaties.ServiceAgent;

public interface INotificatiesServiceAgent
{
    Task<ServiceAgentResponse<KanaalResponseDto>> GetKanaalByUrl(string url);
    Task<ServiceAgentResponse<IEnumerable<KanaalResponseDto>>> GetAllKanalenAsync();
    Task<ServiceAgentResponse> DeleteAbonnementByIdAsync(Guid abonnementId);
    Task<ServiceAgentResponse<NotificatieDto>> NotificeerAsync(NotificatieDto notificatieRequest, CancellationToken ct = default);
}
