using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.BusinessRules;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Catalogi.Web.Services;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Referentielijsten.ServiceAgent;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.HandlerTests;

public class UpdateZaakTypeCommandHandlerTests
{
    private const string TestOwner = "111111111";
    private const string TestCatalogusUrl = "http://catalogi.local/api/v1/catalogussen/22222222-2222-2222-2222-222222222222";

    [Fact]
    public async Task Handle_BesluitTypeOmschrijvingNotFoundInCatalog_CreatesSoftReferencedRelation()
    {
        // Arrange
        var catalogusId = Guid.NewGuid();
        var zaakTypeId = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<ZtcDbContext>().UseInMemoryDatabase(databaseName: $"ztc-{Guid.NewGuid()}").Options;
        var dbContext = new UnitTestZtcDbContext(options);

        var catalogus = new Catalogus
        {
            Id = catalogusId,
            Owner = TestOwner,
            Domein = "TST",
            Rsin = TestOwner,
            ContactpersoonBeheerNaam = "Test",
        };

        var zaakType = new ZaakType
        {
            Id = zaakTypeId,
            Owner = TestOwner,
            CatalogusId = catalogusId,
            Catalogus = catalogus,
            Doel = "Test",
            Aanleiding = "Test",
            HandelingInitiator = "Test",
            Onderwerp = "Test",
            HandelingBehandelaar = "Test",
            Concept = true,
            SelectielijstProcestype = null,
            ZaakTypeBesluitTypen = [],
            ZaakTypeDeelZaakTypen = [],
            ZaakTypeGerelateerdeZaakTypen = [],
        };

        dbContext.Catalogussen.Add(catalogus);
        dbContext.ZaakTypen.Add(zaakType);
        await dbContext.SaveChangesAsync();

        var uriServiceMock = new Mock<IEntityUriService>();
        uriServiceMock.Setup(m => m.GetId(TestCatalogusUrl)).Returns(catalogusId);

        var authorizationContextAccessorMock = new Mock<IAuthorizationContextAccessor>();
        authorizationContextAccessorMock
            .Setup(m => m.AuthorizationContext)
            .Returns(new AuthorizationContext(new AuthorizedApplication { HasAllAuthorizations = true, Rsin = TestOwner }, []));

        var conceptBusinessRuleMock = new Mock<IConceptBusinessRule>();
        conceptBusinessRuleMock
            .Setup(m => m.ValidateGeldigheid(It.IsAny<List<IConceptEntity>>(), It.IsAny<IConceptEntity>(), It.IsAny<List<ValidationError>>()))
            .Returns(true);

        var zaakTypeDataServiceMock = new Mock<IZaakTypeDataService>();
        zaakTypeDataServiceMock
            .Setup(m => m.GetAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zaakType);

        var auditTrailServiceMock = new Mock<IAuditTrailService>();
        var auditTrailFactoryMock = new Mock<IAuditTrailFactory>();
        auditTrailFactoryMock.Setup(m => m.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>())).Returns(auditTrailServiceMock.Object);

        var inMemorySettings = new Dictionary<string, string> { { "Application:DontSendNotificaties", "true" } };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings!).Build();

        var sut = new UpdateZaakTypeCommandHandler(
            NullLogger<UpdateZaakTypeCommandHandler>.Instance,
            configuration,
            dbContext,
            Mock.Of<INotificatieService>(),
            uriServiceMock.Object,
            Mock.Of<IEntityUpdater<ZaakType>>(),
            conceptBusinessRuleMock.Object,
            Mock.Of<IReferentielijstenServiceAgent>(),
            authorizationContextAccessorMock.Object,
            Mock.Of<ICacheInvalidator>(),
            auditTrailFactoryMock.Object,
            zaakTypeDataServiceMock.Object
        );

        var command = new UpdateZaakTypeCommand
        {
            Id = zaakTypeId,
            Catalogus = TestCatalogusUrl,
            IsPartialUpdate = true,
            DeelZaakTypen = [],
            BesluitTypen = ["ZZZ-Unique-Test"],
            ZaakType = new ZaakType
            {
                Concept = true,
                SelectielijstProcestype = null,
                ZaakTypeGerelateerdeZaakTypen = [],
            },
        };

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(CommandStatus.OK, result.Status);

        var persistedRelations = await dbContext.ZaakTypeBesluitTypen.Where(z => z.ZaakTypeId == zaakTypeId).ToListAsync();

        var relation = Assert.Single(persistedRelations);
        Assert.Equal("ZZZ-Unique-Test", relation.BesluitTypeOmschrijving);
        Assert.Null(relation.BesluitType); // soft reference: navigation stays unresolved until a matching besluittype exists
    }

    [Fact]
    public async Task Handle_BesluitTypeOmschrijvingFoundInCatalog_CreatesRelation()
    {
        // Arrange
        var catalogusId = Guid.NewGuid();
        var zaakTypeId = Guid.NewGuid();

        var options = new DbContextOptionsBuilder<ZtcDbContext>().UseInMemoryDatabase(databaseName: $"ztc-{Guid.NewGuid()}").Options;
        var dbContext = new UnitTestZtcDbContext(options);

        var catalogus = new Catalogus
        {
            Id = catalogusId,
            Owner = TestOwner,
            Domein = "TST",
            Rsin = TestOwner,
            ContactpersoonBeheerNaam = "Test",
        };

        var zaakType = new ZaakType
        {
            Id = zaakTypeId,
            Owner = TestOwner,
            CatalogusId = catalogusId,
            Catalogus = catalogus,
            Doel = "Test",
            Aanleiding = "Test",
            HandelingInitiator = "Test",
            Onderwerp = "Test",
            HandelingBehandelaar = "Test",
            Concept = true,
            SelectielijstProcestype = null,
            ZaakTypeBesluitTypen = [],
            ZaakTypeDeelZaakTypen = [],
            ZaakTypeGerelateerdeZaakTypen = [],
        };

        var besluitType = new BesluitType
        {
            Id = Guid.NewGuid(),
            Owner = TestOwner,
            CatalogusId = catalogusId,
            Catalogus = catalogus,
            Omschrijving = "MatchMe",
            BeginGeldigheid = new DateOnly(2020, 1, 1),
            EindeGeldigheid = null,
            Concept = false,
        };

        dbContext.Catalogussen.Add(catalogus);
        dbContext.ZaakTypen.Add(zaakType);
        dbContext.BesluitTypen.Add(besluitType);
        await dbContext.SaveChangesAsync();

        var uriServiceMock = new Mock<IEntityUriService>();
        uriServiceMock.Setup(m => m.GetId(TestCatalogusUrl)).Returns(catalogusId);

        var authorizationContextAccessorMock = new Mock<IAuthorizationContextAccessor>();
        authorizationContextAccessorMock
            .Setup(m => m.AuthorizationContext)
            .Returns(new AuthorizationContext(new AuthorizedApplication { HasAllAuthorizations = true, Rsin = TestOwner }, []));

        var conceptBusinessRuleMock = new Mock<IConceptBusinessRule>();
        conceptBusinessRuleMock
            .Setup(m => m.ValidateGeldigheid(It.IsAny<List<IConceptEntity>>(), It.IsAny<IConceptEntity>(), It.IsAny<List<ValidationError>>()))
            .Returns(true);

        var zaakTypeDataServiceMock = new Mock<IZaakTypeDataService>();
        zaakTypeDataServiceMock
            .Setup(m => m.GetAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(zaakType);

        var auditTrailServiceMock = new Mock<IAuditTrailService>();
        var auditTrailFactoryMock = new Mock<IAuditTrailFactory>();
        auditTrailFactoryMock.Setup(m => m.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>())).Returns(auditTrailServiceMock.Object);

        var inMemorySettings = new Dictionary<string, string> { { "Application:DontSendNotificaties", "true" } };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings!).Build();

        var sut = new UpdateZaakTypeCommandHandler(
            NullLogger<UpdateZaakTypeCommandHandler>.Instance,
            configuration,
            dbContext,
            Mock.Of<INotificatieService>(),
            uriServiceMock.Object,
            Mock.Of<IEntityUpdater<ZaakType>>(),
            conceptBusinessRuleMock.Object,
            Mock.Of<IReferentielijstenServiceAgent>(),
            authorizationContextAccessorMock.Object,
            Mock.Of<ICacheInvalidator>(),
            auditTrailFactoryMock.Object,
            zaakTypeDataServiceMock.Object
        );

        var command = new UpdateZaakTypeCommand
        {
            Id = zaakTypeId,
            Catalogus = TestCatalogusUrl,
            IsPartialUpdate = true,
            DeelZaakTypen = [],
            BesluitTypen = ["MatchMe"],
            ZaakType = new ZaakType
            {
                Concept = true,
                SelectielijstProcestype = null,
                ZaakTypeGerelateerdeZaakTypen = [],
            },
        };

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(CommandStatus.OK, result.Status);

        var persistedRelations = await dbContext.ZaakTypeBesluitTypen.Where(z => z.ZaakTypeId == zaakTypeId).ToListAsync();

        var relation = Assert.Single(persistedRelations);
        Assert.Equal("MatchMe", relation.BesluitTypeOmschrijving);
    }
}
