using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Authentication;
using Roxit.ZGW.Common.Services;

namespace Roxit.ZGW.Common.ServiceAgent.Authentication;

public class ServiceAgentAuthenticationHandlerFactory
{
    private readonly ICachedZGWSecrets _cachedSecrets;
    private readonly IOrganisationContextAccessor _organisationContextAccessor;
    private readonly ILogger<ServiceAgentAuthenticationHandler> _logger;
    private readonly IZgwTokenCacheService _zgwTokenCacheService;

    public ServiceAgentAuthenticationHandlerFactory(
        ICachedZGWSecrets cachedSecrets,
        IOrganisationContextAccessor organisationContextAccessor,
        IZgwTokenCacheService zgwTokenCacheService,
        ILogger<ServiceAgentAuthenticationHandler> logger
    )
    {
        _cachedSecrets = cachedSecrets;
        _organisationContextAccessor = organisationContextAccessor;
        _zgwTokenCacheService = zgwTokenCacheService;
        _logger = logger;
    }

    public ServiceAgentAuthenticationHandler Create(string serviceRoleName)
    {
        return new ServiceAgentAuthenticationHandler(serviceRoleName, _cachedSecrets, _organisationContextAccessor, _zgwTokenCacheService, _logger);
    }
}
