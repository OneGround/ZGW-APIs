using System.Collections.Generic;
using System.Threading.Tasks;
using Roxit.ZGW.Besluiten.Contracts.v1.Queries;
using Roxit.ZGW.Besluiten.Contracts.v1.Responses;
using Roxit.ZGW.Common.ServiceAgent;

namespace Roxit.ZGW.Besluiten.ServiceAgent.v1;

public interface IBesluitenServiceAgent
{
    Task<ServiceAgentResponse<BesluitResponseDto>> GetBesluitByUrlAsync(string besluitUrl);
    Task<ServiceAgentResponse<IEnumerable<BesluitInformatieObjectResponseDto>>> GetBesluitInformatieObjectenAsync(
        GetAllBesluitInformatieObjectenQueryParameters parameters
    );
}
