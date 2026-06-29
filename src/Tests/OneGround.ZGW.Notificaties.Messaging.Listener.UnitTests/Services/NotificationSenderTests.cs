using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OneGround.ZGW.Common.Batching;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Notificaties.Messaging;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Services;
using OneGround.ZGW.Notificaties.Messaging.Configuration;
using Xunit;

namespace OneGround.ZGW.Notificaties.Listener.UnitTests.Services;

public class NotificationSenderTests
{
    private readonly Mock<ICircuitBreakerSubscriberHealthTracker> _healthTrackerMock = new();
    private readonly StubHttpMessageHandler _handler = new();

    private static readonly Guid AbonnementId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private const string Url = "https://example.com/webhook";

    public NotificationSenderTests()
    {
        _healthTrackerMock.Setup(t => t.IsHealthyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private NotificationSender CreateSut()
    {
        var applicationOptions = new Mock<IOptions<ApplicationOptions>>();
        applicationOptions.Setup(o => o.Value).Returns(new ApplicationOptions { CallbackTimeout = TimeSpan.FromSeconds(30) });

        return new NotificationSender(
            Mock.Of<ILogger<NotificationSender>>(),
            new HttpClient(_handler),
            Mock.Of<IBatchIdAccessor>(),
            Mock.Of<ICorrelationContextAccessor>(),
            _healthTrackerMock.Object,
            applicationOptions.Object
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
    public async Task SendAsync_WhenDeliverySucceeds_ClearsFailingMarkerOnce()
    {
        _handler.ResponseStatusCode = HttpStatusCode.OK;
        var sut = CreateSut();

        var result = await sut.SendAsync(CreateNotificatie(), AbonnementId, Url, "Bearer token");

        Assert.True(result.Success);
        _healthTrackerMock.Verify(t => t.MarkHealthyAsync(AbonnementId, It.IsAny<CancellationToken>()), Times.Once);
        _healthTrackerMock.Verify(
            t => t.MarkUnhealthyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SendAsync_WhenDeliveryFails_MarksFailingOnce()
    {
        _handler.ResponseStatusCode = HttpStatusCode.InternalServerError;
        var sut = CreateSut();

        var result = await sut.SendAsync(CreateNotificatie(), AbonnementId, Url, "Bearer token");

        Assert.False(result.Success);
        _healthTrackerMock.Verify(
            t => t.MarkUnhealthyAsync(AbonnementId, Url, It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _healthTrackerMock.Verify(t => t.MarkHealthyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.OK;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(ResponseStatusCode) { Content = new StringContent("") });
        }
    }
}
