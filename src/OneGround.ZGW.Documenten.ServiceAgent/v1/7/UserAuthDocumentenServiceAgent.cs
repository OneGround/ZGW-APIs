using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Services;

namespace OneGround.ZGW.Documenten.ServiceAgent.v1._7;

// Note: Identical implementation to DocumentenServiceAgent -- the only reason this is a distinct class
// is so AddServiceAgent<IUserAuthDocumentenServiceAgent, UserAuthDocumentenServiceAgent> gets its own
// typed HttpClient (keyed by this class's full name) wired with AuthorizationType.UserAccount, instead
// of sharing IDocumentenServiceAgent's own ServiceAccount-authenticated HttpClient registration.
public class UserAuthDocumentenServiceAgent : DocumentenServiceAgent, IUserAuthDocumentenServiceAgent
{
    public UserAuthDocumentenServiceAgent(
        ILogger<DocumentenServiceAgent> logger,
        HttpClient client,
        IServiceDiscovery serviceDiscovery,
        IServiceAgentResponseBuilder responseBuilder,
        IConfiguration configuration
    )
        : base(logger, client, serviceDiscovery, responseBuilder, configuration) { }
}
