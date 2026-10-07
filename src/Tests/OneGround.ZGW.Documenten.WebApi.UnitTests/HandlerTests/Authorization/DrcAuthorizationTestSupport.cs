using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Handlers;
using OneGround.ZGW.Documenten.WebApi.UnitTests.BusinessRulesTests.v1;
using VertrouwelijkheidAanduiding = OneGround.ZGW.Common.DataModel.VertrouwelijkheidAanduiding;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.HandlerTests.Authorization;

/// <summary>
/// Shared setup for DRC handler tests that check which informatieobjecttypen and vertrouwelijkheidaanduidingen a client can reach.
/// </summary>
internal static class DrcAuthorizationTestSupport
{
    // The handlers compare owners as plain strings, so this is deliberately not an RSIN.
    public const string CurrentTenant = "tenant-current";

    public const string TypeA = "http://catalogi.local/api/v1/informatieobjecttypen/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    public const string TypeB = "http://catalogi.local/api/v1/informatieobjecttypen/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

    public static DrcDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DrcDbContext>().UseInMemoryDatabase($"drc-{Guid.NewGuid()}").Options;
        var context = new UnitTestDrcDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string>
                {
                    { "Application:SkipMigrationsAtStartup", "true" },
                    { "Application:IgnoreInformatieObjectTypeValidation", "true" },
                    { "Application:IgnoreZaakAndBesluitValidation", "true" },
                    { "Application:DontSendNotificaties", "true" },
                    { "Application:EnkelvoudigInformatieObjectenPageSize", "50" },
                    { "Application:CachedSecretExpirationTime", "00:03:00" },
                    { "Application:ResolveForwardedHost", "false" },
                    { "Application:DefaultDocumentenService", "unittest_documentservice" },
                }
            )
            .Build();

    public static AuthorizationPermission Permission(string informatieObjectType, VertrouwelijkheidAanduiding maximum, params string[] scopes) =>
        new()
        {
            InformatieObjectType = informatieObjectType,
            MaximumVertrouwelijkheidAanduiding = (int)maximum,
            Scopes = scopes,
        };

    public static IAuthorizationContextAccessor Client(string[] requestedScopes, params AuthorizationPermission[] permissions)
    {
        var accessor = new Mock<IAuthorizationContextAccessor>();
        accessor
            .Setup(a => a.AuthorizationContext)
            .Returns(new AuthorizationContext(new AuthorizedApplication { Rsin = CurrentTenant, Authorizations = permissions }, requestedScopes));
        return accessor.Object;
    }

    public static async Task<EnkelvoudigInformatieObject> SeedDocumentAsync(
        DrcDbContext context,
        string informatieObjectType,
        VertrouwelijkheidAanduiding vertrouwelijkheid = VertrouwelijkheidAanduiding.openbaar
    )
    {
        var documentId = Guid.NewGuid();
        var versieId = Guid.NewGuid();

        context.EnkelvoudigInformatieObjecten.Add(
            new EnkelvoudigInformatieObject
            {
                Id = documentId,
                Owner = CurrentTenant,
                InformatieObjectType = informatieObjectType,
                LatestVertrouwelijkheidAanduiding = vertrouwelijkheid,
                LatestEnkelvoudigInformatieObjectVersieId = versieId,
                CatalogusId = Guid.NewGuid(),
                CreationTime = DateTime.UtcNow,
            }
        );
        context.EnkelvoudigInformatieObjectVersies.Add(
            new EnkelvoudigInformatieObjectVersie
            {
                Id = versieId,
                Owner = CurrentTenant,
                EnkelvoudigInformatieObjectId = documentId,
                Vertrouwelijkheidaanduiding = vertrouwelijkheid,
                Versie = 1,
                Taal = "nld",
                BeginRegistratie = DateTime.UtcNow,
                Bestandsomvang = 0,
                CreationTime = DateTime.UtcNow,
            }
        );
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        return await context
            .EnkelvoudigInformatieObjecten.Include(e => e.LatestEnkelvoudigInformatieObjectVersie)
            .AsNoTracking()
            .SingleAsync(e => e.Id == documentId);
    }

    /// <summary>Notification building mutates the kenmerken dictionary, so it must never be null.</summary>
    public static Mock<IDocumentKenmerkenResolver> KenmerkenResolver()
    {
        var resolver = new Mock<IDocumentKenmerkenResolver>();
        resolver
            .Setup(r => r.GetKenmerkenAsync(It.IsAny<EnkelvoudigInformatieObject>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new Dictionary<string, string>());
        return resolver;
    }

    public static Mock<IAuditTrailFactory> AuditTrailFactory()
    {
        var factory = new Mock<IAuditTrailFactory>();
        factory.Setup(f => f.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>())).Returns(new Mock<IAuditTrailService>().Object);
        return factory;
    }
}
