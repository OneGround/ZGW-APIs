using System;
using System.Reflection;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Common.Configuration;

namespace OneGround.ZGW.Common.Services;

public class EndpointsAppSettingsServiceDiscovery : IServiceDiscovery
{
    private readonly IOptionsMonitor<EndpointConfiguration> _endPointConfigurationOptions;

    public EndpointsAppSettingsServiceDiscovery(IOptionsMonitor<EndpointConfiguration> endPointConfigurationOptions)
    {
        _endPointConfigurationOptions = endPointConfigurationOptions;
    }

    public Uri GetApi(string service)
    {
        var discoverableService = GetService(service);
        return new Uri(discoverableService.Api);
    }

    private DiscoverableService GetService(string service)
    {
        var currentValue = _endPointConfigurationOptions.CurrentValue;

        var prop =
            currentValue.GetType().GetProperty(service, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Service '{service}' not found in configuration");
        return (DiscoverableService)prop.GetValue(currentValue);
    }

    public Uri GetApi(string service, string version)
    {
        return GetApi(service);
    }
}
