using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OneGround.ZGW.Catalogi.DataModel;
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

public class DeleteCatalogusCommandHandlerTests
{
    private const string TestOwner = "111111111";
    private const string OtherOwner = "222222222";

    private static (DeleteCatalogusCommandHandler Handler, UnitTestZtcDbContext Context, Mock<ICatalogEventService> EventService) CreateHandler()
    {
        var options = new DbContextOptionsBuilder<ZtcDbContext>()
            .UseInMemoryDatabase(databaseName: $"ztc-{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
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

        var handler = new DeleteCatalogusCommandHandler(
            NullLogger<DeleteCatalogusCommandHandler>.Instance,
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
            ContactpersoonBeheerNaam = "Naam",
        };

    [Fact]
    public async Task Handle_UnknownCatalogus_ReturnsNotFound()
    {
        var (handler, _, eventService) = CreateHandler();

        var result = await handler.Handle(new DeleteCatalogusCommand { Id = Guid.NewGuid() }, CancellationToken.None);

        Assert.Equal(CommandStatus.NotFound, result.Status);
        eventService.Verify(e => e.OnCatalogDeletedAsync(It.IsAny<Catalogus>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CatalogusOfAnotherOwner_ReturnsNotFoundAndDoesNotDeleteIt()
    {
        var (handler, context, _) = CreateHandler();
        var other = ExistingCatalogus(OtherOwner);
        context.Catalogussen.Add(other);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new DeleteCatalogusCommand { Id = other.Id }, CancellationToken.None);

        Assert.Equal(CommandStatus.NotFound, result.Status);
        Assert.Single(await context.Catalogussen.ToListAsync());
    }

    [Fact]
    public async Task Handle_EmptyCatalogus_DeletesItAndNotifiesTheEventService()
    {
        var (handler, context, eventService) = CreateHandler();
        var existing = ExistingCatalogus();
        context.Catalogussen.Add(existing);
        await context.SaveChangesAsync();

        var result = await handler.Handle(new DeleteCatalogusCommand { Id = existing.Id }, CancellationToken.None);

        Assert.Equal(CommandStatus.OK, result.Status);
        Assert.Empty(await context.Catalogussen.ToListAsync());
        eventService.Verify(
            e => e.OnCatalogDeletedAsync(It.Is<Catalogus>(c => c.Id == existing.Id && c.Domein == "TST"), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_CatalogusWithZaakType_ReturnsPendingRelationsAndKeepsEverything()
    {
        var (handler, context, eventService) = CreateHandler();
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
        context.Catalogussen.Add(existing);
        await context.SaveChangesAsync();

        await AssertPendingRelationsAsync(handler, context, eventService, existing.Id);
    }

    [Fact]
    public async Task Handle_CatalogusWithBesluitType_ReturnsPendingRelationsAndKeepsEverything()
    {
        var (handler, context, eventService) = CreateHandler();
        var existing = ExistingCatalogus();
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

        await AssertPendingRelationsAsync(handler, context, eventService, existing.Id);
    }

    [Fact]
    public async Task Handle_CatalogusWithInformatieObjectType_ReturnsPendingRelationsAndKeepsEverything()
    {
        var (handler, context, eventService) = CreateHandler();
        var existing = ExistingCatalogus();
        existing.InformatieObjectTypes =
        [
            new InformatieObjectType
            {
                Id = Guid.NewGuid(),
                Owner = TestOwner,
                Omschrijving = "IOT",
                InformatieObjectCategorie = "TST-Document",
            },
        ];
        context.Catalogussen.Add(existing);
        await context.SaveChangesAsync();

        await AssertPendingRelationsAsync(handler, context, eventService, existing.Id);
    }

    [Fact]
    public async Task Handle_CatalogusWithBesluitTypeOfAnotherOwner_StillReturnsPendingRelations()
    {
        // No owner filter on the types on purpose: it must block instead of being deleted by the cascade
        var (handler, context, eventService) = CreateHandler();
        var existing = ExistingCatalogus();
        existing.BesluitTypes =
        [
            new BesluitType
            {
                Id = Guid.NewGuid(),
                Owner = OtherOwner,
                Omschrijving = "BT",
            },
        ];
        context.Catalogussen.Add(existing);
        await context.SaveChangesAsync();

        await AssertPendingRelationsAsync(handler, context, eventService, existing.Id);
    }

    [Fact]
    public async Task Handle_EventServiceThrows_PropagatesTheExceptionSoTheTransactionIsNotCommitted()
    {
        var (handler, context, eventService) = CreateHandler();
        var existing = ExistingCatalogus();
        context.Catalogussen.Add(existing);
        await context.SaveChangesAsync();

        eventService
            .Setup(e => e.OnCatalogDeletedAsync(It.IsAny<Catalogus>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("external store unavailable"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new DeleteCatalogusCommand { Id = existing.Id }, CancellationToken.None)
        );
    }

    private static async Task AssertPendingRelationsAsync(
        DeleteCatalogusCommandHandler handler,
        UnitTestZtcDbContext context,
        Mock<ICatalogEventService> eventService,
        Guid id
    )
    {
        var result = await handler.Handle(new DeleteCatalogusCommand { Id = id }, CancellationToken.None);

        Assert.Equal(CommandStatus.ValidationError, result.Status);
        var error = Assert.Single(result.Errors);
        Assert.Equal("nonFieldErrors", error.Name);
        Assert.Equal(ErrorCode.PendingRelations, error.Code);
        Assert.Single(await context.Catalogussen.ToListAsync());
        eventService.Verify(e => e.OnCatalogDeletedAsync(It.IsAny<Catalogus>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
