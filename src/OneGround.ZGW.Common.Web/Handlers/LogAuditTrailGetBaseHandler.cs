using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Configuration;

namespace OneGround.ZGW.Common.Web.Handlers;

public abstract class LogAuditTrailGetBaseHandler : ZGWBaseHandler
{
    private readonly bool _isClientIdExcluded;

    protected LogAuditTrailGetBaseHandler(
        IConfiguration configuration,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IHttpContextAccessor httpContextAccessor
    )
        : base(configuration, authorizationContextAccessor)
    {
        var settings = Configuration.GetSection(AuditTrailRetrieveOptions.SectionName).Get<AuditTrailRetrieveOptions>() ?? new();

        IsAudittrailRetrieveMinimal = settings.AudittrailRecordRetrieveMinimal;

        var matcher = new ClientIdExcludeMatcher(settings.AudittrailRetrieveRecordExcludeClientIds);

        var clientId = httpContextAccessor.HttpContext?.GetClientId();
        _isClientIdExcluded = matcher.IsExcluded(clientId);
    }

    protected bool IsClientIdExcluded => _isClientIdExcluded;
    protected bool IsAudittrailRetrieveMinimal { get; }
}
