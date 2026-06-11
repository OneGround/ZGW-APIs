using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.DataModel.Authorization;
using OneGround.ZGW.Documenten.Web.Handlers;
using OneGround.ZGW.Documenten.Web.Handlers.v1;
using OneGround.ZGW.Documenten.Web.Models.v1;
using OneGround.ZGW.Documenten.Web.Services;
using OneGround.ZGW.Documenten.WebApi.UnitTests.BusinessRulesTests.v1;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.HandlerTests;

/// <summary>
/// Regression tests for the pre-4 owner filter on the versie subquery in
/// GetAllObjectInformatieObjectenQueryHandler (Handlers/v1/GetAllObjectInformatieObjectenQuery.cs).
///
/// Bug: the inner .Any(v => v.Id == ... &amp;&amp; auth check) had no v.Owner == _rsin guard.
/// Without it the planner probes all 70M versie rows for authorization, and — more
/// critically — an OIO whose EIO's LatestVersieId accidentally resolves to a versie
/// owned by another tenant can produce a false-positive auth result.
///
/// Fix: add v.Owner == _rsin as the FIRST predicate inside the Any() call so the
/// database can use the (Owner, Id, Vertrouwelijkheidaanduiding) covering index.
/// </summary>
public class GetAllObjectInformatieObjectenQueryHandlerTests
{
    private const string RsinA = "111111111";
    private const string RsinB = "222222222";
    private const string InformatieObjectTypeX = "http://catalogi.example.com/informatieobjecttypen/type-x";

    /// <summary>
    /// Regression test (OIO variant): RSIN_A's EIO has LatestVersieId that points to
    /// RSIN_B's versie (openbaar). Without v.Owner == _rsin in the subquery the handler
    /// returns the OIO because it finds RSIN_B's versie satisfying the auth check —
    /// a cross-tenant false positive. With the fix the subquery only matches versies
    /// owned by RSIN_A, so no versie is found and the OIO is correctly excluded.
    /// </summary>
    [Fact]
    public async Task Handle_WithAuthorizationFilter_DoesNotMatchVersieFromDifferentOwner_OIO()
    {
        // Arrange
        using var ctx = BuildInMemoryDrcContext();

        // RSIN_B has a versie with vha=openbaar that WOULD pass the auth check.
        var rsinBVersieId = Guid.NewGuid();
        var rsinBEioId = Guid.NewGuid();
        ctx.EnkelvoudigInformatieObjecten.Add(
            new EnkelvoudigInformatieObject
            {
                Id = rsinBEioId,
                Owner = RsinB,
                InformatieObjectType = InformatieObjectTypeX,
                LatestEnkelvoudigInformatieObjectVersieId = rsinBVersieId,
                CatalogusId = Guid.NewGuid(),
                CreationTime = DateTime.UtcNow,
            }
        );
        ctx.EnkelvoudigInformatieObjectVersies.Add(
            new EnkelvoudigInformatieObjectVersie
            {
                Id = rsinBVersieId,
                Owner = RsinB,
                EnkelvoudigInformatieObjectId = rsinBEioId,
                Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar, // would pass auth
                Versie = 1,
                Taal = "nld",
                BeginRegistratie = DateTime.UtcNow,
                Bestandsomvang = 0,
                CreationTime = DateTime.UtcNow,
            }
        );

        // RSIN_A has an EIO whose LatestVersieId points to RSIN_B's versie ID.
        // (Without the owner filter the subquery resolves rsinBVersieId → finds
        // RSIN_B's openbaar versie → auth passes → OIO wrongly returned.)
        var rsinAEioId = Guid.NewGuid();
        ctx.EnkelvoudigInformatieObjecten.Add(
            new EnkelvoudigInformatieObject
            {
                Id = rsinAEioId,
                Owner = RsinA,
                InformatieObjectType = InformatieObjectTypeX,
                // Deliberately point to RSIN_B's versie to expose the missing owner filter.
                LatestEnkelvoudigInformatieObjectVersieId = rsinBVersieId,
                CatalogusId = Guid.NewGuid(),
                CreationTime = DateTime.UtcNow,
            }
        );
        ctx.ObjectInformatieObjecten.Add(
            new ObjectInformatieObject
            {
                Id = Guid.NewGuid(),
                Owner = RsinA,
                InformatieObjectId = rsinAEioId,
                Object = "https://zaakobject.example.com/zaken/1",
                ObjectType = ObjectType.zaak,
                CreationTime = DateTime.UtcNow,
            }
        );

        await ctx.SaveChangesAsync();

        // Auth allows typeX up to openbaar.
        await SeedAuthTempTableAsync(ctx, maxVha: VertrouwelijkheidAanduiding.openbaar);

        var handler = BuildHandler(ctx, currentRsin: RsinA, hasAllAuthorizations: false);

        // Act
        var result = await handler.Handle(
            new GetAllObjectInformatieObjectenQuery { GetAllObjectInformatieObjectenFilter = new GetAllObjectInformatieObjectenFilter() },
            CancellationToken.None
        );

        // Assert: RSIN_A's EIO has no own versie satisfying the auth check (only
        // RSIN_B's versie matches the id, but the owner filter excludes it).
        // The OIO must NOT be returned.
        Assert.Empty(result.Result);
    }

    /// <summary>
    /// Happy-path (OIO variant): RSIN_A has an EIO with its own versie (openbaar, within
    /// auth threshold). The handler must return that OIO and must NOT return RSIN_B's OIO.
    /// </summary>
    [Fact]
    public async Task Handle_WithAuthorizationFilter_ReturnsOnlyOwnTenantItems_OIO()
    {
        // Arrange: two tenants, each with EIO + versie(openbaar) + OIO.
        using var ctx = BuildInMemoryDrcContext();
        await SeedTwoTenantsAsync(ctx);
        await SeedAuthTempTableAsync(ctx, maxVha: VertrouwelijkheidAanduiding.openbaar);

        var handler = BuildHandler(ctx, currentRsin: RsinA, hasAllAuthorizations: false);

        // Act
        var result = await handler.Handle(
            new GetAllObjectInformatieObjectenQuery { GetAllObjectInformatieObjectenFilter = new GetAllObjectInformatieObjectenFilter() },
            CancellationToken.None
        );

        // Assert: one item returned, belonging to RSIN_A only.
        Assert.Single(result.Result);
        Assert.All(result.Result, oio => Assert.Equal(RsinA, oio.InformatieObject.Owner));
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static UnitTestDrcDbContext BuildInMemoryDrcContext()
    {
        var options = new DbContextOptionsBuilder<DrcDbContext>().UseInMemoryDatabase(databaseName: $"drc-oio-{Guid.NewGuid()}").Options;

        var ctx = new UnitTestDrcDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    /// <summary>
    /// Seeds EIO + versie (openbaar) + OIO for both RSIN_A and RSIN_B with
    /// correctly owned versies.
    /// </summary>
    private static async Task SeedTwoTenantsAsync(UnitTestDrcDbContext ctx)
    {
        int counter = 0;
        foreach (var rsin in new[] { RsinA, RsinB })
        {
            counter++;
            var eioId = Guid.NewGuid();
            var versieId = Guid.NewGuid();

            ctx.EnkelvoudigInformatieObjecten.Add(
                new EnkelvoudigInformatieObject
                {
                    Id = eioId,
                    Owner = rsin,
                    InformatieObjectType = InformatieObjectTypeX,
                    LatestEnkelvoudigInformatieObjectVersieId = versieId,
                    CatalogusId = Guid.NewGuid(),
                    CreationTime = DateTime.UtcNow,
                }
            );

            ctx.EnkelvoudigInformatieObjectVersies.Add(
                new EnkelvoudigInformatieObjectVersie
                {
                    Id = versieId,
                    Owner = rsin,
                    EnkelvoudigInformatieObjectId = eioId,
                    Vertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar,
                    Versie = 1,
                    Taal = "nld",
                    BeginRegistratie = DateTime.UtcNow,
                    Bestandsomvang = 0,
                    CreationTime = DateTime.UtcNow,
                }
            );

            ctx.ObjectInformatieObjecten.Add(
                new ObjectInformatieObject
                {
                    Id = Guid.NewGuid(),
                    Owner = rsin,
                    InformatieObjectId = eioId,
                    Object = $"https://zaakobject.example.com/zaken/{counter}",
                    ObjectType = ObjectType.zaak,
                    CreationTime = DateTime.UtcNow,
                }
            );
        }

        await ctx.SaveChangesAsync();
    }

    /// <summary>
    /// Directly adds TempInformatieObjectAuthorization rows to the in-memory context
    /// (bypassing the real service which uses raw CREATE TEMPORARY TABLE SQL that is
    /// incompatible with the EF InMemory provider).
    /// </summary>
    private static async Task SeedAuthTempTableAsync(UnitTestDrcDbContext ctx, VertrouwelijkheidAanduiding maxVha)
    {
        ctx.TempInformatieObjectAuthorization.Add(
            new TempInformatieObjectAuthorization { InformatieObjectType = InformatieObjectTypeX, MaximumVertrouwelijkheidAanduiding = (int)maxVha }
        );
        await ctx.SaveChangesAsync();
    }

    private static GetAllObjectInformatieObjectenQueryHandler BuildHandler(UnitTestDrcDbContext ctx, string currentRsin, bool hasAllAuthorizations)
    {
        var configuration = new ConfigurationBuilder()
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

        var authContextAccessor = new Mock<IAuthorizationContextAccessor>();
        authContextAccessor
            .Setup(m => m.AuthorizationContext)
            .Returns(
                new AuthorizationContext(
                    new AuthorizedApplication
                    {
                        HasAllAuthorizations = hasAllAuthorizations,
                        Rsin = currentRsin,
                        Authorizations = [],
                    },
                    []
                )
            );

        // The real service uses ExecuteSqlRawAsync (incompatible with EF InMemory).
        // The auth table is seeded directly by SeedAuthTempTableAsync; this mock is a no-op.
        var tempTableService = new Mock<IInformatieObjectAuthorizationTempTableService>();
        tempTableService
            .Setup(s =>
                s.InsertInformatieObjectTypeAuthorizationsToTempTableAsync(
                    It.IsAny<AuthorizationContext>(),
                    It.IsAny<DrcDbContext>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        // NullLogger avoids Moq/Castle proxy issues with internal generic type parameters.
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<GetAllObjectInformatieObjectenQueryHandler>.Instance;
        var uriService = new Mock<IEntityUriService>();
        var documentKenmerkenResolver = new Mock<IDocumentKenmerkenResolver>();

        return new GetAllObjectInformatieObjectenQueryHandler(
            logger: logger,
            configuration: configuration,
            uriService: uriService.Object,
            context: ctx,
            authorizationContextAccessor: authContextAccessor.Object,
            informatieObjectAuthorizationTempTableService: tempTableService.Object,
            documentKenmerkenResolver: documentKenmerkenResolver.Object
        );
    }
}
