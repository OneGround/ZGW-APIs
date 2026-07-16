using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Options;
using OneGround.ZGW.Notificaties.Messaging.Jobs.Notificatie;
using StackExchange.Redis;
using Xunit;

namespace OneGround.ZGW.Notificaties.Listener.UnitTests.Jobs;

public class BlockFailingSubscriptionsJobTests
{
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock = new();
    private readonly CircuitBreakerOptions _settings = new()
    {
        FailureThreshold = 3,
        BreakDuration = TimeSpan.FromMinutes(5),
        CacheExpirationMinutes = 10,
        BlockSubscriptionAfter = TimeSpan.FromDays(14),
    };

    private BlockFailingSubscriptionsJob CreateSut(IConnectionMultiplexer connectionMultiplexer)
    {
        var optionsMock = new Mock<IOptions<CircuitBreakerOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_settings);

        return new BlockFailingSubscriptionsJob(
            _cacheMock.Object,
            connectionMultiplexer,
            optionsMock.Object,
            _serviceScopeFactoryMock.Object,
            Mock.Of<ILogger<BlockFailingSubscriptionsJob>>()
        );
    }

    [Fact]
    public async Task BlockSubscriptionsAsync_BlocksOnlyUnblockedMatchingSubscriptions()
    {
        var unblockedMatching = Guid.NewGuid();
        var alreadyBlocked = Guid.NewGuid();
        var nonMatching = Guid.NewGuid();

        var databaseName = $"nrc-{Guid.NewGuid()}";

        await using (var seedContext = NewContext(databaseName))
        {
            seedContext.Database.EnsureCreated();
            seedContext.Abonnementen.Add(NewAbonnement(unblockedMatching, blocked: false));
            seedContext.Abonnementen.Add(NewAbonnement(alreadyBlocked, blocked: true));
            seedContext.Abonnementen.Add(NewAbonnement(nonMatching, blocked: false));
            await seedContext.SaveChangesAsync();
        }

        var serviceProvider = BuildServiceProvider(databaseName);
        _serviceScopeFactoryMock.Setup(f => f.CreateScope()).Returns(() => serviceProvider.CreateScope());

        var sut = CreateSut(Mock.Of<IConnectionMultiplexer>());

        var blockedCount = await sut.BlockSubscriptionsAsync(new[] { unblockedMatching, alreadyBlocked });

        Assert.Equal(1, blockedCount);

        await using var readContext = NewContext(databaseName);
        var reloaded = await readContext.Abonnementen.AsNoTracking().ToListAsync();
        Assert.True(reloaded.Single(a => a.Id == unblockedMatching).Blocked);
        Assert.True(reloaded.Single(a => a.Id == alreadyBlocked).Blocked);
        Assert.False(reloaded.Single(a => a.Id == nonMatching).Blocked);
    }

    [Fact]
    public async Task BlockFailingSubscriptionsAsync_WhenRedisUnavailable_DoesNotRethrow()
    {
        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock
            .Setup(r => r.GetEndPoints(It.IsAny<bool>()))
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "simulated unavailable"));

        var sut = CreateSut(redisMock.Object);

        var exception = await Record.ExceptionAsync(() => sut.BlockFailingSubscriptionsAsync(context: null));

        Assert.Null(exception);
    }

    private static Abonnement NewAbonnement(Guid id, bool blocked) =>
        new()
        {
            Id = id,
            Blocked = blocked,
            CallbackUrl = "https://example.com/webhook",
            Auth = "Bearer token",
            Owner = "00000000000",
        };

    private static NrcDbContext NewContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<NrcDbContext>().UseInMemoryDatabase(databaseName).Options;
        return new NrcDbContext(options);
    }

    private static ServiceProvider BuildServiceProvider(string databaseName)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped(_ => NewContext(databaseName));
        return serviceCollection.BuildServiceProvider();
    }
}
