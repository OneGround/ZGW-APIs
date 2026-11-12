using OneGround.ZGW.Common.Web.Authorization;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Authorization;

public class AuthorizedApplicationCacheKeyFingerprintTests
{
    private const string TypeA = "https://catalogi.integrationtest.invalid/api/v1/zaaktypen/aaaaaaaa-0000-0000-0000-00000000000a";
    private const string TypeB = "https://catalogi.integrationtest.invalid/api/v1/zaaktypen/bbbbbbbb-0000-0000-0000-00000000000b";

    [Fact]
    public void Permission_order_and_duplicates_do_not_change_the_fingerprint()
    {
        var ab = Restricted(Permission(TypeA, 5), Permission(TypeB, 2));
        var baWithDuplicate = Restricted(Permission(TypeB, 2), Permission(TypeA, 5), Permission(TypeB, 2));

        Assert.Equal(ab.GetCacheKeyFingerprint(), baWithDuplicate.GetCacheKeyFingerprint());
    }

    [Fact]
    public void A_different_type_or_confidentiality_changes_the_fingerprint()
    {
        var baseline = Restricted(Permission(TypeA, 5)).GetCacheKeyFingerprint();

        Assert.NotEqual(baseline, Restricted(Permission(TypeB, 5)).GetCacheKeyFingerprint());
        Assert.NotEqual(baseline, Restricted(Permission(TypeA, 6)).GetCacheKeyFingerprint());
        Assert.NotEqual(baseline, Restricted(Permission(TypeA, 5), Permission(TypeB, 5)).GetCacheKeyFingerprint());
    }

    [Fact]
    public void All_authorizations_differ_from_restricted_and_by_publication_flag()
    {
        var all = new AuthorizedApplication { HasAllAuthorizations = true };
        var allGereed = new AuthorizedApplication { HasAllAuthorizations = true, AlleenIsGereedVoorPublicatie = true };
        var restrictedGereed = Restricted(Permission(TypeA, 5));
        restrictedGereed.AlleenIsGereedVoorPublicatie = true;

        Assert.NotEqual(all.GetCacheKeyFingerprint(), allGereed.GetCacheKeyFingerprint());
        Assert.NotEqual(all.GetCacheKeyFingerprint(), Restricted().GetCacheKeyFingerprint());
        Assert.NotEqual(Restricted(Permission(TypeA, 5)).GetCacheKeyFingerprint(), restrictedGereed.GetCacheKeyFingerprint());
    }

    [Fact]
    public void Missing_authorizations_give_the_same_fingerprint_as_none()
    {
        var missing = new AuthorizedApplication { Authorizations = null };

        Assert.Equal(Restricted().GetCacheKeyFingerprint(), missing.GetCacheKeyFingerprint());
    }

    private static AuthorizedApplication Restricted(params AuthorizationPermission[] permissions) => new() { Authorizations = permissions };

    private static AuthorizationPermission Permission(string zaakType, int maximumVertrouwelijkheid) =>
        new() { ZaakType = zaakType, MaximumVertrouwelijkheidAanduiding = maximumVertrouwelijkheid };
}
