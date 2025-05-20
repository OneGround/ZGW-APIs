using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Besluiten.Contracts.v1.Queries;
using OneGround.ZGW.Besluiten.Contracts.v1.Responses;
using OneGround.ZGW.Common.ServiceAgent;

namespace OneGround.ZGW.Besluiten.ServiceAgent.v1;

public interface IBesluitenServiceAgent
{
    Task<ServiceAgentResponse<BesluitResponseDto>> GetBesluitByUrlAsync(string besluitUrl);
    Task<ServiceAgentResponse<IEnumerable<BesluitInformatieObjectResponseDto>>> GetBesluitInformatieObjectenAsync(
        GetAllBesluitInformatieObjectenQueryParameters parameters
    );
}
