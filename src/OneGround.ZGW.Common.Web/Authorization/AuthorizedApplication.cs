using System.Collections.Generic;

namespace OneGround.ZGW.Common.Web.Authorization;

public class AuthorizedApplication
{
    /// <summary>
    /// Application label in AC.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Indicates the context has all authorizations (super user).
    /// </summary>
    public bool HasAllAuthorizations { get; set; }

    /// <summary>
    /// Allowed authorizations based on scope.
    /// </summary>
    public IEnumerable<AuthorizationPermission> Authorizations { get; set; }

    /// <summary>
    /// Gets rsin value which application is authorized for.
    /// </summary>
    public string Rsin { get; set; }
}
