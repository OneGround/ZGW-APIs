using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Handlers;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests.Authorization;

/// <summary>
/// Shared setup for ZRC handler tests that check which zaaktypen and vertrouwelijkheidaanduidingen a client can reach.
/// </summary>
internal static class ZrcAuthorizationTestSupport
{
    // The handlers compare owners as plain strings, so these are deliberately not RSINs.
    public const string CurrentTenant = "tenant-current";
    public const string OtherTenant = "tenant-other";

    public const string ZaakTypeA = "http://catalogi.local/api/v1/zaaktypen/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    public const string ZaakTypeB = "http://catalogi.local/api/v1/zaaktypen/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

    public static ZrcDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ZrcDbContext>().UseInMemoryDatabase($"zrc-{Guid.NewGuid()}").Options;
        var context = new UnitTestZrcDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string> { { "Application:DontSendNotificaties", "true" }, { "Application:SkipMigrationsAtStartup", "true" } }
            )
            .Build();

    public static AuthorizationPermission Permission(string zaakType, VertrouwelijkheidAanduiding maximum, params string[] scopes) =>
        new()
        {
            ZaakType = zaakType,
            MaximumVertrouwelijkheidAanduiding = (int)maximum,
            Scopes = scopes,
        };

    public static IAuthorizationContextAccessor Client(string[] requestedScopes, params AuthorizationPermission[] permissions) =>
        Accessor(new AuthorizedApplication { Rsin = CurrentTenant, Authorizations = permissions }, requestedScopes);

    public static IAuthorizationContextAccessor ClientWithAllAuthorizations(params string[] requestedScopes) =>
        Accessor(
            new AuthorizedApplication
            {
                Rsin = CurrentTenant,
                HasAllAuthorizations = true,
                Authorizations = [],
            },
            requestedScopes
        );

    public static Zaak NewZaak(string owner, string zaakType, VertrouwelijkheidAanduiding vertrouwelijkheid = VertrouwelijkheidAanduiding.openbaar) =>
        new()
        {
            Id = Guid.NewGuid(),
            Owner = owner,
            Zaaktype = zaakType,
            Bronorganisatie = owner,
            VerantwoordelijkeOrganisatie = owner,
            Identificatie = $"ZAAK-{Guid.NewGuid():N}",
            Startdatum = DateOnly.FromDateTime(DateTime.UtcNow),
            VertrouwelijkheidAanduiding = vertrouwelijkheid,
            Archiefstatus = ArchiefStatus.nog_te_archiveren,
            Communicatiekanaal = string.Empty,
            Selectielijstklasse = string.Empty,
            Kenmerken = [],
            RelevanteAndereZaken = [],
            Deelzaken = [],
        };

    /// <summary>Notification building mutates the kenmerken dictionary, so it must never be null.</summary>
    public static Mock<IZaakKenmerkenResolver> KenmerkenResolver()
    {
        var resolver = new Mock<IZaakKenmerkenResolver>();
        resolver
            .Setup(r => r.GetKenmerkenAsync(It.IsAny<Zaak>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new Dictionary<string, string>());
        return resolver;
    }

    public static Mock<IAuditTrailFactory> AuditTrailFactory()
    {
        var factory = new Mock<IAuditTrailFactory>();
        factory.Setup(f => f.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>())).Returns(new Mock<IAuditTrailService>().Object);
        return factory;
    }

    private static IAuthorizationContextAccessor Accessor(AuthorizedApplication application, string[] requestedScopes)
    {
        var accessor = new Mock<IAuthorizationContextAccessor>();
        accessor.Setup(a => a.AuthorizationContext).Returns(new AuthorizationContext(application, requestedScopes));
        return accessor.Object;
    }
}
