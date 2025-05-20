using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Common.Web.Authorization;

namespace OneGround.ZGW.Common.Web.Handlers;

public abstract class LogAuditTrailGetBaseHandler : ZGWBaseHandler
{
    private readonly IEnumerable<string> _audittrailRetrieveForRsins;

    protected LogAuditTrailGetBaseHandler(IConfiguration configuration, IAuthorizationContextAccessor authorizationContextAccessor)
        : base(configuration, authorizationContextAccessor)
    {
        _audittrailRetrieveForRsins = Configuration.GetSection("Application:AudittrailRetrieveForRsins").Get<IEnumerable<string>>() ?? [];

        IsAudittrailRetrieveMinimal = Configuration.GetSection("Application:AudittrailRecordRetrieveMinimal").Get<bool?>() ?? true;

        IsAudittrailRecordRetrieveList = Configuration.GetSection("Application:AudittrailRecordRetrieveList").Get<bool?>() ?? false;
    }

    protected bool IsAudittrailRetrieveForRsin => _audittrailRetrieveForRsins.Contains(_rsin);
    protected bool IsAudittrailRecordRetrieveList { get; }
    protected bool IsAudittrailRetrieveMinimal { get; }
}
