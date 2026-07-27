using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Services;
using OneGround.ZGW.Common.Web.Authorization;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.HandlerTests;

public class ZaakTypeDataServiceTests
{
    private const string TestOwner = "111111111";

    [Fact]
    public async Task GetAsync_SoftReferencedBesluitType_ResolvesNavigationWhenMatchingBesluitTypeExists()
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

        // The soft reference persisted earlier: only the omschrijving, navigation unresolved.
        var softReference = new ZaakTypeBesluitType
        {
            Id = Guid.NewGuid(),
            Owner = TestOwner,
            ZaakTypeId = zaakTypeId,
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
        dbContext.ZaakTypeBesluitTypen.Add(softReference);
        dbContext.BesluitTypen.Add(besluitType);
        await dbContext.SaveChangesAsync();

        var authorizationContextAccessorMock = new Mock<IAuthorizationContextAccessor>();
        authorizationContextAccessorMock
            .Setup(m => m.AuthorizationContext)
            .Returns(new AuthorizationContext(new AuthorizedApplication { HasAllAuthorizations = true, Rsin = TestOwner }, []));

        var configuration = new ConfigurationBuilder().AddInMemoryCollection([]).Build();

        var sut = new ZaakTypeDataService(configuration, authorizationContextAccessorMock.Object, dbContext);

        // Act
        var result = await sut.GetAsync(zaakTypeId);

        // Assert
        Assert.NotNull(result);

        var relation = Assert.Single(result.ZaakTypeBesluitTypen);
        Assert.Equal("MatchMe", relation.BesluitTypeOmschrijving);
        Assert.NotNull(relation.BesluitType);
        Assert.Equal("MatchMe", relation.BesluitType.Omschrijving);
    }
}
