using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Roxit.ZGW.Catalogi.ServiceAgent.v1;
using Roxit.ZGW.Common.Contracts;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Handlers;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Documenten.Services;
using Roxit.ZGW.Documenten.Web.BusinessRules.v1;
using Roxit.ZGW.Documenten.Web.Services;
using Roxit.ZGW.Documenten.WebApi.UnitTests.BusinessRulesTests.v1;

namespace Roxit.ZGW.Documenten.WebApi.UnitTests.EnkelvoudigeInformatieHandlerTests;

public abstract class EnkelvoudigInformatieObjectVersionsBase<THandler>
    where THandler : ZGWBaseHandler
{
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
    protected DrcDbContext _mockDbContext;
    protected IConfiguration _configuration;
    protected Mock<IOptions<FormOptions>> _mockFormOptions;

    protected async Task SetupMocksAsync(List<EnkelvoudigInformatieObjectVersie> enkelvoudigeInformatieObjectVersies = null)
    {
        // 1.
        _mockMediator = new Mock<MassTransit.Mediator.IMediator>();

        // 2.
        _mockLogger = new Mock<ILogger<THandler>>();

        // 3.
        var inMemorySettings = new Dictionary<string, string>
        {
            { "Application:SkipMigrationsAtStartup", "true" },
            { "Application:DontCheckServerValidation", "false" },
            { "Application:IgnoreInformatieObjectTypeValidation", "true" },
            { "Application:IgnoreZaakAndBesluitValidation", "true" },
            { "Application:DontSendNotificaties", "false" },
            { "Application:EnkelvoudigInformatieObjectenPageSize", "50" },
            { "Application:CachedSecretExpirationTime", "00:03:00" },
            { "Application:AuthorizedRsins", "813264571" },
            { "Application:ResolveForwardedHost", "false" },
            { "Application:DefaultDocumentenService", "unittest_documentservice" },
        };
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        _mockFormOptions = new Mock<IOptions<FormOptions>>();
        _mockFormOptions.Setup(m => m.Value).Returns(new FormOptions { MultipartBodyLengthLimit = 1 * 1024 * 1024 });

        // 4.
        _mockDbContext = new UnitTestDrcDbContext(await GetMockedDrcDbContext(enkelvoudigeInformatieObjectVersies));

        // 5.
        _mockUriService = new Mock<IEntityUriService>();

        // 6.
        var mockedAuditTrailservice = new Mock<IAuditTrailService>();

        _mockedAuditTrailFactory = new Mock<IAuditTrailFactory>();
        _mockedAuditTrailFactory.Setup(m => m.Create(It.IsAny<AuditTrailOptions>())).Returns(mockedAuditTrailservice.Object);

        // 7.
        _mockNummerGenerator = new Mock<INummerGenerator>();
        _mockNummerGenerator
            .Setup(m => m.GenerateAsync(It.IsAny<string>(), "documenten", It.IsAny<Func<string, bool>>(), default))
            .ReturnsAsync($"DOC-{DateTime.Now:HHmmss}");

        // 8.
        _mockDocumentService = new Mock<IDocumentService>();

        _mockDocumentServicesResolver = new Mock<IDocumentServicesResolver>();
        _mockDocumentServicesResolver.Setup(m => m.GetDefault()).Returns(_mockDocumentService.Object);
        _mockDocumentServicesResolver.Setup(m => m.Find("unittest_documentservice")).Returns(_mockDocumentService.Object);

        // 9.
        _mockEnkvoudigInfObjBusinessRuleService = new Mock<IEnkelvoudigInformatieObjectBusinessRuleService>();

        // 10.
        _mockCatalogiServiceAgent = new Mock<ICatalogiServiceAgent>();

        // 11.
        _mockedAuditTrailService = new Mock<IAuditTrailService>();

        // 12.
        _mockAuditTrailFactory = new Mock<IAuditTrailFactory>();
        _mockAuditTrailFactory.Setup(m => m.Create(It.IsAny<AuditTrailOptions>())).Returns(_mockedAuditTrailService.Object);

        // 13.
        _mockAuthorizationContextAccessor = new Mock<IAuthorizationContextAccessor>();
        _mockAuthorizationContextAccessor
            .Setup(m => m.AuthorizationContext)
            .Returns(new AuthorizationContext(new AuthorizedApplication { HasAllAuthorizations = true, Rsin = "813264571" }, []));

        // 14.
        _mockLockGenerator = new Mock<ILockGenerator>();
        _mockLockGenerator.Setup(m => m.Generate()).Returns($"{Guid.NewGuid()}");

        // 15.
        _mockNotificatieService = new Mock<INotificatieService>();
        _mockNotificatieService.Setup(m => m.NotifyAsync(It.IsAny<Notification>(), default));
    }

    protected async Task<DbContextOptions<DrcDbContext>> GetMockedDrcDbContext(
        List<EnkelvoudigInformatieObjectVersie> enkelvoudigeInformatieObjectVersies = null
    )
    {
        var options = new DbContextOptionsBuilder<DrcDbContext>().UseInMemoryDatabase(databaseName: $"drc-{Guid.NewGuid()}").Options;

        // Insert seed data into the database using one instance of the context
        using (var context = new UnitTestDrcDbContext(options))
        {
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            if (enkelvoudigeInformatieObjectVersies != null)
            {
                context.EnkelvoudigInformatieObjecten.Add(
                    new EnkelvoudigInformatieObject
                    {
                        Id = new Guid("f50ef517-8f97-4646-8b6e-02899eb80221"),
                        InformatieObjectType = "http://catalogi.user.local:5011/api/v1/informatieobjecttypen/bddcbdcc-c4ac-45df-984f-eec70134c1d2",
                        EnkelvoudigInformatieObjectVersies = enkelvoudigeInformatieObjectVersies,
                        Owner = "813264571",
                        Lock = null,
                        Locked = false,
                        LatestEnkelvoudigInformatieObjectVersie = enkelvoudigeInformatieObjectVersies.Last(),
                    }
                );

                await context.SaveChangesAsync();
            }
        }

        return options;
    }
}
