using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Configuration;

namespace OneGround.ZGW.Common.Web.Handlers;

public abstract class LogAuditTrailGetBaseHandler : ZGWBaseHandler
{
    private readonly IRetrieveAuditClientExclusion _clientExclusion;

    protected LogAuditTrailGetBaseHandler(
        IConfiguration configuration,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IRetrieveAuditClientExclusion clientExclusion
    )
        : base(configuration, authorizationContextAccessor)
    {
        var settings = Configuration.GetSection(AuditTrailRetrieveOptions.SectionName).Get<AuditTrailRetrieveOptions>() ?? new();
        IsAudittrailRetrieveMinimal = settings.AudittrailRecordRetrieveMinimal;
        _clientExclusion = clientExclusion;
    }

    protected bool IsClientIdExcluded => _clientExclusion.IsCurrentClientExcluded;
    protected bool IsAudittrailRetrieveMinimal { get; }
}
