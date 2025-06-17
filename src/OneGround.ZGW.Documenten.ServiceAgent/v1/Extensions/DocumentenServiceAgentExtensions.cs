using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.ServiceAgent.Extensions;

namespace OneGround.ZGW.Documenten.ServiceAgent.v1.Extensions;

public static class DocumentenServiceAgentExtensions
{
    public static void AddDocumentenServiceAgent(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceAgent<IDocumentenServiceAgent, DocumentenServiceAgent>(ServiceRoleName.DRC, configuration);
        services.AddScoped<ICachedDocumentenServiceAgent, CachedDocumentServiceAgent>();
    }
}
