using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.DataModel.Authorization;
using OneGround.ZGW.Documenten.Web.Handlers;
using OneGround.ZGW.Documenten.Web.Handlers.v1._5;
using OneGround.ZGW.Documenten.Web.Models.v1._5;
using OneGround.ZGW.Documenten.Web.Services;
using OneGround.ZGW.Documenten.WebApi.UnitTests.BusinessRulesTests.v1;
using Xunit;
using VertrouwelijkheidAanduiding = OneGround.ZGW.Common.DataModel.VertrouwelijkheidAanduiding;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.HandlerTests;

/// <summary>
/// Tests for authorization filtering in GetAllVerzendingenQueryHandler
/// (Handlers/v1/5/GetAllVerzendingenQueryHandler.cs).
///
/// The handler filters on the denormalized LatestVertrouwelijkheidAanduiding on the EIO (reached via
/// the Verzending → InformatieObject navigation), combined with the owner (rsin) filter. These tests
/// cover: (1) an item whose VHA exceeds the authorized maximum for its type is excluded, and
/// (2) only the current tenant's items are returned.
///
/// Note: the real null-VHA → excluded behaviour relies on SQL UNKNOWN semantics and can only be
/// verified against PostgreSQL; the EF InMemory provider evaluates the predicate as plain C# (so a
/// null .Value would throw), which is why the seeded EIOs always set LatestVertrouwelijkheidAanduiding.
/// </summary>
public class GetAllVerzendingenQueryHandlerTests
{
    private const string RsinA = "111111111";
    private const string RsinB = "222222222";
    private const string InformatieObjectTypeX = "http://catalogi.example.com/informatieobjecttypen/type-x";

    /// <summary>
    /// Auth filtering on the denormalized EIO column: a Verzending whose EIO's
    /// LatestVertrouwelijkheidAanduiding exceeds the authorized maximum for its type must be excluded.
    /// Here auth allows typeX only up to openbaar, but RSIN_A's EIO is vertrouwelijk, so its Verzending
    /// must NOT be returned. (Replaces an earlier test whose premise — resolving the VHA via a
    /// cross-tenant versie subquery — is obsolete now that the handler reads the denormalized
    /// LatestVertrouwelijkheidAanduiding directly on the EIO.)
    /// </summary>
    [Fact]
    public async Task Handle_WithAuthorizationFilter_ExcludesItemWhoseVhaExceedsAuthThreshold_Verzending()
    {
        // Arrange
        using var ctx = BuildInMemoryDrcContext();

        // RSIN_A's EIO is typeX but classified 'vertrouwelijk' — above the authorized 'openbaar'.
        var rsinAEioId = Guid.NewGuid();
        ctx.EnkelvoudigInformatieObjecten.Add(
            new EnkelvoudigInformatieObject
            {
                Id = rsinAEioId,
                Owner = RsinA,
                InformatieObjectType = InformatieObjectTypeX,
                LatestVertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.vertrouwelijk,
                CatalogusId = Guid.NewGuid(),
                CreationTime = DateTime.UtcNow,
            }
        );
        ctx.Verzendingen.Add(
            new Verzending
            {
                Id = Guid.NewGuid(),
                InformatieObjectId = rsinAEioId,
                Betrokkene = "https://betrokkene.example.com/1",
                AardRelatie = AardRelatie.afzender,
                Contactpersoon = "https://contactpersoon.example.com/1",
                CreationTime = DateTime.UtcNow,
            }
        );

        await ctx.SaveChangesAsync();

        // Auth allows typeX only up to openbaar (vertrouwelijk is above this threshold).
        await SeedAuthTempTableAsync(ctx, maxVha: VertrouwelijkheidAanduiding.openbaar);

        var handler = BuildHandler(ctx, currentRsin: RsinA, hasAllAuthorizations: false);

        // Act
        var result = await handler.Handle(
            new GetAllVerzendingenQuery
            {
                GetAllVerzendingenFilter = new GetAllVerzendingenFilter(),
                Pagination = new OneGround.ZGW.Common.Web.Models.PaginationFilter { Page = 1, Size = 100 },
            },
            CancellationToken.None
        );

        // Assert: the EIO's VHA (vertrouwelijk) exceeds the authorized maximum (openbaar),
        // so the Verzending must NOT be returned.
        Assert.Empty(result.Result.PageResult);
    }

    /// <summary>
    /// Happy-path: RSIN_A has an EIO with its own versie (openbaar, within auth threshold).
    /// When HasAllAuthorizations is false the handler must return that Verzending and must NOT
    /// return RSIN_B's Verzending (same typeX + openbaar).
    /// </summary>
    [Fact]
    public async Task Handle_WithAuthorizationFilter_ReturnsOnlyOwnTenantItems_Verzending()
    {
        // Arrange: two tenants, each with EIO + versie(openbaar) + Verzending.
        using var ctx = BuildInMemoryDrcContext();
        await SeedTwoTenantsAsync(ctx);
        await SeedAuthTempTableAsync(ctx, maxVha: VertrouwelijkheidAanduiding.openbaar);

        var handler = BuildHandler(ctx, currentRsin: RsinA, hasAllAuthorizations: false);

        // Act
        var result = await handler.Handle(
            new GetAllVerzendingenQuery
            {
                GetAllVerzendingenFilter = new GetAllVerzendingenFilter(),
                Pagination = new OneGround.ZGW.Common.Web.Models.PaginationFilter { Page = 1, Size = 100 },
            },
            CancellationToken.None
        );

        // Assert: one item returned, belonging to RSIN_A only.
        Assert.Single(result.Result.PageResult);
        Assert.All(result.Result.PageResult, v => Assert.Equal(RsinA, v.InformatieObject.Owner));
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static UnitTestDrcDbContext BuildInMemoryDrcContext()
    {
        var options = new DbContextOptionsBuilder<DrcDbContext>().UseInMemoryDatabase(databaseName: $"drc-verzendingen-{Guid.NewGuid()}").Options;

        var ctx = new UnitTestDrcDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    /// <summary>
    /// Seeds EIO + versie (openbaar) + Verzending for both RSIN_A and RSIN_B with
    /// correctly owned versies (each EIO's LatestVersieId points to its own versie).
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
                    LatestVertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.openbaar,
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

            ctx.Verzendingen.Add(
                new Verzending
                {
                    Id = Guid.NewGuid(),
                    InformatieObjectId = eioId,
                    Betrokkene = $"https://betrokkene.example.com/{counter}",
                    AardRelatie = AardRelatie.afzender,
                    Contactpersoon = $"https://contactpersoon.example.com/{counter}",
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

    private static GetAllVerzendingenQueryHandler BuildHandler(UnitTestDrcDbContext ctx, string currentRsin, bool hasAllAuthorizations)
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

        // The Verzendingen handler's GetAuthorizationCountCachedAsync uses ExecuteSqlRawAsync
        // (SET LOCAL enable_nestloop = off) inside the cache factory, which is incompatible
        // with the EF InMemory provider. The NoOpDistributedCacheHelper returns a default(T)
        // without calling the factory so the ExecuteSqlRawAsync path is never reached.
        // Since the tests only assert on PageResult, not on Count, default(int) = 0 is fine.
        var cache = new NoOpDistributedCacheHelper();

        // NullLogger avoids Moq/Castle proxy issues with internal generic type parameters.
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<GetAllVerzendingenQueryHandler>.Instance;
        var uriService = new Mock<IEntityUriService>();
        var documentKenmerkenResolver = new Mock<IDocumentKenmerkenResolver>();

        return new GetAllVerzendingenQueryHandler(
            logger: logger,
            configuration: configuration,
            uriService: uriService.Object,
            context: ctx,
            authorizationContextAccessor: authContextAccessor.Object,
            cache: cache,
            informatieObjectAuthorizationTempTableService: tempTableService.Object,
            documentKenmerkenResolver: documentKenmerkenResolver.Object
        );
    }
}

/// <summary>
/// Test-only IDistributedCacheHelper that returns default(T) without calling the factory.
/// This avoids hitting ExecuteSqlRawAsync (incompatible with EF InMemory) inside the
/// GetAuthorizationCountCachedAsync factory lambda in GetAllVerzendingenQueryHandler.
/// </summary>
internal sealed class NoOpDistributedCacheHelper : IDistributedCacheHelper
{
    public Task<T> GetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan absoluteExpirationRelativeToNow,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(default(T));
}
