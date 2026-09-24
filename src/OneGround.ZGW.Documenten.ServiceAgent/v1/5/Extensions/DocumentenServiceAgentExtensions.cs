using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.ServiceAgent.Extensions;

namespace OneGround.ZGW.Documenten.ServiceAgent.v1._5.Extensions;

public static class DocumentenServiceAgentExtensions
{
    [Obsolete("This class is obsolete. Use the new expand/field-selection and registration mechanism instead.")]
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
