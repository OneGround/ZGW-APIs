using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Web.Authorization;

namespace Roxit.ZGW.Zaken.Web.Authorization;

public class ScopeAttribute : BaseScopeAttribute
{
    public ScopeAttribute(params string[] scopes)
        : base(ServiceRoleName.ZRC, scopes) { }
}
