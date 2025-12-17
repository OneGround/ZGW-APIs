using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Batching;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.Notificaties.Contracts.v1;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Services;
using OneGround.ZGW.Notificaties.Messaging.Configuration;

namespace OneGround.ZGW.Notificaties.Messaging;

public interface INotificationSender
{
    public Task<SubscriberResult> SendAsync(INotificatie notificatie, string url, string auth, CancellationToken cancellationToken = default);
}

public class NotificationSender : INotificationSender
{
    private readonly ILogger<NotificationSender> _logger;
    private readonly HttpClient _client;
    private readonly IBatchIdAccessor _batchIdAccessor;
    private readonly ICorrelationContextAccessor _correlationIdAccessor;
    private readonly ICircuitBreakerSubscriberHealthTracker _healthTracker;

    public NotificationSender(
        ILogger<NotificationSender> logger,
        HttpClient client,
        IBatchIdAccessor batchIdAccessor,
        ICorrelationContextAccessor correlationIdAccessor,
        ICircuitBreakerSubscriberHealthTracker healthTracker,
        IOptions<ApplicationOptions> applicationOptions
    )
    {
        _logger = logger;
        _client = client;
        _client.Timeout = applicationOptions.Value.CallbackTimeout;
        _batchIdAccessor = batchIdAccessor;
        _correlationIdAccessor = correlationIdAccessor;
        _healthTracker = healthTracker;
    }

    public async Task<SubscriberResult> SendAsync(INotificatie notificatie, string url, string auth, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notificatie);

        // Circuit breaker: Check if subscriber is healthy before attempting to send
        var isHealthy = await SafeHealthCheckAsync(url, cancellationToken);
        if (!isHealthy)
        {
            _logger.LogWarning(
                "{NotificationSender}: Notification skipped - circuit breaker is OPEN. Subscriber: {Url}, Channel: {Kanaal}",
                nameof(NotificationSender),
                url,
                notificatie.Kanaal
            );

            return new SubscriberResult { Success = false, Message = "Circuit breaker open - subscriber is unhealthy" };
        }

        try
        {
            if (AuthenticationHeaderValue.TryParse(auth, out var authorization))
            {
                _client.DefaultRequestHeaders.Authorization = authorization;
            }
            else
            {
                // Note: Log this error as warning because it is the client consumer responsibility to set up correctly
                _logger.LogWarning("Failed to parse authorization token. Check notification subscription auth field value.");
            }

            var request = new NotificatieDto
            {
                Aanmaakdatum = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                Kanaal = notificatie.Kanaal,
                Actie = $"{notificatie.Actie}",
                HoofdObject = notificatie.HoofdObject,
                Resource = notificatie.Resource,
                ResourceUrl = notificatie.ResourceUrl,
                Kenmerken = notificatie.Kenmerken,
            };

            _logger.LogInformation(
                "{NotificationSender}: Posting notification to channel '{Kanaal}' subscriber '{url}'...",
                nameof(NotificationSender),
                notificatie.Kanaal,
                url
            );

            var requestBody = JsonConvert.SerializeObject(request);
            using var httpRequest = new HttpRequestMessage();
            httpRequest.Method = HttpMethod.Post;
            httpRequest.RequestUri = new Uri(url);
            httpRequest.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            if (!string.IsNullOrEmpty(_batchIdAccessor?.Id))
            {
                httpRequest.Headers.Add("X-Batch-Id", _batchIdAccessor.Id);
            }

            if (!string.IsNullOrEmpty(_correlationIdAccessor?.CorrelationId))
            {
                httpRequest.Headers.Add("X-Correlation-Id", _correlationIdAccessor.CorrelationId);
            }

            var response = await _client.SendAsync(httpRequest, cancellationToken);

            _logger.LogInformation(
                "{NotificationSender}: Notification to channel '{Kanaal}' subscriber '{url}' responded with HTTP status code {StatusCode}",
                nameof(NotificationSender),
                notificatie.Kanaal,
                url,
                (int)response.StatusCode
            );

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                // Circuit breaker: Mark subscriber as healthy on success
                await _healthTracker.MarkHealthyAsync(url, cancellationToken);

                _logger.LogDebug(
                    "{NotificationSender}: Subscriber marked as healthy. URL: {Url}, Channel: {Kanaal}",
                    nameof(NotificationSender),
                    url,
                    notificatie.Kanaal
                );

                return new SubscriberResult { Success = true, StatusCode = response.StatusCode };
            }
            else
            {
                // Circuit breaker: Mark subscriber as unhealthy on HTTP error
                await _healthTracker.MarkUnhealthyAsync(url, $"HTTP {(int)response.StatusCode}", (int)response.StatusCode, cancellationToken);

                _logger.LogWarning(
                    "{NotificationSender}: Subscriber marked as unhealthy due to HTTP error. URL: {Url}, Channel: {Kanaal}, Status: {StatusCode}",
                    nameof(NotificationSender),
                    url,
                    notificatie.Kanaal,
                    (int)response.StatusCode
                );

                return new SubscriberResult
                {
                    Success = false,
                    StatusCode = response.StatusCode,
                    Message = responseBody,
                };
            }
        }
        catch (TaskCanceledException tce)
        {
            // Circuit breaker: Mark subscriber as unhealthy on timeout
            await _healthTracker.MarkUnhealthyAsync(
                url,
                "Request timeout",
                statusCode: 408, // Request Timeout
                cancellationToken
            );

            // Note: Don't log the complete error stacktrace here (client consumer)
            _logger.LogWarning(
                tce,
                "{NotificationSender}: Notification to channel {Kanaal} subscriber '{url}' timed out. Subscriber marked as unhealthy.",
                nameof(NotificationSender),
                notificatie.Kanaal,
                url
            );

            return new SubscriberResult { Success = false, Message = tce.Message };
        }
        catch (HttpRequestException httpEx)
        {
            // Circuit breaker: Mark subscriber as unhealthy on connection error
            await _healthTracker.MarkUnhealthyAsync(url, httpEx.Message, statusCode: (int?)httpEx.StatusCode, cancellationToken);

            _logger.LogWarning(
                httpEx,
                "{NotificationSender}: Notification to channel {Kanaal} subscriber '{url}' failed due to connection error. Subscriber marked as unhealthy. Error: {Error}",
                nameof(NotificationSender),
                notificatie.Kanaal,
                url,
                httpEx.Message
            );

            return new SubscriberResult { Success = false, Message = httpEx.Message };
        }
        catch (Exception ex)
        {
            // Circuit breaker: Mark subscriber as unhealthy on unexpected error
            await _healthTracker.MarkUnhealthyAsync(url, "Unexpected error", statusCode: null, cancellationToken);

            // Note: Don't log the complete error stacktrace here (client consumer)
            _logger.LogWarning(
                "{NotificationSender}: Notification to channel {Kanaal} subscriber '{url}' has failed. Subscriber marked as unhealthy. Error: {Error}",
                nameof(NotificationSender),
                notificatie.Kanaal,
                url,
                ex.Message
            );

            return new SubscriberResult { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// Safely checks subscriber health with fail-open behavior.
    /// If health tracker fails, allows notification through.
    /// </summary>
    private async Task<bool> SafeHealthCheckAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            return await _healthTracker.IsHealthyAsync(url, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "{NotificationSender}: Health check failed for subscriber {Url}. Treating as healthy (fail-open behavior).",
                nameof(NotificationSender),
                url
            );

            // Fail-open: If health tracker is broken, don't block notifications
            return true;
        }
    }
}
