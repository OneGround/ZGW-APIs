using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Roxit.ZGW.Common.ServiceAgent;
using Roxit.ZGW.Notificaties.Contracts.v1.Responses;

namespace Roxit.ZGW.Notificaties.ServiceAgent;

public interface INotificatiesServiceAgent
{
    Task<ServiceAgentResponse<KanaalResponseDto>> GetKanaalByUrl(string url);
    Task<ServiceAgentResponse<IEnumerable<KanaalResponseDto>>> GetAllKanalenAsync();
    Task<ServiceAgentResponse> DeleteAbonnementByIdAsync(Guid abonnementId);
}
