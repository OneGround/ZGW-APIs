using System;
using System.Linq;
using Roxit.ZGW.Besluiten.DataModel;
using Roxit.ZGW.Common.Web.Authorization;

namespace Roxit.ZGW.Besluiten.Web.Authorization;

public static class AuthorizationContextExtensions
{
    /// <summary>
    /// Validates if Zaak is authorized to at least one scope requested in the controller action <see cref="ScopeAttribute"/>.
    /// </summary>
    public static bool IsAuthorized(this AuthorizationContext context, Besluit besluit)
    {
        return context.IsAuthorized(besluit.BesluitType, context.RequestedScopes);
    }

    private static bool IsAuthorized(this AuthorizationContext context, string besluitType, params string[] scopes)
    {
        if (string.IsNullOrEmpty(besluitType))
            throw new ArgumentNullException(nameof(besluitType));

        if (scopes.Length == 0)
            throw new ArgumentNullException(nameof(scopes));

        if (context.Authorization.HasAllAuthorizations)
            return true;

        return context.Authorization.Authorizations.Where(a => a.BesluitType == besluitType).Where(a => scopes.Any(s => a.Scopes.Contains(s))).Any();
    }
}
