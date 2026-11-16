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
using OneGround.ZGW.Catalogi.Web.Handlers.v1._4;
using OneGround.ZGW.Catalogi.Web.Services;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.HandlerTests;

public class UpdateCatalogusCommandHandlerTests
{
    private const string TestOwner = "111111111";
    private const string OtherOwner = "222222222";

    private static (UpdateCatalogusCommandHandler Handler, UnitTestZtcDbContext Context, Mock<ICatalogEventService> EventService) CreateHandler()
    {
        var options = new DbContextOptionsBuilder<ZtcDbContext>().UseInMemoryDatabase(databaseName: $"ztc-{Guid.NewGuid()}").Options;
        var context = new UnitTestZtcDbContext(options);

        var authorizationContextAccessor = new Mock<IAuthorizationContextAccessor>();
        authorizationContextAccessor
            .Setup(m => m.AuthorizationContext)
            .Returns(new AuthorizationContext(new AuthorizedApplication { HasAllAuthorizations = true, Rsin = TestOwner }, []));

        var auditTrailFactory = new Mock<IAuditTrailFactory>();
        auditTrailFactory.Setup(m => m.Create(It.IsAny<AuditTrailOptions>(), It.IsAny<bool>())).Returns(new Mock<IAuditTrailService>().Object);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { { "Application:DontSendNotificaties", "true" } }!)
            .Build();

        var eventService = new Mock<ICatalogEventService>();

        var handler = new UpdateCatalogusCommandHandler(
            NullLogger<UpdateCatalogusCommandHandler>.Instance,
            configuration,
            new Mock<INotificatieService>().Object,
            new Mock<IEntityUriService>().Object,
            context,
            authorizationContextAccessor.Object,
            eventService.Object,
            auditTrailFactory.Object
        );

        return (handler, context, eventService);
    }

    private static Catalogus ExistingCatalogus(string owner = TestOwner) =>
        new()
        {
            Id = Guid.NewGuid(),
            Owner = owner,
            Domein = "TST",
            Rsin = owner,
            ContactpersoonBeheerNaam = "Oud",
            ContactpersoonBeheerTelefoonnummer = "0612345678",
            ContactpersoonBeheerEmailadres = "oud@example.com",
            Naam = "Oude naam",
            Versie = "1.0",
            BegindatumVersie = new DateOnly(2020, 1, 1),
        };

    private static Catalogus Update(string domein = "TST", string rsin = TestOwner) =>
        new()
        {
            Domein = domein,
            Rsin = rsin,
            ContactpersoonBeheerNaam = "Nieuw",
        };

    [Fact]
    public async Task Handle_UnknownCatalogus_ReturnsNotFound()
    {
        var (handler, _, _) = CreateHandler();

        var result = await handler.Handle(new UpdateCatalogusCommand { Id = Guid.NewGuid(), Catalogus = Update() }, CancellationToken.None);

        Assert.Equal(CommandStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_CatalogusOfAnotherOwner_ReturnsNotFoundAndDoesNotChangeIt()
    {
        var (handler, context, _) = CreateHandler();
        var other = ExistingCatalogus(OtherOwner);
        context.Catalogussen.Add(other);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new UpdateCatalogusCommand { Id = other.Id, Catalogus = Update(rsin: OtherOwner) }, CancellationToken.None);

        Assert.Equal(CommandStatus.NotFound, result.Status);
        Assert.Equal("Oud", (await context.Catalogussen.SingleAsync()).ContactpersoonBeheerNaam);
    }

    [Fact]
    public async Task Handle_RsinDiffersFromClient_ReturnsValidationError()
    {
        var (handler, context, _) = CreateHandler();
        var existing = ExistingCatalogus();
        context.Catalogussen.Add(existing);
        await context.SaveChangesAsync();

        var result = await handler.Handle(
            new UpdateCatalogusCommand { Id = existing.Id, Catalogus = Update(rsin: OtherOwner) },
            CancellationToken.None
        );

        Assert.Equal(CommandStatus.ValidationError, result.Status);
        Assert.Equal("nonFieldErrors", Assert.Single(result.Errors).Name);
        Assert.Equal("Oud", (await context.Catalogussen.SingleAsync()).ContactpersoonBeheerNaam);
    }

    [Fact]
    public async Task Handle_DomeinChanged_UpdatesItAndNotifiesTheEventServiceWithTheOldDomein()
    {
        var (handler, context, eventService) = CreateHandler();
        var existing = ExistingCatalogus();
        context.Catalogussen.Add(existing);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new UpdateCatalogusCommand { Id = existing.Id, Catalogus = Update(domein: "NEW") }, CancellationToken.None);

        Assert.Equal(CommandStatus.OK, result.Status);
        Assert.Equal("NEW", (await context.Catalogussen.AsNoTracking().SingleAsync()).Domein);
        eventService.Verify(
            e => e.OnCatalogUpdatedAsync("TST", It.Is<Catalogus>(c => c.Id == existing.Id && c.Domein == "NEW"), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_DomeinNotChanged_NotifiesTheEventServiceWithTheSameDomein()
    {
        var (handler, context, eventService) = CreateHandler();
        var existing = ExistingCatalogus();
        context.Catalogussen.Add(existing);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new UpdateCatalogusCommand { Id = existing.Id, Catalogus = Update() }, CancellationToken.None);

        Assert.Equal(CommandStatus.OK, result.Status);
        eventService.Verify(e => e.OnCatalogUpdatedAsync("TST", It.Is<Catalogus>(c => c.Domein == "TST"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DomeinUsedByAnotherCatalogusOfTheSameOwner_ReturnsValidationErrorAndDoesNotChangeIt()
    {
        var (handler, context, eventService) = CreateHandler();
        var existing = ExistingCatalogus();
        var other = ExistingCatalogus();
        other.Domein = "OTH";
        context.Catalogussen.AddRange(existing, other);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new UpdateCatalogusCommand { Id = existing.Id, Catalogus = Update(domein: "OTH") }, CancellationToken.None);

        Assert.Equal(CommandStatus.ValidationError, result.Status);
        var error = Assert.Single(result.Errors);
        Assert.Equal("nonFieldErrors", error.Name);
        Assert.Equal(ErrorCode.Unique, error.Code);
        Assert.Equal("TST", (await context.Catalogussen.AsNoTracking().SingleAsync(c => c.Id == existing.Id)).Domein);
        eventService.Verify(e => e.OnCatalogUpdatedAsync(It.IsAny<string>(), It.IsAny<Catalogus>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_Update_ReplacesAllUpdatableFieldsIncludingClearedOnes_AndReturnsTheTypes(bool isPartialUpdate)
    {
        var (handler, context, _) = CreateHandler();
        var existing = ExistingCatalogus();
        existing.ZaakTypes =
        [
            new ZaakType
            {
                Id = Guid.NewGuid(),
                Owner = TestOwner,
                Omschrijving = "ZT",
                Doel = "x",
                Aanleiding = "x",
                HandelingInitiator = "x",
                Onderwerp = "x",
                HandelingBehandelaar = "x",
            },
        ];
        existing.BesluitTypes =
        [
            new BesluitType
            {
                Id = Guid.NewGuid(),
                Owner = TestOwner,
                Omschrijving = "BT",
            },
        ];
        context.Catalogussen.Add(existing);
        await context.SaveChangesAsync();

        var update = Update();
        update.Naam = "Nieuwe naam";
        // The optional fields that are not sent are cleared: that is what a PUT does, and what the merge of a PATCH gives for a field set to null
        update.ContactpersoonBeheerTelefoonnummer = null;
        update.ContactpersoonBeheerEmailadres = null;
        update.Versie = null;
        update.BegindatumVersie = null;

        var result = await handler.Handle(
            new UpdateCatalogusCommand
            {
                Id = existing.Id,
                Catalogus = update,
                IsPartialUpdate = isPartialUpdate,
            },
            CancellationToken.None
        );

        Assert.Equal(CommandStatus.OK, result.Status);

        var stored = await context.Catalogussen.AsNoTracking().SingleAsync();
        Assert.Equal("TST", stored.Domein);
        Assert.Equal(TestOwner, stored.Rsin);
        Assert.Equal("Nieuw", stored.ContactpersoonBeheerNaam);
        Assert.Equal("Nieuwe naam", stored.Naam);
        Assert.Null(stored.ContactpersoonBeheerTelefoonnummer);
        Assert.Null(stored.ContactpersoonBeheerEmailadres);
        Assert.Null(stored.Versie);
        Assert.Null(stored.BegindatumVersie);

        // The returned catalogus carries the types: the response lists them
        Assert.Equal(["ZT"], result.Result.ZaakTypes.Select(z => z.Omschrijving));
        Assert.Equal(["BT"], result.Result.BesluitTypes.Select(b => b.Omschrijving));
        Assert.Empty(result.Result.InformatieObjectTypes);
    }
}
