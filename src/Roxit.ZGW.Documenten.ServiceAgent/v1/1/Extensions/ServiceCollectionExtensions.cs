using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.ServiceAgent.Extensions;

namespace Roxit.ZGW.Documenten.ServiceAgent.v1._1.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddDocumentenServiceAgent_v1_1(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServiceAgent<IDocumentenServiceAgent, DocumentenServiceAgent>(ServiceRoleName.DRC, configuration);
    }
}
