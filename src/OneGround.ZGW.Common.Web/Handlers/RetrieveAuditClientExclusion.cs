using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Web.Configuration;

namespace OneGround.ZGW.Common.Web.Handlers;

/// <summary>
/// Decides whether the current request's client_id is excluded from retrieve audit-trail
/// logging, based on the configured glob patterns in
/// <see cref="AuditTrailRetrieveOptions.AudittrailRetrieveRecordExcludeClientIds"/>.
/// Scoped per request; the decision is computed once from the request's client_id.
/// </summary>
public interface IRetrieveAuditClientExclusion
{
    bool IsCurrentClientExcluded { get; }
}

public sealed class RetrieveAuditClientExclusion : IRetrieveAuditClientExclusion
{
    private readonly bool _isExcluded;

    public RetrieveAuditClientExclusion(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        var settings = configuration.GetSection(AuditTrailRetrieveOptions.SectionName).Get<AuditTrailRetrieveOptions>() ?? new();
        var matcher = new ClientIdExcludeMatcher(settings.AudittrailRetrieveRecordExcludeClientIds);
        var clientId = httpContextAccessor.HttpContext?.GetClientId();
        _isExcluded = matcher.IsExcluded(clientId);
    }

    public bool IsCurrentClientExcluded => _isExcluded;
}
