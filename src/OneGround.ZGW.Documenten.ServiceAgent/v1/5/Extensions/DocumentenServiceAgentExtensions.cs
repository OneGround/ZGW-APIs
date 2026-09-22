using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.ServiceAgent.Extensions;

namespace OneGround.ZGW.Documenten.ServiceAgent.v1._5.Extensions;

public static class DocumentenServiceAgentExtensions
{
    public static void AddDocumentenServiceAgent_v1_5(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceAgent<IDocumentenServiceAgent, DocumentenServiceAgent>(ServiceRoleName.DRC, configuration);
        services.AddScoped<ICachedDocumentenServiceAgent, CachedDocumentServiceAgent>();
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
