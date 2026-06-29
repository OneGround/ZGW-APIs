using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using OneGround.ZGW.Common.Batching;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Messaging;
using OneGround.ZGW.Notificaties.Messaging.Consumers;
using OneGround.ZGW.Notificaties.Messaging.Jobs.Notificatie;
using OneGround.ZGW.Notificaties.Messaging.Services;
using Xunit;

namespace OneGround.ZGW.Notificaties.Listener.UnitTests.Jobs;

public class NotificatieJobTests
{
    private readonly Mock<INotificationSender> _notificationSenderMock = new();
    private readonly Mock<IAbonnementService> _abonnementServiceMock = new();
    private readonly NotificatieJob _sut;

    private static readonly Guid AbonnementId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    public NotificatieJobTests()
    {
        _sut = new NotificatieJob(
            _notificationSenderMock.Object,
            Mock.Of<IBatchIdAccessor>(),
            Mock.Of<ICorrelationContextAccessor>(),
            Mock.Of<ILogger<NotificatieJob>>(),
            _abonnementServiceMock.Object
        );
    }

    private static SubscriberNotificatie CreateNotificatie() =>
        new()
        {
            Rsin = "000000000",
            CorrelationId = Guid.NewGuid(),
            Kanaal = "zaken",
        };

    [Fact]
    public async Task ReQueueNotificatieAsync_WhenAbonnementIsBlocked_ThrowsSubscriptionBlockedExceptionWithoutSending()
    {
        _abonnementServiceMock
            .Setup(s => s.GetByIdAsync(AbonnementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new Abonnement
                {
                    Id = AbonnementId,
                    CallbackUrl = "https://example.com/webhook",
                    Auth = "Bearer token",
                    Blocked = true,
                }
            );

        await Assert.ThrowsAsync<SubscriptionBlockedException>(() => _sut.ReQueueNotificatieAsync(AbonnementId, CreateNotificatie(), context: null));

        _notificationSenderMock.Verify(
            s => s.SendAsync(It.IsAny<INotificatie>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ReQueueNotificatieAsync_WhenAbonnementIsNotBlocked_SendsWithAbonnementId()
    {
        _abonnementServiceMock
            .Setup(s => s.GetByIdAsync(AbonnementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new Abonnement
                {
                    Id = AbonnementId,
                    CallbackUrl = "https://example.com/webhook",
                    Auth = "Bearer token",
                    Blocked = false,
                }
            );
        _notificationSenderMock
            .Setup(s =>
                s.SendAsync(It.IsAny<INotificatie>(), AbonnementId, "https://example.com/webhook", "Bearer token", It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new SubscriberResult { Success = true });

        await _sut.ReQueueNotificatieAsync(AbonnementId, CreateNotificatie(), context: null);

        _notificationSenderMock.Verify(
            s => s.SendAsync(It.IsAny<INotificatie>(), AbonnementId, "https://example.com/webhook", "Bearer token", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
