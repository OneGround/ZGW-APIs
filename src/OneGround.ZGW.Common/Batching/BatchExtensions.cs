using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace OneGround.ZGW.Common.Batching;

public static class BatchExtensions
{
    public static IServiceCollection AddBatchId(this IServiceCollection services)
    {
        services.AddSingleton<IBatchIdAccessor, BatchIdAccessor>();
        return services;
    }

    public static IApplicationBuilder UseBatchId(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.ApplicationServices.GetService(typeof(IBatchIdAccessor)) != null
            ? app.UseMiddleware<BatchIdMiddleware>()
            : throw new InvalidOperationException(
                "Unable to find the required service. You must call the AddBatchId method in ConfigureServices in the application startup code."
            );
    }
}
