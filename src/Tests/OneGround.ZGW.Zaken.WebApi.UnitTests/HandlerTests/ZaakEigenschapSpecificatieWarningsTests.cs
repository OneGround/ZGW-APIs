using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OneGround.ZGW.Catalogi.Contracts.v1;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.BusinessRules;
using OneGround.ZGW.Zaken.Web.Handlers;
using OneGround.ZGW.Zaken.Web.Handlers.v1;
using OneGround.ZGW.Zaken.Web.Handlers.v1._2;
using OneGround.ZGW.Zaken.Web.Notificaties;
using OneGround.ZGW.Zaken.Web.Validators.v1;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests;

// Note: waardenverzameling and kardinaliteit of the eigenschap are not blocking: the value is stored and the caller gets warnings (X-Warning headers).
// The other parts of the specificatie (formaat, lengte) are blocking as before.
public class ZaakEigenschapSpecificatieWarningsTests : IAsyncLifetime
{
    private const string TestOwner = "999993653";
    private const string ZaakTypeUrl = "http://catalogi.local/api/v1/zaaktypen/33333333-3333-3333-3333-333333333333";
    private const string EigenschapUrl = "http://catalogi.local/api/v1/eigenschappen/55555555-5555-5555-5555-555555555555";

    private static readonly Guid ZaakId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ExistingId = new("aaaaaaaa-0000-0000-0000-000000000001");

    private ZrcDbContext _dbContext;
    private Mock<ICatalogiServiceAgent> _catalogiServiceAgentMock;
    private Mock<IClosedZaakModificationBusinessRule> _closedZaakRuleMock;
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

        _dbContext.Zaken.Add(
            new Zaak
            {
                Id = ZaakId,
                Owner = TestOwner,
                Zaaktype = ZaakTypeUrl,
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
        _closedZaakRuleMock = new Mock<IClosedZaakModificationBusinessRule>();
        _auditTrailFactoryMock = new Mock<IAuditTrailFactory>();
        _uriServiceMock = new Mock<IEntityUriService>();
        _notificatieServiceMock = new Mock<INotificatieService>();
        _zaakKenmerkenResolverMock = new Mock<IZaakKenmerkenResolver>();

        _authorizationContextAccessorMock = new Mock<IAuthorizationContextAccessor>();
        _authorizationContextAccessorMock
            .Setup(m => m.AuthorizationContext)
            .Returns(
                new AuthorizationContext(
                    new AuthorizedApplication { HasAllAuthorizations = true, Rsin = TestOwner },
                    [AuthorizationScopes.Zaken.Update]
                )
            );

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string> { { "Application:DontSendNotificaties", "true" }, { "Application:SkipMigrationsAtStartup", "true" } }
            )
            .Build();

        _closedZaakRuleMock.Setup(m => m.ValidateClosedZaakModificationRule(It.IsAny<Zaak>(), It.IsAny<List<ValidationError>>())).Returns(true);
        _auditTrailFactoryMock.Setup(m => m.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>())).Returns(new Mock<IAuditTrailService>().Object);
        _zaakKenmerkenResolverMock
            .Setup(m => m.GetKenmerkenAsync(It.IsAny<Zaak>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>());

        _catalogiServiceAgentMock
            .Setup(m => m.GetZaakTypeByUrlAsync(ZaakTypeUrl))
            .ReturnsAsync(new ServiceAgentResponse<ZaakTypeResponseDto>(new ZaakTypeResponseDto { Eigenschappen = [EigenschapUrl] }));
    }

    public Task DisposeAsync()
    {
        _dbContext?.Dispose();
        return Task.CompletedTask;
    }

    private void SetupEigenschap(EigenschapSpecificatieDto specificatie) =>
        _catalogiServiceAgentMock
            .Setup(m => m.GetEigenschapByUrlAsync(EigenschapUrl))
            .ReturnsAsync(
                new ServiceAgentResponse<EigenschapResponseDto>(
                    new EigenschapResponseDto
                    {
                        Url = EigenschapUrl,
                        Naam = "eigenschap",
                        Specificatie = specificatie,
                    }
                )
            );

    private static EigenschapSpecificatieDto Specificatie(
        string formaat = "tekst",
        string lengte = "20",
        string kardinaliteit = "1",
        params string[] waarden
    ) =>
        new()
        {
            Formaat = formaat,
            Lengte = lengte,
            Kardinaliteit = kardinaliteit,
            Waardenverzameling = waarden,
        };

    private static ZaakEigenschap NewZaakEigenschap(string waarde, Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Owner = TestOwner,
            ZaakId = ZaakId,
            Eigenschap = EigenschapUrl,
            Naam = "eigenschap",
            Waarde = waarde,
        };

    private async Task AddExistingAsync(string waarde = "bestaand")
    {
        _dbContext.ZaakEigenschappen.Add(NewZaakEigenschap(waarde, ExistingId));
        await _dbContext.SaveChangesAsync();
    }

    private Task<CommandResult<ZaakEigenschap>> CreateAsync(string waarde, bool warn) =>
        new CreateZaakEigenschapCommandHandler(
            NullLogger<CreateZaakEigenschapCommandHandler>.Instance,
            _configuration,
            _dbContext,
            _uriServiceMock.Object,
            _notificatieServiceMock.Object,
            _catalogiServiceAgentMock.Object,
            _closedZaakRuleMock.Object,
            _auditTrailFactoryMock.Object,
            _authorizationContextAccessorMock.Object,
            _zaakKenmerkenResolverMock.Object
        ).Handle(
            new CreateZaakEigenschapCommand
            {
                ZaakId = ZaakId,
                ZaakEigenschap = NewZaakEigenschap(waarde),
                WarnOnSpecificatieViolations = warn,
            },
            CancellationToken.None
        );

    private Task<CommandResult<ZaakEigenschap>> UpdateAsync(string waarde, bool warn) =>
        new UpdateZaakEigenschapCommandHandler(
            NullLogger<UpdateZaakEigenschapCommandHandler>.Instance,
            _configuration,
            _dbContext,
            _catalogiServiceAgentMock.Object,
            _uriServiceMock.Object,
            _notificatieServiceMock.Object,
            _closedZaakRuleMock.Object,
            new OneGround.ZGW.Zaken.Web.Handlers.v1._2.EntityUpdaters.ZaakEigenschapUpdater(),
            _auditTrailFactoryMock.Object,
            _authorizationContextAccessorMock.Object,
            _zaakKenmerkenResolverMock.Object
        ).Handle(
            new UpdateZaakEigenschapCommand
            {
                Id = ExistingId,
                ZaakId = ZaakId,
                ZaakEigenschap = NewZaakEigenschap(waarde),
                WarnOnSpecificatieViolations = warn,
            },
            CancellationToken.None
        );

    // ---- validator

    [Theory]
    [InlineData("tekst", "A", new[] { "A", "B" }, 0)]
    [InlineData("tekst", "a", new[] { "A", "B" }, 1)] // Note: text is compared exactly
    [InlineData("tekst", "C", new[] { "A", "B" }, 1)]
    [InlineData("tekst", "C", new string[0], 0)] // Note: an empty waardenverzameling means any value
    [InlineData("getal", "1.5", new[] { "1.50", "2" }, 0)] // Note: numbers are compared as numbers
    [InlineData("getal", "3", new[] { "1.50", "2" }, 1)]
    [InlineData("datum", "20240101", new[] { "20240101" }, 0)]
    [InlineData("datum", "20240102", new[] { "20240101" }, 1)]
    [InlineData("datum_tijd", "20240101120000", new[] { "20240101120000" }, 0)]
    [InlineData("datum_tijd", "20240101120001", new[] { "20240101120000" }, 1)]
    public void GetWarnings_Waardenverzameling(string formaat, string waarde, string[] verzameling, int expectedWarnings)
    {
        var warnings = ZaakEigenschapValidator.GetWarnings(
            NewZaakEigenschap(waarde),
            Specificatie(formaat, "20", "n", verzameling),
            aantalOverigeWaarden: 0
        );

        Assert.Equal(expectedWarnings, warnings.Count);
    }

    [Theory]
    [InlineData("1", 0, 0)]
    [InlineData("1", 1, 1)]
    [InlineData("3", 2, 0)]
    [InlineData("3", 3, 1)]
    [InlineData("n", 100, 0)]
    [InlineData("N", 100, 0)]
    [InlineData("1..n", 5, 0)] // Note: not a number, so not checked
    [InlineData(null, 5, 0)]
    public void GetWarnings_Kardinaliteit(string kardinaliteit, int aantalOverigeWaarden, int expectedWarnings)
    {
        var warnings = ZaakEigenschapValidator.GetWarnings(NewZaakEigenschap("x"), Specificatie("tekst", "20", kardinaliteit), aantalOverigeWaarden);

        Assert.Equal(expectedWarnings, warnings.Count);
    }

    [Fact]
    public void GetWarnings_WithoutSpecificatie_ReturnsNothing()
    {
        Assert.Empty(ZaakEigenschapValidator.GetWarnings(NewZaakEigenschap("x"), null, 5));
    }

    [Fact]
    public void GetWarnings_DoesNotRepeatTheValueInTheWarning()
    {
        var warnings = ZaakEigenschapValidator.GetWarnings(NewZaakEigenschap("geheime-waarde"), Specificatie("tekst", "20", "1", "A"), 0);

        Assert.DoesNotContain(warnings, w => w.Contains("geheime-waarde"));
    }

    // ---- create

    [Fact]
    public async Task Create_ValueNotInWaardenverzameling_StoresValueAndWarns_WhenWarningsAreRequested()
    {
        SetupEigenschap(Specificatie("tekst", "20", "n", "A", "B"));

        var result = await CreateAsync("C", warn: true);

        Assert.Equal(CommandStatus.OK, result.Status);
        Assert.Single(result.Warnings);
        Assert.Equal(1, await _dbContext.ZaakEigenschappen.CountAsync(z => z.Waarde == "C"));
    }

    [Fact]
    public async Task Create_ValueNotInWaardenverzameling_IsSilent_WhenWarningsAreNotRequested()
    {
        SetupEigenschap(Specificatie("tekst", "20", "n", "A", "B"));

        var result = await CreateAsync("C", warn: false);

        Assert.Equal(CommandStatus.OK, result.Status);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task Create_AboveKardinaliteit_WarnsAndStores()
    {
        SetupEigenschap(Specificatie("tekst", "20", "1"));
        await AddExistingAsync();

        var result = await CreateAsync("tweede", warn: true);

        Assert.Equal(CommandStatus.OK, result.Status);
        Assert.Single(result.Warnings);
        Assert.Equal(2, await _dbContext.ZaakEigenschappen.CountAsync());
    }

    [Fact]
    public async Task Create_WithinKardinaliteit_DoesNotWarn()
    {
        SetupEigenschap(Specificatie("tekst", "20", "2"));
        await AddExistingAsync();

        var result = await CreateAsync("tweede", warn: true);

        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task Create_TooLongText_IsStillBlocking_AlsoWhenWarningsAreRequested()
    {
        SetupEigenschap(Specificatie("tekst", "3", "n"));

        var result = await CreateAsync("te lang", warn: true);

        Assert.Equal(CommandStatus.ValidationError, result.Status);
        Assert.Empty(_dbContext.ZaakEigenschappen);
    }

    // ---- update

    [Fact]
    public async Task Update_TheOwnRowDoesNotCountForKardinaliteit()
    {
        SetupEigenschap(Specificatie("tekst", "20", "1"));
        await AddExistingAsync();

        var result = await UpdateAsync("nieuw", warn: true);

        Assert.Equal(CommandStatus.OK, result.Status);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task Update_ValueNotInWaardenverzameling_StoresValueAndWarns()
    {
        SetupEigenschap(Specificatie("tekst", "20", "n", "A", "B"));
        await AddExistingAsync("A");

        var result = await UpdateAsync("C", warn: true);

        Assert.Equal(CommandStatus.OK, result.Status);
        Assert.Single(result.Warnings);
        Assert.Equal("C", (await _dbContext.ZaakEigenschappen.SingleAsync()).Waarde);
    }

    [Fact]
    public async Task Update_WhenTheEigenschapCannotBeRetrieved_IsAValidationError()
    {
        await AddExistingAsync();
        _catalogiServiceAgentMock
            .Setup(m => m.GetEigenschapByUrlAsync(EigenschapUrl))
            .ReturnsAsync(new ServiceAgentResponse<EigenschapResponseDto>(new ErrorResponse { Code = "invalid", Title = "Niet bereikbaar" }));

        var result = await UpdateAsync("nieuw", warn: true);

        Assert.Equal(CommandStatus.ValidationError, result.Status);
    }
}
