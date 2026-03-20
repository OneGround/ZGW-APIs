using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Contracts;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Services;
using OneGround.ZGW.Documenten.Web.BusinessRules.v1;
using OneGround.ZGW.Documenten.Web.Handlers;
using OneGround.ZGW.Documenten.Web.Services;
using OneGround.ZGW.Documenten.WebApi.UnitTests.BusinessRulesTests.v1;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.EnkelvoudigeInformatieHandlerTests;

/// <summary>
/// Shared fixture for test setup - created once per test class for better performance
/// </summary>
public class TestMocksFixture : IDisposable
{
    public IConfiguration Configuration { get; }
    public Mock<IOptions<FormOptions>> MockFormOptions { get; }

    public TestMocksFixture()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            { "Application:SkipMigrationsAtStartup", "true" },
            { "Application:IgnoreInformatieObjectTypeValidation", "true" },
            { "Application:IgnoreZaakAndBesluitValidation", "true" },
            { "Application:DontSendNotificaties", "false" },
            { "Application:EnkelvoudigInformatieObjectenPageSize", "50" },
            { "Application:CachedSecretExpirationTime", "00:03:00" },
            { "Application:ResolveForwardedHost", "false" },
            { "Application:DefaultDocumentenService", "unittest_documentservice" },
        };
        Configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        MockFormOptions = new Mock<IOptions<FormOptions>>();
        MockFormOptions.Setup(m => m.Value).Returns(new FormOptions { MultipartBodyLengthLimit = 1 * 1024 * 1024 });
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}

public abstract class EnkelvoudigInformatieObjectVersionsBase<THandler> : IClassFixture<TestMocksFixture>
    where THandler : ZGWBaseHandler
{
    protected readonly TestMocksFixture _fixture;
    protected Mock<MassTransit.Mediator.IMediator> _mockMediator;
    protected Mock<ILogger<THandler>> _mockLogger;
    protected Mock<IEntityUriService> _mockUriService;
    protected Mock<IAuditTrailFactory> _mockedAuditTrailFactory;
    protected Mock<INummerGenerator> _mockNummerGenerator;
    protected Mock<IDocumentService> _mockDocumentService;
    protected Mock<IDocumentServicesResolver> _mockDocumentServicesResolver;
    protected Mock<IEnkelvoudigInformatieObjectBusinessRuleService> _mockEnkvoudigInfObjBusinessRuleService;
    protected Mock<ICatalogiServiceAgent> _mockCatalogiServiceAgent;
    protected Mock<IAuditTrailService> _mockedAuditTrailService;
    protected Mock<IAuditTrailFactory> _mockAuditTrailFactory;
    protected Mock<IAuthorizationContextAccessor> _mockAuthorizationContextAccessor;
    protected Mock<ILockGenerator> _mockLockGenerator;
    protected Mock<INotificatieService> _mockNotificatieService;
    protected Mock<IDocumentKenmerkenResolver> _mockDocumentKenmerkenResolver;
    protected Mock<IEnkelvoudigInformatieObjectMergerFactory> _mockEntityMergerFactory;
    protected DrcDbContext _mockDbContext;
    protected IConfiguration _configuration;
    protected Mock<IOptions<FormOptions>> _mockFormOptions;

    protected EnkelvoudigInformatieObjectVersionsBase(TestMocksFixture fixture)
    {
        _fixture = fixture;
        _configuration = fixture.Configuration;
        _mockFormOptions = fixture.MockFormOptions;
    }

    protected async Task SetupMocksAsync(List<EnkelvoudigInformatieObjectVersie> enkelvoudigeInformatieObjectVersies = null)
    {
        // 1. Mediator
        _mockMediator = new Mock<MassTransit.Mediator.IMediator>();

        // 2. Logger
        _mockLogger = new Mock<ILogger<THandler>>();

        // 4. DbContext - Use unique database per test to avoid parallel test conflicts
        _mockDbContext = new UnitTestDrcDbContext(await GetMockedDrcDbContext(enkelvoudigeInformatieObjectVersies));

        // 5. Uri Service
        _mockUriService = new Mock<IEntityUriService>();

        // 6. Audit Trail Factory
        var mockedAuditTrailservice = new Mock<IAuditTrailService>();
        _mockedAuditTrailFactory = new Mock<IAuditTrailFactory>();
        _mockedAuditTrailFactory.Setup(m => m.Create(It.IsAny<AuditTrailOptions>())).Returns(mockedAuditTrailservice.Object);

        // 7. Nummer Generator
        _mockNummerGenerator = new Mock<INummerGenerator>();
        _mockNummerGenerator
            .Setup(m => m.GenerateAsync(It.IsAny<string>(), "documenten", It.IsAny<Func<string, bool>>(), default))
            .ReturnsAsync($"DOC-{DateTime.Now:HHmmss}");

        // 8. Document Services
        _mockDocumentService = new Mock<IDocumentService>();
        _mockDocumentServicesResolver = new Mock<IDocumentServicesResolver>();
        _mockDocumentServicesResolver.Setup(m => m.GetDefault()).Returns(_mockDocumentService.Object);
        _mockDocumentServicesResolver.Setup(m => m.Find("unittest_documentservice")).Returns(_mockDocumentService.Object);

        // 9. Business Rule Service
        _mockEnkvoudigInfObjBusinessRuleService = new Mock<IEnkelvoudigInformatieObjectBusinessRuleService>();

        // 10. Catalogi Service Agent
        _mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();
        _mockCatalogiServiceAgent
            .Setup(m => m.GetInformatieObjectTypeByUrlAsync(It.IsAny<string>()))
            .ReturnsAsync(new ServiceAgentResponse<InformatieObjectTypeResponseDto>(new InformatieObjectTypeResponseDto()));

        // 11. Audit Trail Service
        _mockedAuditTrailService = new Mock<IAuditTrailService>();

        // 12. Audit Trail Factory (duplicate removed, using from step 6)
        _mockAuditTrailFactory = _mockedAuditTrailFactory;

        // 13. Authorization Context
        _mockAuthorizationContextAccessor = new Mock<IAuthorizationContextAccessor>();
        _mockAuthorizationContextAccessor
            .Setup(m => m.AuthorizationContext)
            .Returns(new AuthorizationContext(new AuthorizedApplication { HasAllAuthorizations = true, Rsin = "813264571" }, []));

        // 14. Lock Generator
        _mockLockGenerator = new Mock<ILockGenerator>();
        _mockLockGenerator.Setup(m => m.Generate()).Returns($"{Guid.NewGuid()}");

        // 15. Notificatie Service
        _mockNotificatieService = new Mock<INotificatieService>();
        _mockNotificatieService.Setup(m => m.NotifyAsync(It.IsAny<Notification>(), default));

        // Document Kenmerken Resolver
        _mockDocumentKenmerkenResolver = new Mock<IDocumentKenmerkenResolver>();
        _mockDocumentKenmerkenResolver
            .Setup(m => m.GetKenmerkenAsync(It.IsAny<EnkelvoudigInformatieObject>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>() { { "bronOrganisatie", "123" } });

        // Entity Merger Factory
        List<ValidationError> errors = new List<ValidationError>();
        var mockEnkelvoudigInformatieObjectMerger = new Mock<IEnkelvoudigInformatieObjectMerger>();
        mockEnkelvoudigInformatieObjectMerger
            .Setup(m => m.TryMergeWithPartial(It.IsAny<object>(), It.IsAny<EnkelvoudigInformatieObject>(), errors))
            .Returns(new EnkelvoudigInformatieObjectVersie());

        _mockEntityMergerFactory = new Mock<IEnkelvoudigInformatieObjectMergerFactory>();
        _mockEntityMergerFactory.Setup(m => m.Create<It.IsAnyType>()).Returns(mockEnkelvoudigInformatieObjectMerger.Object);
    }

    protected async Task<DbContextOptions<DrcDbContext>> GetMockedDrcDbContext(
        List<EnkelvoudigInformatieObjectVersie> enkelvoudigeInformatieObjectVersies = null
    )
    {
        // Use unique database per test to avoid parallel test conflicts
        var options = new DbContextOptionsBuilder<DrcDbContext>()
            .UseInMemoryDatabase(databaseName: $"drc-{Guid.NewGuid()}")
            .EnableSensitiveDataLogging(false) // Disable for performance
            .EnableDetailedErrors(false) // Disable for performance
            .Options;

        // Insert seed data into the database using one instance of the context
        using (var context = new UnitTestDrcDbContext(options))
        {
            // Only create/delete if we have data to seed
            if (enkelvoudigeInformatieObjectVersies != null)
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();

                context.EnkelvoudigInformatieObjecten.Add(
                    new EnkelvoudigInformatieObject
                    {
                        Id = new Guid("f50ef517-8f97-4646-8b6e-02899eb80221"),
                        InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/bddcbdcc-c4ac-45df-984f-eec70134c1d2",
                        EnkelvoudigInformatieObjectVersies = enkelvoudigeInformatieObjectVersies,
                        Owner = "813264571",
                        Lock = "33292",
                        Locked = false,
                        LatestEnkelvoudigInformatieObjectVersie = enkelvoudigeInformatieObjectVersies.Last(),
                    }
                );

                await context.SaveChangesAsync();
            }
            else
            {
                // For tests without seed data, just ensure the schema exists
                await context.Database.EnsureCreatedAsync();
            }
        }

        return options;
    }
}
