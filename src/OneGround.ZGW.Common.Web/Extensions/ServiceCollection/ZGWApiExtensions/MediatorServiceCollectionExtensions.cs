using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Handlers;

namespace OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;

public static class MediatorServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection services, Assembly callingAssembly, ZGWApiServiceSettings apiServiceSettings)
    {
        services.AddMediatR(x =>
        {
            x.RegisterServicesFromAssemblies(callingAssembly);
        });

        if (apiServiceSettings.RegisterSharedAudittrailHandlers)
        {
            services.AddMediatR(x =>
            {
                x.RegisterServicesFromAssemblies(typeof(LogAuditTrailGetObjectListCommand).GetTypeInfo()
                    .Assembly); // The shared command handlers for audittrail for some API's not all
            });
        }

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(HandlerLoggingBehavior<,>));

        return services;
    }
}
