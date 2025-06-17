using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.ServiceAgent.Extensions;

namespace OneGround.ZGW.Documenten.ServiceAgent.v1._1.Extensions;

public static class DocumentenServiceAgentV11Extensions
{
    public static void AddDocumentenServiceAgent_v1_1(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceAgent<IDocumentenServiceAgent, DocumentenServiceAgent>(ServiceRoleName.DRC, configuration);
    }
}
