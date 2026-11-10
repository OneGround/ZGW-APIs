using System;
using System.Collections.Generic;
using System.Linq;
using OneGround.ZGW.Common.Helpers;

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

    /// <summary>
    /// Flag indicating 'Only Ready for publication'.
    /// </summary>
    public bool AlleenIsGereedVoorPublicatie { get; set; }

    private string _cacheKeyFingerprint;

    /// <summary>
    /// Identifies what this application may see, so cached results are only shared between applications with equal rights.
    /// </summary>
    public string GetCacheKeyFingerprint() => _cacheKeyFingerprint ??= ComputeCacheKeyFingerprint();

    private string ComputeCacheKeyFingerprint()
    {
        var publicatie = AlleenIsGereedVoorPublicatie ? "gereed" : "alle";
        if (HasAllAuthorizations)
            return $"all:{publicatie}";

        var permissions = (Authorizations ?? [])
            .Select(a => $"{a.ZaakType}|{a.InformatieObjectType}|{a.BesluitType}|{a.MaximumVertrouwelijkheidAanduiding}")
            .Distinct()
            .Order(StringComparer.Ordinal);

        return $"{publicatie}:{ObjectHasher.ComputeSha1Hash(string.Join(';', permissions))}";
    }
}
