using Microsoft.Extensions.DependencyInjection;

namespace OneGround.ZGW.Common.CorrelationId;

public static class CorrelationIdExtensions
{
    public static void AddCorrelationId(this IServiceCollection services)
    {
        services.AddTransient<ICorrelationContextAccessor, CorrelationContextAccessor>();
    }
}
