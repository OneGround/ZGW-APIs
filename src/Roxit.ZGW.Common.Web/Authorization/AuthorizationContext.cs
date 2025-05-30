namespace Roxit.ZGW.Common.Web.Authorization;

public class AuthorizationContext
{
    /// <summary>
    /// Contains authorization details.
    /// </summary>
    public AuthorizedApplication Authorization { get; }

    /// <summary>
    /// Contains a list of scopes requested.
    /// </summary>
    public string[] RequestedScopes { get; }

    public AuthorizationContext(AuthorizedApplication authorization, string[] requestedScopes)
    {
        Authorization = authorization;
        RequestedScopes = requestedScopes;
    }
}
