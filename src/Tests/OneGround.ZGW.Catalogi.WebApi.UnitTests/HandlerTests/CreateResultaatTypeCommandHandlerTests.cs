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
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.ServiceAgent;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Referentielijsten.Contracts.v1.Responses;
using OneGround.ZGW.Referentielijsten.ServiceAgent;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.HandlerTests;

public class CreateResultaatTypeCommandHandlerTests
{
    private const string TestOwner = "111111111";
    private const string TestZaakTypeUrl = "http://catalogi.local/api/v1/zaaktypen/33333333-3333-3333-3333-333333333333";

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
        };

        dbContext.Catalogussen.Add(catalogus);
        dbContext.ZaakTypen.Add(zaakType);
        await dbContext.SaveChangesAsync();

        var uriServiceMock = new Mock<IEntityUriService>();
        uriServiceMock.Setup(m => m.GetId(TestZaakTypeUrl)).Returns(zaakTypeId);

        var authorizationContextAccessorMock = new Mock<IAuthorizationContextAccessor>();
        authorizationContextAccessorMock
            .Setup(m => m.AuthorizationContext)
            .Returns(new AuthorizationContext(new AuthorizedApplication { HasAllAuthorizations = true, Rsin = TestOwner }, []));

        var conceptBusinessRuleMock = new Mock<IConceptBusinessRule>();
        conceptBusinessRuleMock.Setup(m => m.ValidateConceptZaakType(It.IsAny<ZaakType>(), It.IsAny<List<ValidationError>>())).Returns(true);

        var referentielijstenServiceAgentMock = new Mock<IReferentielijstenServiceAgent>();
        referentielijstenServiceAgentMock
            .Setup(m => m.GetResultaatByUrl(It.IsAny<string>()))
            .ReturnsAsync(new ServiceAgentResponse<ResultaatResponseDto>(new ResultaatResponseDto { Waardering = null, BewaarTermijn = null }));
        referentielijstenServiceAgentMock
            .Setup(m => m.GetResultaatTypeOmschrijvingByUrlAsync(It.IsAny<string>()))
            .ReturnsAsync(
                new ServiceAgentResponse<ResultaatTypeOmschrijvingResponseDto>(new ResultaatTypeOmschrijvingResponseDto { Omschrijving = "Test" })
            );

        var auditTrailServiceMock = new Mock<IAuditTrailService>();
        var auditTrailFactoryMock = new Mock<IAuditTrailFactory>();
        auditTrailFactoryMock.Setup(m => m.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>())).Returns(auditTrailServiceMock.Object);

        var inMemorySettings = new Dictionary<string, string> { { "Application:DontSendNotificaties", "true" } };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings!).Build();

        var sut = new CreateResultaatTypeCommandHandler(
            NullLogger<CreateResultaatTypeCommandHandler>.Instance,
            configuration,
            dbContext,
            uriServiceMock.Object,
            conceptBusinessRuleMock.Object,
            Mock.Of<IResultaatTypeBusinessRuleService>(),
            referentielijstenServiceAgentMock.Object,
            authorizationContextAccessorMock.Object,
            Mock.Of<ICacheInvalidator>(),
            auditTrailFactoryMock.Object
        );

        var command = new CreateResultaatTypeCommand
        {
            ZaakType = TestZaakTypeUrl,
            BesluitTypen = ["ZZZ-Unique-Test"],
            ResultaatType = new ResultaatType
            {
                Omschrijving = "Test",
                ResultaatTypeOmschrijving = "Test",
                OmschrijvingGeneriek = "Test",
                SelectieLijstKlasse = "http://referentielijsten.local/api/v1/resultaten/44444444-4444-4444-4444-444444444444",
            },
        };

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(CommandStatus.OK, result.Status);

        var persistedRelations = await dbContext.ResultaatTypeBesluitTypen.Where(r => r.ResultaatTypeId == command.ResultaatType.Id).ToListAsync();

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
        uriServiceMock.Setup(m => m.GetId(TestZaakTypeUrl)).Returns(zaakTypeId);

        var authorizationContextAccessorMock = new Mock<IAuthorizationContextAccessor>();
        authorizationContextAccessorMock
            .Setup(m => m.AuthorizationContext)
            .Returns(new AuthorizationContext(new AuthorizedApplication { HasAllAuthorizations = true, Rsin = TestOwner }, []));

        var conceptBusinessRuleMock = new Mock<IConceptBusinessRule>();
        conceptBusinessRuleMock.Setup(m => m.ValidateConceptZaakType(It.IsAny<ZaakType>(), It.IsAny<List<ValidationError>>())).Returns(true);

        var referentielijstenServiceAgentMock = new Mock<IReferentielijstenServiceAgent>();
        referentielijstenServiceAgentMock
            .Setup(m => m.GetResultaatByUrl(It.IsAny<string>()))
            .ReturnsAsync(new ServiceAgentResponse<ResultaatResponseDto>(new ResultaatResponseDto { Waardering = null, BewaarTermijn = null }));
        referentielijstenServiceAgentMock
            .Setup(m => m.GetResultaatTypeOmschrijvingByUrlAsync(It.IsAny<string>()))
            .ReturnsAsync(
                new ServiceAgentResponse<ResultaatTypeOmschrijvingResponseDto>(new ResultaatTypeOmschrijvingResponseDto { Omschrijving = "Test" })
            );

        var auditTrailServiceMock = new Mock<IAuditTrailService>();
        var auditTrailFactoryMock = new Mock<IAuditTrailFactory>();
        auditTrailFactoryMock.Setup(m => m.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>())).Returns(auditTrailServiceMock.Object);

        var inMemorySettings = new Dictionary<string, string> { { "Application:DontSendNotificaties", "true" } };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings!).Build();

        var sut = new CreateResultaatTypeCommandHandler(
            NullLogger<CreateResultaatTypeCommandHandler>.Instance,
            configuration,
            dbContext,
            uriServiceMock.Object,
            conceptBusinessRuleMock.Object,
            Mock.Of<IResultaatTypeBusinessRuleService>(),
            referentielijstenServiceAgentMock.Object,
            authorizationContextAccessorMock.Object,
            Mock.Of<ICacheInvalidator>(),
            auditTrailFactoryMock.Object
        );

        var command = new CreateResultaatTypeCommand
        {
            ZaakType = TestZaakTypeUrl,
            BesluitTypen = ["MatchMe"],
            ResultaatType = new ResultaatType
            {
                Omschrijving = "Test",
                ResultaatTypeOmschrijving = "Test",
                OmschrijvingGeneriek = "Test",
                SelectieLijstKlasse = "http://referentielijsten.local/api/v1/resultaten/44444444-4444-4444-4444-444444444444",
            },
        };

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(CommandStatus.OK, result.Status);

        var persistedRelations = await dbContext.ResultaatTypeBesluitTypen.Where(r => r.ResultaatTypeId == command.ResultaatType.Id).ToListAsync();

        var relation = Assert.Single(persistedRelations);
        Assert.Equal("MatchMe", relation.BesluitTypeOmschrijving);
    }
}
