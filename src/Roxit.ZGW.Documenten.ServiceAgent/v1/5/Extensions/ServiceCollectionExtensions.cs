using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.ServiceAgent;
using Roxit.ZGW.Common.ServiceAgent.Extensions;
using Roxit.ZGW.Common.Services;

namespace Roxit.ZGW.Documenten.ServiceAgent.v1._5.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServiceAuthDocumentenServiceAgent_v1_5(this IServiceCollection services, IConfiguration configuration)
    {
        // Service-account authorized ServiceAgent like "oneground-09435039" (for system cross-API like add/deleting mirrored zrc/brc relations)
        services.AddServiceAgent<IDocumentenServiceAgent, DocumentenServiceAgent>(ServiceRoleName.DRC, configuration);
    }

    public static void AddUserAuthDocumentenServiceAgent_v1_5(this IServiceCollection services, IConfiguration configuration)
    {
        // User authorized ServiceAgent (for user cross-API expands like ZRC->DRC / BRC->DRC)
        services.AddServiceAgent<IUserAuthDocumentenServiceAgent, UserAuthDocumentenServiceAgent>(
            ServiceRoleName.DRC,
            configuration,
            authorizationType: AuthorizationType.UserAccount
        );
    }
}

public interface IUserAuthDocumentenServiceAgent : IDocumentenServiceAgent { }

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
