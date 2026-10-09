using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.ServiceAgent.Extensions;

namespace OneGround.ZGW.Documenten.ServiceAgent.v1._7.Extensions;

public static class DocumentenServiceAgentExtensions
{
    public static void AddDocumentenServiceAgent_v1_7(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceAgent<IDocumentenServiceAgent, DocumentenServiceAgent>(ServiceRoleName.DRC, configuration);
        services.AddScoped<ICachedDocumentenServiceAgent, CachedDocumentServiceAgent>();
    }

    // Note: User authorized ServiceAgent (for user cross-API expands like ZRC->DRC), on the v1._7
    // DTO/contract shape -- see IUserAuthDocumentenServiceAgent's own remarks for why this exists
    // alongside (not instead of) the v1._5 AddUserAuthDocumentenServiceAgent_v1_5.
    public static void AddUserAuthDocumentenServiceAgent_v1_7(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceAgent<IUserAuthDocumentenServiceAgent, UserAuthDocumentenServiceAgent>(
            ServiceRoleName.DRC,
            configuration,
            authorizationType: AuthorizationType.UserAccount
        );
    }
}
