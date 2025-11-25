using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Authentication;
using OneGround.ZGW.Common.Services;

namespace OneGround.ZGW.Common.ServiceAgent.Authentication;

public class ServiceAgentAuthenticationHandler : DelegatingHandler
{
    private readonly string _serviceRoleName;
    private readonly ICachedZGWSecrets _cachedSecrets;
    private readonly IOrganisationContextAccessor _organisationContextAccessor;
    private readonly ILogger<ServiceAgentAuthenticationHandler> _logger;
    private readonly IZgwTokenCacheService _zgwTokenCacheService;

    public ServiceAgentAuthenticationHandler(
        string serviceRoleName,
        ICachedZGWSecrets cachedSecrets,
        IOrganisationContextAccessor organisationContextAccessor,
        IZgwTokenCacheService zgwTokenCacheService,
        ILogger<ServiceAgentAuthenticationHandler> logger
    )
    {
        _serviceRoleName = serviceRoleName;
        _cachedSecrets = cachedSecrets;
        _organisationContextAccessor = organisationContextAccessor;
        _zgwTokenCacheService = zgwTokenCacheService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var rsin = _organisationContextAccessor.OrganisationContext.Rsin;
        var value = await _cachedSecrets.GetServiceSecretAsync(rsin, _serviceRoleName, cancellationToken);

        if (value != null)
        {
            var response = await _zgwTokenCacheService.GetCachedTokenAsync(value.ClientId, value.Secret, cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, response.token);
        }
        else
        {
            _logger.LogDebug("No service secret configured for service: {ServiceRoleName} and rsin: {Rsin}", _serviceRoleName, rsin);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
