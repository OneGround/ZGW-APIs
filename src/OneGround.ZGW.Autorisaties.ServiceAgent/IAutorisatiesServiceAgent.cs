using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Autorisaties.Contracts.v1.Responses;
using OneGround.ZGW.Common.ServiceAgent;

namespace OneGround.ZGW.Autorisaties.ServiceAgent;

public interface IAutorisatiesServiceAgent
{
    Task<ServiceAgentResponse<ApplicatieResponseDto>> GetApplicatieByClientIdAsync(string clientId);
    Task<ServiceAgentResponse<ApplicatieResponseDto>> PatchApplicatieByUrlAsync(string applicatieUrl, JObject applicatiePatchRequest);
}
