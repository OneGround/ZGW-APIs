using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Web.Authorization;

namespace OneGround.ZGW.Zaken.Web.Authorization;

public class ScopeAttribute : BaseScopeAttribute
{
    public ScopeAttribute(params string[] scopes)
        : base(ServiceRoleName.ZRC, scopes) { }
}
