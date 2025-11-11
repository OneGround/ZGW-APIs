using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Authentication;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Services;

namespace Roxit.ZGW.Documenten.Jobs.Subscription;

public abstract class SubscriptionJobBase<TLogger>
    where TLogger : class
{
    protected readonly ILogger<TLogger> _logger;
    protected readonly ICorrelationContextAccessor _correlationContextAccessor;
    protected readonly IOrganisationContextAccessor _organisationContextAccessor;
    protected readonly IOrganisationContextFactory _organisationContextFactory;
    private readonly IServiceDiscovery _serviceDiscovery;

    protected SubscriptionJobBase(
        ILogger<TLogger> logger,
        ICorrelationContextAccessor correlationContextAccessor,
        IOrganisationContextAccessor organisationContextAccessor,
        IOrganisationContextFactory organisationContextFactory,
        IServiceDiscovery serviceDiscovery
    )
    {
        _logger = logger;
        _correlationContextAccessor = correlationContextAccessor;
        _organisationContextAccessor = organisationContextAccessor;
        _organisationContextFactory = organisationContextFactory;
        _serviceDiscovery = serviceDiscovery;
    }

    protected string DocumentListenerApiUrl => _serviceDiscovery.GetApi(ServiceRoleName.DRC_LISTENER).ToString().TrimEnd('/');

    protected bool IsDocumentListenerSubscription(string callbackUrl)
    {
        if (string.IsNullOrWhiteSpace(callbackUrl))
        {
            return false;
        }
        return callbackUrl.Contains(DocumentListenerApiUrl, StringComparison.OrdinalIgnoreCase);
    }

    protected IDisposable GetLoggingScope(string rsin, string correlationId)
    {
        return _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId, ["RSIN"] = rsin });
    }
}
