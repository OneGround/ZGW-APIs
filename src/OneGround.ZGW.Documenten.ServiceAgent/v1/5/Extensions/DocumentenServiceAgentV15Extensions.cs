using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.ServiceAgent.Extensions;

namespace OneGround.ZGW.Documenten.ServiceAgent.v1._5.Extensions;

public static class DocumentenServiceAgentV15Extensions
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
