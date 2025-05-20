using System.Threading.Tasks;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Referentielijsten.Contracts.v1.Responses;

namespace OneGround.ZGW.Referentielijsten.ServiceAgent;

public interface IReferentielijstenServiceAgent
{
    Task<ServiceAgentResponse<ProcesTypeResponseDto>> GetProcesTypeByUrlAsync(string procesTypeUrl);
    Task<ServiceAgentResponse<ResultaatResponseDto>> GetResultaatByUrl(string resultaatUrl);
    Task<ServiceAgentResponse<PagedResponse<ResultaatResponseDto>>> GetResultaten(int page = 1);
    Task<ServiceAgentResponse<ResultaatTypeOmschrijvingResponseDto>> GetResultaatTypeOmschrijvingByUrlAsync(string resultaatTypeOmschrijvingUrl);
}
