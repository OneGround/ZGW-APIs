using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Web.Authorization;

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
        IsAudittrailRetrieveMinimal = Configuration.GetSection("Application:AudittrailRecordRetrieveMinimal").Get<bool?>() ?? true;

        var excludeClientIds = Configuration.GetSection("Application:AudittrailRetrieveRecordExcludeClientIds").Get<IEnumerable<string>>() ?? [];
        var matcher = new ClientIdExcludeMatcher(excludeClientIds);

        var clientId = httpContextAccessor.HttpContext?.GetClientId();
        _isClientIdExcluded = matcher.IsExcluded(clientId);
    }

    protected bool IsClientIdExcluded => _isClientIdExcluded;
    protected bool IsAudittrailRetrieveMinimal { get; }
}
