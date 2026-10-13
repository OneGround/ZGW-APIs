using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Services;

namespace OneGround.ZGW.Catalogi.ServiceAgent.v1._3;

// Note: Identical implementation to CatalogiServiceAgent -- the only reason this is a distinct class is so
// AddServiceAgent<IUserAuthCatalogiServiceAgent, UserAuthCatalogiServiceAgent> gets its own typed HttpClient (keyed by this class's full
// name) wired with AuthorizationType.UserAccount, instead of sharing ICatalogiServiceAgent's ServiceAccount-authenticated HttpClient.
public class UserAuthCatalogiServiceAgent : CatalogiServiceAgent, IUserAuthCatalogiServiceAgent
{
    public UserAuthCatalogiServiceAgent(
        ILogger<CatalogiServiceAgent> logger,
        HttpClient client,
        IServiceDiscovery serviceDiscovery,
        IServiceAgentResponseBuilder responseBuilder,
        IConfiguration configuration
    )
        : base(logger, client, serviceDiscovery, responseBuilder, configuration) { }
}
