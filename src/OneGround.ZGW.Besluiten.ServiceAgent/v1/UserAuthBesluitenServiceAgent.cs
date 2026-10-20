using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Services;

namespace OneGround.ZGW.Besluiten.ServiceAgent.v1;

// Note: Identical implementation to BesluitenServiceAgent -- the only reason this is a distinct class is so
// AddServiceAgent<IUserAuthBesluitenServiceAgent, UserAuthBesluitenServiceAgent> gets its own typed HttpClient (keyed by this class's full name)
// wired with AuthorizationType.UserAccount, instead of sharing IBesluitenServiceAgent's own ServiceAccount-authenticated HttpClient registration.
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
