using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using OneGround.ZGW.Zaken.Web.BusinessRules;
using OneGround.ZGW.Zaken.Web.Handlers;
using OneGround.ZGW.Zaken.Web.Handlers.v1;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests;

public class CreateZaakRolCommandHandlerTests : IAsyncLifetime
{
    private const string TestOwner = "813264571";
    private const string TestZaakUrl = "http://zaken.local/api/v1/zaken/11111111-1111-1111-1111-111111111111";
    private const string TestRolTypeUrl = "http://catalogi.local/api/v1/roltypen/22222222-2222-2222-2222-222222222222";
    private const string TestZaakTypeUrl = "http://catalogi.local/api/v1/zaaktypen/33333333-3333-3333-3333-333333333333";

    private static readonly Guid ZaakId = new("11111111-1111-1111-1111-111111111111");

    private ZrcDbContext _dbContext;
    private Mock<ICatalogiServiceAgent> _catalogiServiceAgentMock;
    private Mock<IClosedZaakModificationBusinessRule> _closedZaakModificationBusinessRuleMock;
    private Mock<IAuditTrailFactory> _auditTrailFactoryMock;
    private Mock<IEntityUriService> _uriServiceMock;
    private Mock<IAuthorizationContextAccessor> _authorizationContextAccessorMock;
    private Mock<INotificatieService> _notificatieServiceMock;
    private Mock<IZaakKenmerkenResolver> _zaakKenmerkenResolverMock;
    private IConfiguration _configuration;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ZrcDbContext>().UseInMemoryDatabase(databaseName: $"zrc-{Guid.NewGuid()}").Options;

        _dbContext = new UnitTestZrcDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        // Seed a Zaak with the expected owner
        _dbContext.Zaken.Add(
            new Zaak
            {
                Id = ZaakId,
                Owner = TestOwner,
                Zaaktype = TestZaakTypeUrl,
                Bronorganisatie = TestOwner,
                Identificatie = "ZAAK-2024-001",
                Startdatum = DateOnly.FromDateTime(DateTime.UtcNow),
                VertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.openbaar,
                Archiefstatus = ArchiefStatus.nog_te_archiveren,
                Communicatiekanaal = string.Empty,
                Selectielijstklasse = string.Empty,
                VerantwoordelijkeOrganisatie = TestOwner,
            }
        );
        await _dbContext.SaveChangesAsync();

        _catalogiServiceAgentMock = new Mock<ICatalogiServiceAgent>();
        _closedZaakModificationBusinessRuleMock = new Mock<IClosedZaakModificationBusinessRule>();
        _auditTrailFactoryMock = new Mock<IAuditTrailFactory>();
        _uriServiceMock = new Mock<IEntityUriService>();
        _notificatieServiceMock = new Mock<INotificatieService>();
        _zaakKenmerkenResolverMock = new Mock<IZaakKenmerkenResolver>();

        // Authorization: allow everything
        _authorizationContextAccessorMock = new Mock<IAuthorizationContextAccessor>();
        _authorizationContextAccessorMock
            .Setup(m => m.AuthorizationContext)
            .Returns(new AuthorizationContext(new AuthorizedApplication { HasAllAuthorizations = true, Rsin = TestOwner }, []));

        // Configuration: Application section is required by ZakenBaseHandler
        var inMemorySettings = new Dictionary<string, string>
        {
            { "Application:DontSendNotificaties", "true" },
            { "Application:SkipMigrationsAtStartup", "true" },
        };
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings!).Build();

        // UriService: extract Zaak ID from URL
        _uriServiceMock.Setup(m => m.GetId(TestZaakUrl)).Returns(ZaakId);

        // Catalogi: return valid RolType and ZaakType
        _catalogiServiceAgentMock
            .Setup(m => m.GetRolTypeByUrlAsync(TestRolTypeUrl))
            .ReturnsAsync(
                new ServiceAgentResponse<RolTypeResponseDto>(
                    new RolTypeResponseDto { Omschrijving = "Initiator", OmschrijvingGeneriek = nameof(OmschrijvingGeneriek.initiator) }
                )
            );

        _catalogiServiceAgentMock
            .Setup(m => m.GetZaakTypeByUrlAsync(TestZaakTypeUrl))
            .ReturnsAsync(new ServiceAgentResponse<ZaakTypeResponseDto>(new ZaakTypeResponseDto { RolTypen = new[] { TestRolTypeUrl } }));

        // BusinessRule: closed zaak check passes
        _closedZaakModificationBusinessRuleMock
            .Setup(m => m.ValidateClosedZaakModificationRule(It.IsAny<Zaak>(), It.IsAny<List<ValidationError>>()))
            .Returns(true);

        // AuditTrail: return disposable mock
        var auditTrailServiceMock = new Mock<IAuditTrailService>();
        _auditTrailFactoryMock.Setup(m => m.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>())).Returns(auditTrailServiceMock.Object);

        // ZaakKenmerkenResolver
        _zaakKenmerkenResolverMock
            .Setup(m => m.GetKenmerkenAsync(It.IsAny<Zaak>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { { "bronorganisatie", TestOwner } });
    }

    public Task DisposeAsync()
    {
        _dbContext?.Dispose();
        return Task.CompletedTask;
    }

    private CreateZaakRolCommandHandler CreateSut()
    {
        return new CreateZaakRolCommandHandler(
            NullLogger<CreateZaakRolCommandHandler>.Instance,
            _configuration,
            _dbContext,
            _uriServiceMock.Object,
            _catalogiServiceAgentMock.Object,
            _notificatieServiceMock.Object,
            _closedZaakModificationBusinessRuleMock.Object,
            _auditTrailFactoryMock.Object,
            _authorizationContextAccessorMock.Object,
            _zaakKenmerkenResolverMock.Object
        );
    }

    [Fact]
    public async Task Handle_ValidBsn_SetsHashAndVersion()
    {
        // Arrange — SaveChangesAsync auto-hashes via UnitTestZrcDbContext's mock hasher (returns "hashed-{input}")
        var zaakRol = new ZaakRol
        {
            RolType = TestRolTypeUrl,
            Roltoelichting = "test",
            BetrokkeneType = BetrokkeneType.natuurlijk_persoon,
            NatuurlijkPersoon = new NatuurlijkPersoonZaakRol { InpBsnEncrypted = "999999990" },
        };

        var command = new CreateZaakRolCommand { ZaakUrl = TestZaakUrl, ZaakRol = zaakRol };
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(CommandStatus.OK, result.Status);
        Assert.Equal("hashed-999999990", zaakRol.NatuurlijkPersoon.InpBsnHash);
        Assert.Equal("v1", zaakRol.NatuurlijkPersoon.InpBsnHashKeyVersion);
    }

    [Fact]
    public async Task Handle_NullBsn_DoesNotHash()
    {
        // Arrange
        var zaakRol = new ZaakRol
        {
            RolType = TestRolTypeUrl,
            Roltoelichting = "test",
            BetrokkeneType = BetrokkeneType.natuurlijk_persoon,
            NatuurlijkPersoon = new NatuurlijkPersoonZaakRol { InpBsnEncrypted = null },
        };

        var command = new CreateZaakRolCommand { ZaakUrl = TestZaakUrl, ZaakRol = zaakRol };
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(CommandStatus.OK, result.Status);
        Assert.Null(zaakRol.NatuurlijkPersoon.InpBsnHash);
        Assert.Null(zaakRol.NatuurlijkPersoon.InpBsnHashKeyVersion);
    }

    [Fact]
    public async Task Handle_NoNatuurlijkPersoon_DoesNotHash()
    {
        // Arrange — ZaakRol without any NatuurlijkPersoon
        var zaakRol = new ZaakRol
        {
            RolType = TestRolTypeUrl,
            Roltoelichting = "test",
            BetrokkeneType = BetrokkeneType.medewerker,
        };

        var command = new CreateZaakRolCommand { ZaakUrl = TestZaakUrl, ZaakRol = zaakRol };
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(CommandStatus.OK, result.Status);
    }
}
