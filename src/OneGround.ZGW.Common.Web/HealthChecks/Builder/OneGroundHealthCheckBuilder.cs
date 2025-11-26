using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Common.Web.HealthChecks.Checks;
using OneGround.ZGW.Common.Web.HealthChecks.Options;

namespace OneGround.ZGW.Common.Web.HealthChecks.Builder;

public class OneGroundHealthCheckBuilder(IServiceCollection services, IHealthChecksBuilder healthChecksBuilder)
{
    private IHealthChecksBuilder _healthChecksBuilder = healthChecksBuilder;
    private readonly OptionsBuilder<OneGroundHealthChecksOptions> _optionsBuilder = services.AddOptions<OneGroundHealthChecksOptions>();
    private readonly List<string> _healthChecksList = [];

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

    public OneGroundHealthCheckBuilder Configure(Action<OneGroundHealthChecksOptions> configureOptions)
    {
        _optionsBuilder.Configure(configureOptions);
        return this;
    }

    public void Build(Action<OneGroundHealthChecksOptions> configureOptions = null)
    {
        _optionsBuilder.Configure(x =>
        {
            configureOptions?.Invoke(x);
            x.RegisteredHealthChecks = _healthChecksList;
        });
    }
}
