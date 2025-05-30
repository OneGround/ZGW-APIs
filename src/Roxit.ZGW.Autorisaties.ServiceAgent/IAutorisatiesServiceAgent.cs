using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Roxit.ZGW.Autorisaties.Contracts.v1.Responses;
using Roxit.ZGW.Common.ServiceAgent;

namespace Roxit.ZGW.Autorisaties.ServiceAgent;

public interface IAutorisatiesServiceAgent
{
    Task<ServiceAgentResponse<ApplicatieResponseDto>> GetApplicatieByClientIdAsync(string clientId);
    Task<ServiceAgentResponse<ApplicatieResponseDto>> PatchApplicatieByUrlAsync(string applicatieUrl, JObject applicatiePatchRequest);
}
