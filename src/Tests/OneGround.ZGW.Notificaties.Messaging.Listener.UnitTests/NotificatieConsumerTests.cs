using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoFixture;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Messaging.Configuration;
using OneGround.ZGW.Notificaties.Messaging.Consumers;
using OneGround.ZGW.Notificaties.Messaging.Jobs.Notificatie;
using Xunit;

namespace OneGround.ZGW.Notificaties.Listener.UnitTests;

public class OmitOnRecursionFixture : Fixture
{
    public OmitOnRecursionFixture()
    {
        Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => Behaviors.Remove(b));
        Behaviors.Add(new OmitOnRecursionBehavior());
    }
}

public class SendNotificatiesConsumerTests
{
    private readonly Fixture _fixture = new OmitOnRecursionFixture();
    private readonly Mock<ILogger<SendNotificatiesConsumer>> _logger;
    private readonly Mock<INotificatieScheduler> _notificatieScheduler;
    private readonly IMemoryCache _memoryCache;
    private readonly IOptions<ApplicationOptions> _configuration;
    private readonly Mock<INotificationFilterService> _notificationFilterService;

    public SendNotificatiesConsumerTests()
    {
        _logger = new Mock<ILogger<SendNotificatiesConsumer>>();

        _notificatieScheduler = new Mock<INotificatieScheduler>();

        _notificatieScheduler.Setup(m => m.Enqueue(It.IsAny<Expression<Func<NotificatieJob, Task>>>()));

        _notificationFilterService = new Mock<INotificationFilterService>();
        _notificationFilterService
            .Setup(x => x.IsIgnored(It.IsAny<ISendNotificaties>(), It.IsAny<Abonnement>(), It.IsAny<AbonnementKanaal>()))
            .Returns(true);

        _configuration = Options.Create(new ApplicationOptions() { AbonnementenCacheExpirationTime = TimeSpan.FromMinutes(2) });

        _memoryCache = new MockMemoryCache();
    }

    [Fact]
    public async Task SendNotification_WhenNoFiltersPresent()
    {
        var channel = _fixture.Create<Kanaal>();
        channel.Naam = "zaken";
        channel.Filters = ["Bronorganisatie"];
        channel.AbonnementKanalen = [];

        var subscription = _fixture.Create<Abonnement>();
        subscription.AbonnementKanalen = [];
        subscription.Owner = "owner";

        var context = await SetupDbContext(c =>
        {
            c.Kanalen.Add(channel);
            c.Abonnementen.Add(subscription);
            c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = subscription,
                    Kanaal = channel,
                    Id = Guid.NewGuid(),
                    Filters = [],
                }
            );
        });

        var serviceProvider = BuildServiceCollection(context);

        var consumer = new SendNotificatiesConsumer(
            _logger.Object,
            serviceProvider,
            _memoryCache,
            _notificatieScheduler.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        _notificationFilterService
            .Setup(x => x.IsIgnored(It.IsAny<ISendNotificaties>(), It.IsAny<Abonnement>(), It.IsAny<AbonnementKanaal>()))
            .Returns(false);
        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        var headersMock = new Mock<Headers>();
        message.Setup(s => s.Headers).Returns(headersMock.Object);

        await consumer.Consume(message.Object);

        _notificatieScheduler.Verify(m => m.Enqueue(It.IsAny<Expression<Func<NotificatieJob, Task>>>()), Times.Once);
    }

    [Fact]
    public async Task SendNotification_WhenFilterMatches()
    {
        var channel = _fixture.Create<Kanaal>();
        channel.Naam = "zaken";
        channel.Filters = ["bronorganisatie"];
        channel.AbonnementKanalen = [];

        var subscription = _fixture.Create<Abonnement>();
        subscription.AbonnementKanalen = [];
        subscription.Owner = "owner";

        var context = await SetupDbContext(c =>
        {
            c.Kanalen.Add(channel);
            c.Abonnementen.Add(subscription);
            var guid = c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = subscription,
                    Kanaal = channel,
                    Id = Guid.NewGuid(),
                    Filters = [new FilterValue { Key = "bronorganisatie", Value = "123" }],
                }
            );
        });

        var serviceProvider = BuildServiceCollection(context);

        var consumer = new SendNotificatiesConsumer(
            _logger.Object,
            serviceProvider,
            _memoryCache,
            _notificatieScheduler.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        notificatie.Setup(s => s.Kenmerken).Returns(new Dictionary<string, string> { { "bronorganisatie", "123" } });
        _notificationFilterService
            .Setup(x => x.IsIgnored(It.IsAny<ISendNotificaties>(), It.IsAny<Abonnement>(), It.IsAny<AbonnementKanaal>()))
            .Returns(false);

        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        var headersMock = new Mock<Headers>();
        message.Setup(s => s.Headers).Returns(headersMock.Object);

        await consumer.Consume(message.Object);

        _notificatieScheduler.Verify(m => m.Enqueue(It.IsAny<Expression<Func<NotificatieJob, Task>>>()), Times.Once);
    }

    [Fact]
    public async Task SendNotification_WhenFilterDoesNotMatch()
    {
        var channel = _fixture.Create<Kanaal>();
        channel.Naam = "zaken";
        channel.Filters = ["bronorganisatie"];
        channel.AbonnementKanalen = [];

        var subscription = _fixture.Create<Abonnement>();
        subscription.AbonnementKanalen = [];
        subscription.Owner = "owner";

        var context = await SetupDbContext(c =>
        {
            c.Kanalen.Add(channel);
            c.Abonnementen.Add(subscription);
            var guid = c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = subscription,
                    Kanaal = channel,
                    Id = Guid.NewGuid(),
                    Filters = [new FilterValue { Key = "bronorganisatie", Value = "456" }],
                }
            );
        });

        var serviceProvider = BuildServiceCollection(context);

        var consumer = new SendNotificatiesConsumer(
            _logger.Object,
            serviceProvider,
            _memoryCache,
            _notificatieScheduler.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        notificatie.Setup(s => s.Kenmerken).Returns(new Dictionary<string, string> { { "bronorganisatie", "123" } });

        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        var headersMock = new Mock<Headers>();
        message.Setup(s => s.Headers).Returns(headersMock.Object);

        await consumer.Consume(message.Object);

        _notificatieScheduler.Verify(m => m.Enqueue(It.IsAny<Expression<Func<NotificatieJob, Task>>>()), Times.Never);
    }

    [Fact]
    public async Task SendNotification_WhenFilterDoesMatch_CaseInsensitive()
    {
        var channel = _fixture.Create<Kanaal>();
        channel.Naam = "zaken";
        channel.Filters = ["Bronorganisatie"];
        channel.AbonnementKanalen = [];

        var subscription = _fixture.Create<Abonnement>();
        subscription.AbonnementKanalen = [];
        subscription.Owner = "owner";

        var context = await SetupDbContext(c =>
        {
            c.Kanalen.Add(channel);
            c.Abonnementen.Add(subscription);
            var guid = c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = subscription,
                    Kanaal = channel,
                    Id = Guid.NewGuid(),
                    Filters = [new FilterValue { Key = "bronOrganisatie", Value = "123" }],
                }
            );
        });

        var serviceProvider = BuildServiceCollection(context);

        var consumer = new SendNotificatiesConsumer(
            _logger.Object,
            serviceProvider,
            _memoryCache,
            _notificatieScheduler.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        notificatie.Setup(s => s.Kenmerken).Returns(new Dictionary<string, string> { { "Bronorganisatie", "123" } });

        _notificationFilterService
            .Setup(x => x.IsIgnored(It.IsAny<ISendNotificaties>(), It.IsAny<Abonnement>(), It.IsAny<AbonnementKanaal>()))
            .Returns(false);

        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        var headersMock = new Mock<Headers>();
        message.Setup(s => s.Headers).Returns(headersMock.Object);

        await consumer.Consume(message.Object);

        _notificatieScheduler.Verify(m => m.Enqueue(It.IsAny<Expression<Func<NotificatieJob, Task>>>()), Times.Once);
    }

    [Fact]
    public async Task SendNotification_WhenIgnore_FilterCallbacks()
    {
        var channel = _fixture.Create<Kanaal>();
        channel.Naam = "zaken";
        channel.Filters = [];
        channel.AbonnementKanalen = [];

        Abonnement CreateSubscription(string callback)
        {
            var subscription = _fixture.Create<Abonnement>();
            subscription.AbonnementKanalen = [];
            subscription.Owner = "owner";
            subscription.CallbackUrl = callback;
            return subscription;
        }

        var callbackUrl = "http://example.com/api/notifications";
        var context = await SetupDbContext(c =>
        {
            c.Kanalen.Add(channel);
            c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = CreateSubscription(callbackUrl),
                    Kanaal = channel,
                    Id = Guid.NewGuid(),
                    Filters = [],
                }
            );
            c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = CreateSubscription(callbackUrl),
                    Kanaal = channel,
                    Id = Guid.NewGuid(),
                    Filters = [],
                }
            );
        });
        _notificationFilterService
            .SetupSequence(x => x.IsIgnored(It.IsAny<ISendNotificaties>(), It.IsAny<Abonnement>(), It.IsAny<AbonnementKanaal>()))
            .Returns(true)
            .Returns(false);

        var serviceProvider = BuildServiceCollection(context);

        var consumer = new SendNotificatiesConsumer(
            _logger.Object,
            serviceProvider,
            _memoryCache,
            _notificatieScheduler.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        notificatie.Setup(s => s.Ignore).Returns(true);

        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        var headersMock = new Mock<Headers>();
        message.Setup(s => s.Headers).Returns(headersMock.Object);

        await consumer.Consume(message.Object);

        _notificatieScheduler.Verify(m => m.Enqueue(It.IsAny<Expression<Func<NotificatieJob, Task>>>()), Times.Once);
    }

    [Fact]
    public async Task SendNotification_WhenKenmerkBronFilter_WithSpecificValue_ShouldMatch()
    {
        var channel = _fixture.Create<Kanaal>();
        channel.Naam = "zaken";
        channel.Filters = ["kenmerk_bron"];
        channel.AbonnementKanalen = [];

        var subscription = _fixture.Create<Abonnement>();
        subscription.AbonnementKanalen = [];
        subscription.Owner = "owner";

        var context = await SetupDbContext(c =>
        {
            c.Kanalen.Add(channel);
            c.Abonnementen.Add(subscription);
            c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = subscription,
                    Kanaal = channel,
                    Id = Guid.NewGuid(),
                    Filters = [new FilterValue { Key = "kenmerk_bron", Value = "DRN001" }],
                }
            );
        });

        var serviceProvider = BuildServiceCollection(context);

        var consumer = new SendNotificatiesConsumer(
            _logger.Object,
            serviceProvider,
            _memoryCache,
            _notificatieScheduler.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        notificatie.Setup(s => s.Kenmerken).Returns(new Dictionary<string, string> { { "kenmerk_bron", "DRN001" } });

        _notificationFilterService
            .Setup(x => x.IsIgnored(It.IsAny<ISendNotificaties>(), It.IsAny<Abonnement>(), It.IsAny<AbonnementKanaal>()))
            .Returns(false);

        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        var headersMock = new Mock<Headers>();
        message.Setup(s => s.Headers).Returns(headersMock.Object);

        await consumer.Consume(message.Object);

        _notificatieScheduler.Verify(m => m.Enqueue(It.IsAny<Expression<Func<NotificatieJob, Task>>>()), Times.Once);
    }

    [Fact]
    public async Task SendNotification_WhenKenmerkBronFilter_WithSpecificValue_ShouldNotMatch()
    {
        var channel = _fixture.Create<Kanaal>();
        channel.Naam = "zaken";
        channel.Filters = ["kenmerk_bron"];
        channel.AbonnementKanalen = [];

        var subscription = _fixture.Create<Abonnement>();
        subscription.AbonnementKanalen = [];
        subscription.Owner = "owner";

        var context = await SetupDbContext(c =>
        {
            c.Kanalen.Add(channel);
            c.Abonnementen.Add(subscription);
            c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = subscription,
                    Kanaal = channel,
                    Id = Guid.NewGuid(),
                    Filters = [new FilterValue { Key = "kenmerk_bron", Value = "DRN001" }],
                }
            );
        });

        var serviceProvider = BuildServiceCollection(context);

        var consumer = new SendNotificatiesConsumer(
            _logger.Object,
            serviceProvider,
            _memoryCache,
            _notificatieScheduler.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        notificatie.Setup(s => s.Kenmerken).Returns(new Dictionary<string, string> { { "kenmerk_bron", "DRN002" } });

        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        var headersMock = new Mock<Headers>();
        message.Setup(s => s.Headers).Returns(headersMock.Object);

        await consumer.Consume(message.Object);

        _notificatieScheduler.Verify(m => m.Enqueue(It.IsAny<Expression<Func<NotificatieJob, Task>>>()), Times.Never);
    }

    [Fact]
    public async Task SendNotification_WhenKenmerkBronFilter_NullKenmerken_ShouldNotMatch()
    {
        var channel = _fixture.Create<Kanaal>();
        channel.Naam = "zaken";
        channel.Filters = ["kenmerk_bron"];
        channel.AbonnementKanalen = [];

        var subscription = _fixture.Create<Abonnement>();
        subscription.AbonnementKanalen = [];
        subscription.Owner = "owner";

        var context = await SetupDbContext(c =>
        {
            c.Kanalen.Add(channel);
            c.Abonnementen.Add(subscription);
            c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = subscription,
                    Kanaal = channel,
                    Id = Guid.NewGuid(),
                    Filters = [new FilterValue { Key = "kenmerk_bron", Value = "DRN001" }],
                }
            );
        });

        var serviceProvider = BuildServiceCollection(context);

        var consumer = new SendNotificatiesConsumer(
            _logger.Object,
            serviceProvider,
            _memoryCache,
            _notificatieScheduler.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        notificatie.Setup(s => s.Kenmerken).Returns((Dictionary<string, string>)null!);

        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        var headersMock = new Mock<Headers>();
        message.Setup(s => s.Headers).Returns(headersMock.Object);

        await consumer.Consume(message.Object);

        _notificatieScheduler.Verify(m => m.Enqueue(It.IsAny<Expression<Func<NotificatieJob, Task>>>()), Times.Never);
    }

    [Fact]
    public async Task SendNotification_WhenNoKenmerkBronInNotification_ShouldNotAddKenmerkBronToResult()
    {
        var channel = _fixture.Create<Kanaal>();
        channel.Naam = "zaken";
        channel.Filters = ["bronorganisatie"];
        channel.AbonnementKanalen = [];

        var subscription = _fixture.Create<Abonnement>();
        subscription.AbonnementKanalen = [];
        subscription.Owner = "owner";

        var context = await SetupDbContext(c =>
        {
            c.Kanalen.Add(channel);
            c.Abonnementen.Add(subscription);
            c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = subscription,
                    Kanaal = channel,
                    Id = Guid.NewGuid(),
                    Filters = [new FilterValue { Key = "bronorganisatie", Value = "123" }],
                }
            );
        });

        var serviceProvider = BuildServiceCollection(context);

        var consumer = new SendNotificatiesConsumer(
            _logger.Object,
            serviceProvider,
            _memoryCache,
            _notificatieScheduler.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        notificatie.Setup(s => s.Kenmerken).Returns(new Dictionary<string, string> { { "bronorganisatie", "123" } });

        _notificationFilterService
            .Setup(x => x.IsIgnored(It.IsAny<ISendNotificaties>(), It.IsAny<Abonnement>(), It.IsAny<AbonnementKanaal>()))
            .Returns(false);

        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        var headersMock = new Mock<Headers>();
        message.Setup(s => s.Headers).Returns(headersMock.Object);

        await consumer.Consume(message.Object);

        _notificatieScheduler.Verify(m => m.Enqueue(It.IsAny<Expression<Func<NotificatieJob, Task>>>()), Times.Once);

        // Since no kenmerk_bron filter exists and notification doesn't have kenmerk_bron,
        // the notification is sent but without kenmerk_bron added to it
    }

    [Fact]
    public async Task SendNotification_WhenKenmerkBronFilter_WithSemicolonSeparatedValues_ShouldNotMatchNonExistentValue()
    {
        var channel = _fixture.Create<Kanaal>();
        channel.Naam = "zaken";
        channel.Filters = ["kenmerk_bron"];
        channel.AbonnementKanalen = [];

        var subscription = _fixture.Create<Abonnement>();
        subscription.AbonnementKanalen = [];
        subscription.Owner = "owner";

        var context = await SetupDbContext(c =>
        {
            c.Kanalen.Add(channel);
            c.Abonnementen.Add(subscription);
            c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = subscription,
                    Kanaal = channel,
                    Id = Guid.NewGuid(),
                    Filters = [new FilterValue { Key = "kenmerk_bron", Value = "DRN999" }],
                }
            );
        });

        var serviceProvider = BuildServiceCollection(context);

        var consumer = new SendNotificatiesConsumer(
            _logger.Object,
            serviceProvider,
            _memoryCache,
            _notificatieScheduler.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        notificatie.Setup(s => s.Kenmerken).Returns(new Dictionary<string, string> { { "kenmerk_bron", "DRN001;DRN002;DRN003" } });

        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        var headersMock = new Mock<Headers>();
        message.Setup(s => s.Headers).Returns(headersMock.Object);

        await consumer.Consume(message.Object);

        _notificatieScheduler.Verify(m => m.Enqueue(It.IsAny<Expression<Func<NotificatieJob, Task>>>()), Times.Never);
    }

    [Fact]
    public async Task SendNotification_WhenKenmerkBronFilter_MissingInKenmerken_ShouldNotMatch()
    {
        var channel = _fixture.Create<Kanaal>();
        channel.Naam = "zaken";
        channel.Filters = ["kenmerk_bron"];
        channel.AbonnementKanalen = [];

        var subscription = _fixture.Create<Abonnement>();
        subscription.AbonnementKanalen = [];
        subscription.Owner = "owner";

        var context = await SetupDbContext(c =>
        {
            c.Kanalen.Add(channel);
            c.Abonnementen.Add(subscription);
            c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = subscription,
                    Kanaal = channel,
                    Id = Guid.NewGuid(),
                    Filters = [new FilterValue { Key = "kenmerk_bron", Value = "DRN001" }],
                }
            );
        });

        var serviceProvider = BuildServiceCollection(context);

        var consumer = new SendNotificatiesConsumer(
            _logger.Object,
            serviceProvider,
            _memoryCache,
            _notificatieScheduler.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        notificatie.Setup(s => s.Kenmerken).Returns(new Dictionary<string, string> { { "bronorganisatie", "123" } });

        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        var headersMock = new Mock<Headers>();
        message.Setup(s => s.Headers).Returns(headersMock.Object);

        await consumer.Consume(message.Object);

        _notificatieScheduler.Verify(m => m.Enqueue(It.IsAny<Expression<Func<NotificatieJob, Task>>>()), Times.Never);
    }

    [Fact]
    public async Task SendNotification_WhenKenmerkBronFilter_WithSemicolonSeparatedAndEmptyEntries_ShouldMatch()
    {
        var channel = _fixture.Create<Kanaal>();
        channel.Naam = "zaken";
        channel.Filters = ["kenmerk_bron"];
        channel.AbonnementKanalen = [];

        var subscription = _fixture.Create<Abonnement>();
        subscription.AbonnementKanalen = [];
        subscription.Owner = "owner";

        var context = await SetupDbContext(c =>
        {
            c.Kanalen.Add(channel);
            c.Abonnementen.Add(subscription);
            c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = subscription,
                    Kanaal = channel,
                    Id = Guid.NewGuid(),
                    Filters = [new FilterValue { Key = "kenmerk_bron", Value = "DRN002" }],
                }
            );
        });

        var serviceProvider = BuildServiceCollection(context);

        var consumer = new SendNotificatiesConsumer(
            _logger.Object,
            serviceProvider,
            _memoryCache,
            _notificatieScheduler.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        // Semicolon-separated with empty entries
        notificatie.Setup(s => s.Kenmerken).Returns(new Dictionary<string, string> { { "kenmerk_bron", "DRN001;;DRN002;;DRN003;" } });

        _notificationFilterService
            .Setup(x => x.IsIgnored(It.IsAny<ISendNotificaties>(), It.IsAny<Abonnement>(), It.IsAny<AbonnementKanaal>()))
            .Returns(false);

        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        var headersMock = new Mock<Headers>();
        message.Setup(s => s.Headers).Returns(headersMock.Object);

        await consumer.Consume(message.Object);

        _notificatieScheduler.Verify(m => m.Enqueue(It.IsAny<Expression<Func<NotificatieJob, Task>>>()), Times.Once);
    }

    [Fact]
    public async Task SendNotification_WhenKenmerkBronFilter_CombinedWithOtherFilters_ShouldMatchAllFilters()
    {
        var channel = _fixture.Create<Kanaal>();
        channel.Naam = "zaken";
        channel.Filters = ["bronorganisatie", "kenmerk_bron"];
        channel.AbonnementKanalen = [];

        var subscription = _fixture.Create<Abonnement>();
        subscription.AbonnementKanalen = [];
        subscription.Owner = "owner";

        var context = await SetupDbContext(c =>
        {
            c.Kanalen.Add(channel);
            c.Abonnementen.Add(subscription);
            c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = subscription,
                    Kanaal = channel,
                    Id = Guid.NewGuid(),
                    Filters =
                    [
                        new FilterValue { Key = "bronorganisatie", Value = "123" },
                        new FilterValue { Key = "kenmerk_bron", Value = "DRN001" },
                    ],
                }
            );
        });

        var serviceProvider = BuildServiceCollection(context);

        var consumer = new SendNotificatiesConsumer(
            _logger.Object,
            serviceProvider,
            _memoryCache,
            _notificatieScheduler.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        notificatie
            .Setup(s => s.Kenmerken)
            .Returns(new Dictionary<string, string> { { "bronorganisatie", "123" }, { "kenmerk_bron", "DRN001;DRN002" } });

        _notificationFilterService
            .Setup(x => x.IsIgnored(It.IsAny<ISendNotificaties>(), It.IsAny<Abonnement>(), It.IsAny<AbonnementKanaal>()))
            .Returns(false);

        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        var headersMock = new Mock<Headers>();
        message.Setup(s => s.Headers).Returns(headersMock.Object);

        await consumer.Consume(message.Object);

        _notificatieScheduler.Verify(m => m.Enqueue(It.IsAny<Expression<Func<NotificatieJob, Task>>>()), Times.Once);
    }

    [Fact]
    public async Task SendNotification_WhenKenmerkBronFilter_CombinedWithOtherFilters_OneDoesNotMatch_ShouldNotNotify()
    {
        var channel = _fixture.Create<Kanaal>();
        channel.Naam = "zaken";
        channel.Filters = ["bronorganisatie", "kenmerk_bron"];
        channel.AbonnementKanalen = [];

        var subscription = _fixture.Create<Abonnement>();
        subscription.AbonnementKanalen = [];
        subscription.Owner = "owner";

        var context = await SetupDbContext(c =>
        {
            c.Kanalen.Add(channel);
            c.Abonnementen.Add(subscription);
            c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = subscription,
                    Kanaal = channel,
                    Id = Guid.NewGuid(),
                    Filters =
                    [
                        new FilterValue { Key = "bronorganisatie", Value = "999" },
                        new FilterValue { Key = "kenmerk_bron", Value = "DRN001" },
                    ],
                }
            );
        });

        var serviceProvider = BuildServiceCollection(context);

        var consumer = new SendNotificatiesConsumer(
            _logger.Object,
            serviceProvider,
            _memoryCache,
            _notificatieScheduler.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        notificatie
            .Setup(s => s.Kenmerken)
            .Returns(new Dictionary<string, string> { { "bronorganisatie", "123" }, { "kenmerk_bron", "DRN001;DRN002" } });

        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        var headersMock = new Mock<Headers>();
        message.Setup(s => s.Headers).Returns(headersMock.Object);

        await consumer.Consume(message.Object);

        _notificatieScheduler.Verify(m => m.Enqueue(It.IsAny<Expression<Func<NotificatieJob, Task>>>()), Times.Never);
    }

    [Fact]
    public async Task SendNotification_WhenMultipleChannels_WithDifferentKenmerkBronValues_ShouldNotify()
    {
        var channel1 = _fixture.Create<Kanaal>();
        channel1.Naam = "zaken";
        channel1.Filters = ["kenmerk_bron"];
        channel1.AbonnementKanalen = [];

        var channel2 = _fixture.Create<Kanaal>();
        channel2.Naam = "zaken";
        channel2.Filters = ["kenmerk_bron"];
        channel2.AbonnementKanalen = [];

        var subscription = _fixture.Create<Abonnement>();
        subscription.AbonnementKanalen = [];
        subscription.Owner = "owner";

        var context = await SetupDbContext(c =>
        {
            c.Kanalen.Add(channel1);
            c.Kanalen.Add(channel2);
            c.Abonnementen.Add(subscription);
            c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = subscription,
                    Kanaal = channel1,
                    Id = Guid.NewGuid(),
                    Filters = [new FilterValue { Key = "kenmerk_bron", Value = "DRN001" }],
                }
            );
            c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = subscription,
                    Kanaal = channel2,
                    Id = Guid.NewGuid(),
                    Filters = [new FilterValue { Key = "kenmerk_bron", Value = "DRN002" }],
                }
            );
        });

        var serviceProvider = BuildServiceCollection(context);

        var consumer = new SendNotificatiesConsumer(
            _logger.Object,
            serviceProvider,
            _memoryCache,
            _notificatieScheduler.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        notificatie.Setup(s => s.Kenmerken).Returns(new Dictionary<string, string> { { "kenmerk_bron", "DRN001;DRN002;DRN003" } });

        _notificationFilterService
            .Setup(x => x.IsIgnored(It.IsAny<ISendNotificaties>(), It.IsAny<Abonnement>(), It.IsAny<AbonnementKanaal>()))
            .Returns(false);

        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        var headersMock = new Mock<Headers>();
        message.Setup(s => s.Headers).Returns(headersMock.Object);

        await consumer.Consume(message.Object);

        // Should notify once since both channels match
        _notificatieScheduler.Verify(m => m.Enqueue(It.IsAny<Expression<Func<NotificatieJob, Task>>>()), Times.Once);
    }

    [Fact]
    public async Task SendNotification_WhenKenmerkBronFilter_WithMultipleSemicolonSeparatedValues_FirstValueMatches()
    {
        var channel = _fixture.Create<Kanaal>();
        channel.Naam = "zaken";
        channel.Filters = ["kenmerk_bron"];
        channel.AbonnementKanalen = [];

        var subscription = _fixture.Create<Abonnement>();
        subscription.AbonnementKanalen = [];
        subscription.Owner = "owner";

        var context = await SetupDbContext(c =>
        {
            c.Kanalen.Add(channel);
            c.Abonnementen.Add(subscription);
            c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = subscription,
                    Kanaal = channel,
                    Id = Guid.NewGuid(),
                    Filters = [new FilterValue { Key = "kenmerk_bron", Value = "DRN001" }],
                }
            );
        });

        var serviceProvider = BuildServiceCollection(context);

        var consumer = new SendNotificatiesConsumer(
            _logger.Object,
            serviceProvider,
            _memoryCache,
            _notificatieScheduler.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        notificatie.Setup(s => s.Kenmerken).Returns(new Dictionary<string, string> { { "kenmerk_bron", "DRN001;DRN002;DRN003" } });

        _notificationFilterService
            .Setup(x => x.IsIgnored(It.IsAny<ISendNotificaties>(), It.IsAny<Abonnement>(), It.IsAny<AbonnementKanaal>()))
            .Returns(false);

        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        var headersMock = new Mock<Headers>();
        message.Setup(s => s.Headers).Returns(headersMock.Object);

        await consumer.Consume(message.Object);

        _notificatieScheduler.Verify(m => m.Enqueue(It.IsAny<Expression<Func<NotificatieJob, Task>>>()), Times.Once);
    }

    [Fact]
    public async Task SendNotification_WhenKenmerkBronFilter_WithMultipleSemicolonSeparatedValues_MiddleValueMatches()
    {
        var channel = _fixture.Create<Kanaal>();
        channel.Naam = "zaken";
        channel.Filters = ["kenmerk_bron"];
        channel.AbonnementKanalen = [];

        var subscription = _fixture.Create<Abonnement>();
        subscription.AbonnementKanalen = [];
        subscription.Owner = "owner";

        var context = await SetupDbContext(c =>
        {
            c.Kanalen.Add(channel);
            c.Abonnementen.Add(subscription);
            c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = subscription,
                    Kanaal = channel,
                    Id = Guid.NewGuid(),
                    Filters = [new FilterValue { Key = "kenmerk_bron", Value = "DRN003" }],
                }
            );
        });

        var serviceProvider = BuildServiceCollection(context);

        var consumer = new SendNotificatiesConsumer(
            _logger.Object,
            serviceProvider,
            _memoryCache,
            _notificatieScheduler.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        notificatie.Setup(s => s.Kenmerken).Returns(new Dictionary<string, string> { { "kenmerk_bron", "DRN001;DRN002;DRN003;DRN004" } });

        _notificationFilterService
            .Setup(x => x.IsIgnored(It.IsAny<ISendNotificaties>(), It.IsAny<Abonnement>(), It.IsAny<AbonnementKanaal>()))
            .Returns(false);

        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        var headersMock = new Mock<Headers>();
        message.Setup(s => s.Headers).Returns(headersMock.Object);

        await consumer.Consume(message.Object);

        _notificatieScheduler.Verify(m => m.Enqueue(It.IsAny<Expression<Func<NotificatieJob, Task>>>()), Times.Once);
    }

    [Fact]
    public async Task SendNotification_WhenKenmerkBronFilter_WithMultipleSemicolonSeparatedValues_LastValueMatches()
    {
        var channel = _fixture.Create<Kanaal>();
        channel.Naam = "zaken";
        channel.Filters = ["kenmerk_bron"];
        channel.AbonnementKanalen = [];

        var subscription = _fixture.Create<Abonnement>();
        subscription.AbonnementKanalen = [];
        subscription.Owner = "owner";

        var context = await SetupDbContext(c =>
        {
            c.Kanalen.Add(channel);
            c.Abonnementen.Add(subscription);
            c.AbonnementKanalen.Add(
                new AbonnementKanaal
                {
                    Abonnement = subscription,
                    Kanaal = channel,
                    Id = Guid.NewGuid(),
                    Filters = [new FilterValue { Key = "kenmerk_bron", Value = "DRN005" }],
                }
            );
        });

        var serviceProvider = BuildServiceCollection(context);

        var consumer = new SendNotificatiesConsumer(
            _logger.Object,
            serviceProvider,
            _memoryCache,
            _notificatieScheduler.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        notificatie.Setup(s => s.Kenmerken).Returns(new Dictionary<string, string> { { "kenmerk_bron", "DRN001;DRN002;DRN003;DRN004;DRN005" } });

        _notificationFilterService
            .Setup(x => x.IsIgnored(It.IsAny<ISendNotificaties>(), It.IsAny<Abonnement>(), It.IsAny<AbonnementKanaal>()))
            .Returns(false);

        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        var headersMock = new Mock<Headers>();
        message.Setup(s => s.Headers).Returns(headersMock.Object);

        await consumer.Consume(message.Object);

        _notificatieScheduler.Verify(m => m.Enqueue(It.IsAny<Expression<Func<NotificatieJob, Task>>>()), Times.Once);
    }

    private static ServiceProvider BuildServiceCollection(NrcDbContext context)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(sp => context);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        return serviceProvider;
    }

    private static async Task<NrcDbContext> SetupDbContext(Action<NrcDbContext> setup)
    {
        var options = new DbContextOptionsBuilder<NrcDbContext>().UseInMemoryDatabase(databaseName: $"nrc-{Guid.NewGuid()}").Options;

        // Insert seed data into the database using one instance of the context
        var context = new NrcDbContext(options);

        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        setup?.Invoke(context);

        await context.SaveChangesAsync();

        return context;
    }
}
