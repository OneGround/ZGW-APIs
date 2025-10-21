using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Batching;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.Notificaties.Contracts.v1;
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
    private readonly ApplicationConfiguration _applicationConfiguration;
    private readonly IBatchIdAccessor _batchIdAccessor;
    private readonly ICorrelationContextAccessor _correlationIdAccessor;

    public NotificationSender(
        IConfiguration configuration,
        ILogger<NotificationSender> logger,
        HttpClient client,
        IBatchIdAccessor batchIdAccessor,
        ICorrelationContextAccessor correlationIdAccessor
    )
    {
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
        _logger = logger;
        _client = client;
        _client.Timeout = _applicationConfiguration.CallbackTimeout;
        _batchIdAccessor = batchIdAccessor;
        _correlationIdAccessor = correlationIdAccessor;
    }

    public async Task<SubscriberResult> SendAsync(INotificatie notificatie, string url, string auth, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notificatie, nameof(notificatie));

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

            return response.IsSuccessStatusCode
                ? new SubscriberResult { Success = true }
                : new SubscriberResult
                {
                    Success = false,
                    StatusCode = response.StatusCode,
                    Message = responseBody,
                };
        }
        catch (TaskCanceledException tce)
        {
            // Note: Don't log the complete error stacktrace here (client consumer)
            _logger.LogWarning(
                tce,
                "{NotificationSender}: Notification to channel {Kanaal} subscriber '{url}' timed out.",
                nameof(NotificationSender),
                notificatie.Kanaal,
                url
            );

            return new SubscriberResult { Success = false, Message = tce.Message };
        }
        catch (Exception ex)
        {
            // Note: Don't log the complete error stacktrace here (client consumer)
            _logger.LogWarning(
                "{NotificationSender}: Notification to channel {Kanaal} subscriber '{url}' has failed. Error is: {error}",
                nameof(NotificationSender),
                notificatie.Kanaal,
                url,
                ex.Message
            );

            return new SubscriberResult { Success = false, Message = ex.Message };
        }
    }
}
