using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Web.Authorization;

namespace Roxit.ZGW.Catalogi.Web.Authorization;

public class ScopeAttribute : BaseScopeAttribute
{
    public ScopeAttribute(params string[] scopes)
        : base(ServiceRoleName.ZTC, scopes) { }
}
