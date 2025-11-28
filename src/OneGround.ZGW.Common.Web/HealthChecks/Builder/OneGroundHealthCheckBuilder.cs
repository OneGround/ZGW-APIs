using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Common.Web.HealthChecks.Checks;
using OneGround.ZGW.Common.Web.HealthChecks.Options;

namespace OneGround.ZGW.Common.Web.HealthChecks.Builder;

public class OneGroundHealthCheckBuilder(IServiceCollection services, IHealthChecksBuilder healthChecksBuilder)
{
    private readonly OptionsBuilder<OneGroundHealthChecksOptions> _optionsBuilder = services.AddOptions<OneGroundHealthChecksOptions>();

    public OneGroundHealthCheckBuilder AddRedisCheck()
    {
        var healthCheckName = RedisHealthCheck.HealthCheckName;
        healthChecksBuilder.AddCheck<RedisHealthCheck>(healthCheckName);
        AddRegisteredHealthCheck(healthCheckName);
        return this;
    }

    public OneGroundHealthCheckBuilder AddEventBusCheck()
    {
        var healthCheckName = EventBusHealthCheck.HealthCheckName;
        healthChecksBuilder.AddCheck<EventBusHealthCheck>(healthCheckName);
        AddRegisteredHealthCheck(healthCheckName);
        return this;
    }

    public OneGroundHealthCheckBuilder AddCheck<T>(string healthCheckName)
        where T : class, IHealthCheck
    {
        healthChecksBuilder.AddCheck<T>(healthCheckName);
        AddRegisteredHealthCheck(healthCheckName);
        return this;
    }

    public OneGroundHealthCheckBuilder Configure(Action<OneGroundHealthChecksOptions> configureOptions)
    {
        _optionsBuilder.Configure(configureOptions);
        return this;
    }

    private void AddRegisteredHealthCheck(string healthCheckName)
    {
        _optionsBuilder.Configure(x =>
        {
            if (x.RegisteredHealthChecks.Contains(healthCheckName))
            {
                throw new InvalidOperationException($"HealthCheck with name {healthCheckName} is already registered");
            }
            x.RegisteredHealthChecks.Add(healthCheckName);
        });
    }
}
