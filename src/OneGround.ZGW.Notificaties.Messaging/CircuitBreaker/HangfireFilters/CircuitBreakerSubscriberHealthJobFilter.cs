using Hangfire.Server;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Services;
using OneGround.ZGW.Notificaties.Messaging.Jobs.Notificatie;

namespace OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.HangfireFilters;

public class CircuitBreakerSubscriberHealthJobFilter(
    ICircuitBreakerSubscriberHealthTracker healthTracker,
    ILogger<CircuitBreakerSubscriberHealthJobFilter> logger
) : IServerFilter
{
    public void OnPerforming(PerformingContext filterContext)
    {
        // Only apply to NotificatieJob.ReQueueNotificatieAsync
        if (
            filterContext.BackgroundJob.Job.Type != typeof(NotificatieJob)
            || filterContext.BackgroundJob.Job.Method.Name != nameof(NotificatieJob.ReQueueNotificatieAsync)
        )
        {
            return;
        }

        // Extract SubscriberNotificatie from job arguments (3rd parameter)
        var subscriberNotificatie = TryGetSubscriberNotificatie(filterContext);
        if (subscriberNotificatie == null)
        {
            logger.LogWarning("Unable to extract SubscriberNotificatie from Hangfire job {JobId}. Job will proceed.", filterContext.BackgroundJob.Id);
            return;
        }

        // Extract subscriber URL from the 4th and 5th parameters (callbackUrl or subscriberUrl)
        var subscriberUrl = TryGetSubscriberUrl(filterContext);
        if (string.IsNullOrEmpty(subscriberUrl))
        {
            logger.LogWarning("Unable to extract subscriber URL from Hangfire job {JobId}. Job will proceed.", filterContext.BackgroundJob.Id);
            return;
        }

        // Check if a subscriber is healthy
        var isHealthy = healthTracker.IsHealthyAsync(subscriberUrl).GetAwaiter().GetResult();
        if (isHealthy)
        {
            return;
        }

        var healthState = healthTracker.GetHealthStateAsync(subscriberUrl).GetAwaiter().GetResult();

        logger.LogWarning(
            "Canceling Hangfire job {JobId} for unhealthy subscriber '{SubscriberUrl}'. "
                + "Circuit is OPEN. Blocked until {BlockedUntil}. Consecutive failures: {ConsecutiveFailures}. "
                + "Notification for RSIN: {Rsin}, Kanaal: {Kanaal}, Resource: {Resource}, Actie: {Actie}. "
                + "Job will be automatically retried later.",
            filterContext.BackgroundJob.Id,
            subscriberUrl,
            healthState?.BlockedUntil,
            healthState?.ConsecutiveFailures,
            subscriberNotificatie.Rsin,
            subscriberNotificatie.Kanaal,
            subscriberNotificatie.Resource,
            subscriberNotificatie.Actie
        );

        // Cancel the job - Hangfire will automatically requeue it based on retry policy
        filterContext.Canceled = true;
    }

    public void OnPerformed(PerformedContext filterContext) { }

    private SubscriberNotificatie? TryGetSubscriberNotificatie(PerformingContext context)
    {
        try
        {
            // ReQueueNotificatieAsync signature: (Guid abonnementId, SubscriberNotificatie subscriberNotificatie, string? callbackUrl, string? subscriberUrl)
            // SubscriberNotificatie is the 2nd parameter (index 1)
            if (context.BackgroundJob.Job.Args.Count > 1 && context.BackgroundJob.Job.Args[1] is SubscriberNotificatie subscriberNotificatie)
            {
                return subscriberNotificatie;
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error extracting SubscriberNotificatie from Hangfire job {JobId}.", context.BackgroundJob.Id);
            return null;
        }
    }

    private string? TryGetSubscriberUrl(PerformingContext context)
    {
        try
        {
            // ReQueueNotificatieAsync signature: (Guid abonnementId, SubscriberNotificatie subscriberNotificatie, string? callbackUrl, string? subscriberUrl)
            // callbackUrl is the 3rd parameter (index 2), subscriberUrl is the 4th parameter (index 3)

            // Try callbackUrl first (index 2)
            if (
                context.BackgroundJob.Job.Args.Count > 2
                && context.BackgroundJob.Job.Args[2] is string callbackUrl
                && !string.IsNullOrEmpty(callbackUrl)
            )
            {
                return callbackUrl;
            }

            // Fall back to subscriberUrl (index 3)
            if (
                context.BackgroundJob.Job.Args.Count > 3
                && context.BackgroundJob.Job.Args[3] is string subscriberUrl
                && !string.IsNullOrEmpty(subscriberUrl)
            )
            {
                return subscriberUrl;
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error extracting subscriber URL from Hangfire job {JobId}.", context.BackgroundJob.Id);
            return null;
        }
    }
}
