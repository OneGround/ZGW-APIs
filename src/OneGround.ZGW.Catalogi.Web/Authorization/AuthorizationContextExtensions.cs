using System.Linq;
using OneGround.ZGW.Common.Web.Authorization;

namespace OneGround.ZGW.Catalogi.Web.Authorization;

public static class CatalogiAuthorizationContextExtensions
{
    /// <summary>
    /// Validates if forced delete scope is authorized for this request.
    /// </summary>
    public static bool IsForcedDeleteAuthorized(this AuthorizationContext context)
    {
        if (context.Authorization.HasAllAuthorizations)
            return true;

        return context.Authorization.Authorizations.Any(a => a.Scopes.Contains(AuthorizationScopes.Catalogi.ForcedDelete));
    }

    /// <summary>
    /// Validates if forced update scope is authorized for this request.
    /// </summary>
    public static bool IsForcedUpdateAuthorized(this AuthorizationContext context)
    {
        if (context.Authorization.HasAllAuthorizations)
            return true;

        return context.Authorization.Authorizations.Any(a => a.Scopes.Contains(AuthorizationScopes.Catalogi.ForcedUpdate));
    }
}
