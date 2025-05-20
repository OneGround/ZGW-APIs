using System;
using System.Linq;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.Authorization;

public static class AuthorizationContextExtensions
{
    public static bool IsAuthorized(this AuthorizationContext context, EnkelvoudigInformatieObjectVersie informatieObject)
    {
        return context.IsAuthorized(
            informatieObject.EnkelvoudigInformatieObject.InformatieObjectType,
            informatieObject.Vertrouwelijkheidaanduiding,
            context.RequestedScopes
        );
    }

    public static bool IsAuthorized(this AuthorizationContext context, EnkelvoudigInformatieObject informatieObject)
    {
        return context.IsAuthorized(
            informatieObject.InformatieObjectType,
            informatieObject.LatestEnkelvoudigInformatieObjectVersie.Vertrouwelijkheidaanduiding,
            context.RequestedScopes
        );
    }

    public static bool IsAuthorized(this AuthorizationContext context, EnkelvoudigInformatieObject informatieObject, params string[] scopes)
    {
        return context.IsAuthorized(
            informatieObject.InformatieObjectType,
            informatieObject.LatestEnkelvoudigInformatieObjectVersie.Vertrouwelijkheidaanduiding,
            scopes
        );
    }

    public static bool IsForcedUnlockAuthorized(this AuthorizationContext context)
    {
        if (context.Authorization.HasAllAuthorizations)
            return true;

        return context.Authorization.Authorizations.Any(a => a.Scopes.Contains(AuthorizationScopes.Documenten.ForcedUnlock));
    }

    public static bool IsAuthorized(
        this AuthorizationContext context,
        string informatieObjectType,
        VertrouwelijkheidAanduiding? vertrouwelijkheidAanduiding,
        params string[] scopes
    )
    {
        if (string.IsNullOrEmpty(informatieObjectType))
            throw new ArgumentNullException(nameof(informatieObjectType));

        if (scopes.Length == 0)
            throw new ArgumentNullException(nameof(scopes));

        if (context.Authorization.HasAllAuthorizations)
            return true;

        vertrouwelijkheidAanduiding ??= VertrouwelijkheidAanduiding.nullvalue;

        return context
            .Authorization.Authorizations.Where(a => a.InformatieObjectType == informatieObjectType)
            .Where(a => a.MaximumVertrouwelijkheidAanduiding >= (int)vertrouwelijkheidAanduiding)
            .Where(a => scopes.Any(s => a.Scopes.Contains(s)))
            .Any();
    }
}
