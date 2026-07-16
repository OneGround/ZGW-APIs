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
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.UriServices;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.HandlerTests;

public class GetResultaatTypeQueryHandlerTests
{
    private const string TestOwner = "111111111";

    [Fact]
    public async Task Handle_SoftReferencedBesluitType_ResolvesNavigationWhenMatchingBesluitTypeExists()
    {
        // Arrange
        var catalogusId = Guid.NewGuid();
        var zaakTypeId = Guid.NewGuid();
        var resultaatTypeId = Guid.NewGuid();

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

        var resultaatType = new ResultaatType
        {
            Id = resultaatTypeId,
            Owner = TestOwner,
            ZaakTypeId = zaakTypeId,
            ZaakType = zaakType,
            Omschrijving = "Test",
            ResultaatTypeOmschrijving = "Test",
            OmschrijvingGeneriek = "Test",
            SelectieLijstKlasse = "http://referentielijsten.local/api/v1/resultaten/44444444-4444-4444-4444-444444444444",
        };

        // The soft reference persisted earlier: only the omschrijving, navigation unresolved.
        var softReference = new ResultaatTypeBesluitType
        {
            Id = Guid.NewGuid(),
            Owner = TestOwner,
            ResultaatTypeId = resultaatTypeId,
            BesluitTypeOmschrijving = "MatchMe",
        };

        // A matching, non-concept besluittype within geldigheid exists now.
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
        dbContext.ResultaatTypen.Add(resultaatType);
        dbContext.ResultaatTypeBesluitTypen.Add(softReference);
        dbContext.BesluitTypen.Add(besluitType);
        await dbContext.SaveChangesAsync();

        var authorizationContextAccessorMock = new Mock<IAuthorizationContextAccessor>();
        authorizationContextAccessorMock
            .Setup(m => m.AuthorizationContext)
            .Returns(new AuthorizationContext(new AuthorizedApplication { HasAllAuthorizations = true, Rsin = TestOwner }, []));

        var inMemorySettings = new Dictionary<string, string> { { "Application:DontSendNotificaties", "true" } };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings!).Build();

        var sut = new GetResultaatTypeQueryHandler(
            NullLogger<GetResultaatTypeQueryHandler>.Instance,
            configuration,
            Mock.Of<IEntityUriService>(),
            dbContext,
            authorizationContextAccessorMock.Object
        );

        // Act
        var result = await sut.Handle(new GetResultaatTypeQuery { Id = resultaatTypeId }, CancellationToken.None);

        // Assert
        Assert.Equal(QueryStatus.OK, result.Status);

        var relation = Assert.Single(result.Result.ResultaatTypeBesluitTypen);
        Assert.Equal("MatchMe", relation.BesluitTypeOmschrijving);
        Assert.NotNull(relation.BesluitType);
        Assert.Equal("MatchMe", relation.BesluitType.Omschrijving);
    }
}
