using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Services;

namespace OneGround.ZGW.Besluiten.ServiceAgent.v1;

// Note: Same implementation as BesluitenServiceAgent; a distinct class because the typed HttpClient is keyed by the full class name, so this
// one gets its own AuthorizationType.UserAccount client. Do not merge: the JWT forwarding would be lost.
public class UserAuthBesluitenServiceAgent : BesluitenServiceAgent, IUserAuthBesluitenServiceAgent
{
    public UserAuthBesluitenServiceAgent(
        ILogger<BesluitenServiceAgent> logger,
        HttpClient client,
        IServiceDiscovery serviceDiscovery,
        IServiceAgentResponseBuilder responseBuilder,
        IConfiguration configuration
    )
        : base(logger, client, serviceDiscovery, responseBuilder, configuration) { }
}
