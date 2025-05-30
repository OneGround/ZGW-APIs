using System;
using System.Linq;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.Authorization;

public static class AuthorizationContextExtensions
{
    /// <summary>
    /// Validates if Zaak is authorized to at least one scope requested in the controller action <see cref="ScopeAttribute"/>.
    /// </summary>
    public static bool IsAuthorized(this AuthorizationContext context, Zaak zaak)
    {
        return context.IsAuthorized(zaak.Zaaktype, zaak.VertrouwelijkheidAanduiding, context.RequestedScopes);
    }

    /// <summary>
    /// Validates if Zaak is authorized to at least one scope specified in the list.
    /// </summary>
    public static bool IsAuthorized(this AuthorizationContext context, Zaak zaak, params string[] scopes)
    {
        return context.IsAuthorized(zaak.Zaaktype, zaak.VertrouwelijkheidAanduiding, scopes);
    }

    public static bool IsAuthorized(
        this AuthorizationContext context,
        string zaakType,
        VertrouwelijkheidAanduiding vertrouwelijkheidAanduiding,
        params string[] scopes
    )
    {
        if (string.IsNullOrEmpty(zaakType))
            throw new ArgumentNullException(nameof(zaakType));

        if (scopes.Length == 0)
            throw new ArgumentNullException(nameof(scopes));

        if (context.Authorization.HasAllAuthorizations)
            return true;

        return context
            .Authorization.Authorizations.Where(a => a.ZaakType == zaakType)
            .Where(a => a.MaximumVertrouwelijkheidAanduiding >= (int)vertrouwelijkheidAanduiding)
            .Where(a => scopes.Any(s => a.Scopes.Contains(s)))
            .Any();
    }
}
