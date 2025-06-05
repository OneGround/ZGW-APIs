using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Messaging;
using OneGround.ZGW.Notificaties.Messaging.Consumers;
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
    private readonly Mock<IPublishEndpoint> _publishEndpoint;
    private readonly IMemoryCache _memoryCache;
    private readonly IConfiguration _configuration;
    private readonly Mock<INotificationFilterService> _notificationFilterService;

    public SendNotificatiesConsumerTests()
    {
        _logger = new Mock<ILogger<SendNotificatiesConsumer>>();

        _publishEndpoint = new Mock<IPublishEndpoint>();

        _publishEndpoint.Setup(m =>
            m.Publish(It.IsAny<INotifySubscriber>(), It.IsAny<IPipe<PublishContext<INotifySubscriber>>>(), It.IsAny<CancellationToken>())
        );

        _notificationFilterService = new Mock<INotificationFilterService>();
        _notificationFilterService
            .Setup(x => x.IsIgnored(It.IsAny<ISendNotificaties>(), It.IsAny<Abonnement>(), It.IsAny<AbonnementKanaal>()))
            .Returns(true);

        var inMemorySettings = new Dictionary<string, string>
        {
            { "Application:AbonnementenCacheExpirationTime", "00:02:00" },
            { "Eventbus:UseSeparateRetryQueue", "true" },
        };

        _configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        _memoryCache = new MockMemoryCache();
    }

    [Fact]
    public async Task SendNotification_WhenNoFiltersPresent()
    {
        var channel = _fixture.Create<Kanaal>();
        channel.Naam = "zaken";
        channel.Filters = [];
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
            _publishEndpoint.Object,
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

        await consumer.Consume(message.Object);

        _publishEndpoint.Verify(
            m => m.Publish(It.IsAny<INotifySubscriber>(), It.IsAny<IPipe<PublishContext<INotifySubscriber>>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
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
            _publishEndpoint.Object,
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

        await consumer.Consume(message.Object);

        _publishEndpoint.Verify(
            m => m.Publish(It.IsAny<INotifySubscriber>(), It.IsAny<IPipe<PublishContext<INotifySubscriber>>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
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
            _publishEndpoint.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        notificatie.Setup(s => s.Kenmerken).Returns(new Dictionary<string, string> { { "bronorganisatie", "123" } });

        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        await consumer.Consume(message.Object);

        _publishEndpoint.Verify(
            m => m.Publish(It.IsAny<INotifySubscriber>(), It.IsAny<IPipe<PublishContext<INotifySubscriber>>>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SendNotification_WhenFilterDoesMatch_CaseInsensitive()
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
                    Filters = [new FilterValue { Key = "bronOrganisatie", Value = "123" }],
                }
            );
        });

        var serviceProvider = BuildServiceCollection(context);

        var consumer = new SendNotificatiesConsumer(
            _logger.Object,
            serviceProvider,
            _memoryCache,
            _publishEndpoint.Object,
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

        await consumer.Consume(message.Object);

        _publishEndpoint.Verify(
            m => m.Publish(It.IsAny<INotifySubscriber>(), It.IsAny<IPipe<PublishContext<INotifySubscriber>>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
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
            _publishEndpoint.Object,
            _configuration,
            _notificationFilterService.Object
        );

        var notificatie = new Mock<ISendNotificaties>();
        notificatie.Setup(s => s.Kanaal).Returns("zaken");
        notificatie.Setup(s => s.Rsin).Returns("owner");
        notificatie.Setup(s => s.Ignore).Returns(true);

        var message = new Mock<ConsumeContext<ISendNotificaties>>();
        message.Setup(s => s.Message).Returns(notificatie.Object);

        await consumer.Consume(message.Object);

        _publishEndpoint.Verify(
            m => m.Publish(It.IsAny<INotifySubscriber>(), It.IsAny<IPipe<PublishContext<INotifySubscriber>>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
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
