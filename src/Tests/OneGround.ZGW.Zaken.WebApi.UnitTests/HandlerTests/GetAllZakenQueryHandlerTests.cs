using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess.Encryption;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Handlers;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;
using OneGround.ZGW.Zaken.Web.Models.v1._5;
using OneGround.ZGW.Zaken.Web.Services;
using Xunit;

// The type ZaakRol lives in a namespace of the same name (…DataModel.ZaakRol.ZaakRol),
// so alias both entities to avoid the namespace/type ambiguity.
using NietNatuurlijkPersoonZaakRol = OneGround.ZGW.Zaken.DataModel.ZaakRol.NietNatuurlijkPersoonZaakRol;
using ZaakRolEntity = OneGround.ZGW.Zaken.DataModel.ZaakRol.ZaakRol;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests;

public class GetAllZakenQueryHandlerTests : IAsyncLifetime
{
    private const string TestOwner = "999993653"; // Official RvIG test RSIN/BSN — safe, never assigned.
    private const string TestNnpId = "test-nnp-id"; // Non-numeric placeholder — not a real RSIN.
    private static readonly Guid ZaakId = new("11111111-1111-1111-1111-111111111111");

    private ZrcDbContext _dbContext;
    private Mock<IAuditTrailFactory> _auditTrailFactoryMock;
    private Mock<IEntityUriService> _uriServiceMock;
    private Mock<IAuthorizationContextAccessor> _authorizationContextAccessorMock;
    private Mock<IDistributedCacheHelper> _cacheMock;
    private Mock<IZaakAuthorizationTempTableService> _tempTableServiceMock;
    private Mock<IZaakKenmerkenResolver> _zaakKenmerkenResolverMock;
    private Mock<IHashRotationService> _hashRotationServiceMock;
    private IConfiguration _configuration;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ZrcDbContext>().UseInMemoryDatabase(databaseName: $"zrc-{Guid.NewGuid()}").Options;
        _dbContext = new UnitTestZrcDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        // Seed one Zaak with a niet-natuurlijk-persoon rol whose InnNnpId matches the filter below.
        _dbContext.Zaken.Add(
            new Zaak
            {
                Id = ZaakId,
                Owner = TestOwner,
                Zaaktype = "http://catalogi.local/api/v1/zaaktypen/33333333-3333-3333-3333-333333333333",
                Bronorganisatie = TestOwner,
                Identificatie = "ZAAK-2024-001",
                Startdatum = DateOnly.FromDateTime(DateTime.UtcNow),
                VertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.openbaar,
                Archiefstatus = ArchiefStatus.nog_te_archiveren,
                Communicatiekanaal = string.Empty,
                Selectielijstklasse = string.Empty,
                VerantwoordelijkeOrganisatie = TestOwner,
                ZaakRollen = new List<ZaakRolEntity>
                {
                    new ZaakRolEntity
                    {
                        Id = Guid.NewGuid(),
                        Owner = TestOwner,
                        BetrokkeneType = BetrokkeneType.niet_natuurlijk_persoon,
                        Betrokkene = string.Empty,
                        RolType = string.Empty,
                        Roltoelichting = string.Empty,
                        Omschrijving = "Initiator",
                        OmschrijvingGeneriek = OmschrijvingGeneriek.initiator,
                        NietNatuurlijkPersoon = new NietNatuurlijkPersoonZaakRol { Id = Guid.NewGuid(), Owner = TestOwner, InnNnpId = TestNnpId },
                    },
                },
            }
        );
        await _dbContext.SaveChangesAsync();

        _auditTrailFactoryMock = new Mock<IAuditTrailFactory>();
        var auditTrailServiceMock = new Mock<IAuditTrailService>();
        _auditTrailFactoryMock.Setup(m => m.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>())).Returns(auditTrailServiceMock.Object);

        _uriServiceMock = new Mock<IEntityUriService>();
        _tempTableServiceMock = new Mock<IZaakAuthorizationTempTableService>();
        _zaakKenmerkenResolverMock = new Mock<IZaakKenmerkenResolver>();
        _hashRotationServiceMock = new Mock<IHashRotationService>();

        _authorizationContextAccessorMock = new Mock<IAuthorizationContextAccessor>();
        _authorizationContextAccessorMock
            .Setup(m => m.AuthorizationContext)
            .Returns(new AuthorizationContext(new AuthorizedApplication { HasAllAuthorizations = true, Rsin = TestOwner }, []));

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { { "Application:DontSendNotificaties", "true" } }!)
            .Build();

        // Cache: run the count factory directly against the in-memory query.
        _cacheMock = new Mock<IDistributedCacheHelper>();
        _cacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<int>>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns((string _, Func<Task<int>> factory, TimeSpan _, CancellationToken _) => factory());
    }

    public Task DisposeAsync() => _dbContext.DisposeAsync().AsTask();

    private GetAllZakenQueryHandler CreateHandler(bool clientExcluded)
    {
        var exclusion = new Mock<IRetrieveAuditClientExclusion>();
        exclusion.Setup(e => e.IsCurrentClientExcluded).Returns(clientExcluded);

        return new GetAllZakenQueryHandler(
            NullLogger<GetAllZakenQueryHandler>.Instance,
            _configuration,
            _uriServiceMock.Object,
            _dbContext,
            _authorizationContextAccessorMock.Object,
            _cacheMock.Object,
            _tempTableServiceMock.Object,
            _zaakKenmerkenResolverMock.Object,
            _hashRotationServiceMock.Object,
            _auditTrailFactoryMock.Object,
            exclusion.Object
        );
    }

    private static GetAllZakenQuery QueryFilteredByNnpId() =>
        new()
        {
            GetAllZakenFilter = new GetAllZakenFilter
            {
                Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId = TestNnpId,
                // The real model binder always populates these IList filter properties (empty when unset in the
                // querystring); the Where() extension calls .Any() on them unconditionally, so they must be
                // non-null here too — unrelated to the exclusion logic under test.
                Uuid__in = [],
                Zaaktype__in = [],
                Archiefnominatie__in = [],
                Archiefstatus__in = [],
                Bronorganisatie__in = [],
            },
            Pagination = new PaginationFilter { Page = 1, Size = 100 },
        };

    [Fact]
    public async Task Person_search_is_not_audit_logged_for_excluded_client()
    {
        var handler = CreateHandler(clientExcluded: true);

        await handler.Handle(QueryFilteredByNnpId(), CancellationToken.None);

        _auditTrailFactoryMock.Verify(f => f.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>()), Times.Never());
    }

    [Fact]
    public async Task Person_search_is_audit_logged_for_non_excluded_client()
    {
        var handler = CreateHandler(clientExcluded: false);

        await handler.Handle(QueryFilteredByNnpId(), CancellationToken.None);

        _auditTrailFactoryMock.Verify(f => f.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>()), Times.Once());
    }
}
