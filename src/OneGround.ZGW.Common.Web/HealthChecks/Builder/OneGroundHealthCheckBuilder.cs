using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OneGround.ZGW.Common.Web.HealthChecks.Checks;
using OneGround.ZGW.Common.Web.HealthChecks.Options;

namespace OneGround.ZGW.Common.Web.HealthChecks.Builder;

public class OneGroundHealthCheckBuilder(IServiceCollection services, IHealthChecksBuilder healthChecksBuilder)
{
    private readonly List<string> _healthChecksList = [];
    private IHealthChecksBuilder _healthChecksBuilder = healthChecksBuilder;

    public OneGroundHealthCheckBuilder AddRedisCheck()
    {
        _healthChecksBuilder = _healthChecksBuilder.AddCheck<RedisHealthCheck>(RedisHealthCheck.HealthCheckName);
        _healthChecksList.Add(RedisHealthCheck.HealthCheckName);
        return this;
    }

    public OneGroundHealthCheckBuilder AddEventBusCheck()
    {
        _healthChecksBuilder = _healthChecksBuilder.AddCheck<EventBusHealthCheck>(EventBusHealthCheck.HealthCheckName);
        _healthChecksList.Add(EventBusHealthCheck.HealthCheckName);
        return this;
    }

    public OneGroundHealthCheckBuilder AddCheck<T>(string healthCheckName)
        where T : class, IHealthCheck
    {
        _healthChecksBuilder = _healthChecksBuilder.AddCheck<T>(healthCheckName);
        _healthChecksList.Add(healthCheckName);
        return this;
    }

    public void Build(Action<OneGroundHealthChecksOptions> configureOptions = null)
    {
        services
            .AddOptions<OneGroundHealthChecksOptions>()
            .Configure(x =>
            {
                if (configureOptions != null)
                {
                    configureOptions(x);
                }

                x.RegisteredHealthChecks = _healthChecksList;
            });
    }
}
