using System.Threading.Tasks;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.ServiceAgent;
using Roxit.ZGW.Referentielijsten.Contracts.v1.Responses;

namespace Roxit.ZGW.Referentielijsten.ServiceAgent;

public interface IReferentielijstenServiceAgent
{
    Task<ServiceAgentResponse<ProcesTypeResponseDto>> GetProcesTypeByUrlAsync(string procesTypeUrl);
    Task<ServiceAgentResponse<ResultaatResponseDto>> GetResultaatByUrl(string resultaatUrl);
    Task<ServiceAgentResponse<PagedResponse<ResultaatResponseDto>>> GetResultaten(int page = 1);
    Task<ServiceAgentResponse<ResultaatTypeOmschrijvingResponseDto>> GetResultaatTypeOmschrijvingByUrlAsync(string resultaatTypeOmschrijvingUrl);
}
