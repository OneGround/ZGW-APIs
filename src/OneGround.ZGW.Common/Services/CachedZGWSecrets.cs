using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.Secrets;

namespace OneGround.ZGW.Common.Services;

public class CachedZGWSecrets : ICachedZGWSecrets
{
    private readonly IOptionsMonitor<ZgwServiceAccountConfiguration> _optionsMonitor;

    public CachedZGWSecrets(IOptionsMonitor<ZgwServiceAccountConfiguration> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }

    public Task<ServiceSecret> GetServiceSecretAsync(string rsin, string service, CancellationToken cancellationToken)
    {
        _optionsMonitor.CurrentValue.ZgwServiceAccountCredentials.TryGetValue(rsin, out var credentials);

        if (credentials == null)
        {
            throw new Exception($"No service account credentials were found for {rsin}");
        }

        if (string.IsNullOrEmpty(credentials.ClientId) || string.IsNullOrEmpty(credentials.ClientSecret))
        {
            throw new Exception($"ClientId or ClientSecret not found for rsin {rsin}");
        }

        var serviceSecret = new ServiceSecret { ClientId = credentials.ClientId, Secret = credentials.ClientSecret };

        return Task.FromResult(serviceSecret);
    }
}
