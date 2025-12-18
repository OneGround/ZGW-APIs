using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Documenten.Jobs.Subscription;

namespace OneGround.ZGW.Documenten.Messaging.Listener.Services;

public class ManageSubscriptionsHostedService : IHostedService
{
    private readonly ILogger<ManageSubscriptionsHostedService> _logger;

    public ManageSubscriptionsHostedService(ILogger<ManageSubscriptionsHostedService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var jobId = ManageSubscriptionsJob.GetJobId();
        try
        {
            // Initialize job if it doesn't exist or has never been triggered
            var existingJob = JobStorage.Current.GetConnection().GetRecurringJobs().FirstOrDefault(j => j.Id == jobId);

            if (existingJob != null)
            {
                _logger.LogDebug("Recurring job {JobId} already exists, skipping initialization", jobId);
            }
            else
            {
                RecurringJob.AddOrUpdate<ManageSubscriptionsJob>(jobId, job => job.ExecuteAsync(), Cron.Never);
                RecurringJob.TriggerJob(jobId);
                _logger.LogInformation("Created and triggered recurring job {JobId}.", jobId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize recurring job {JobId}", jobId);
            throw;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping {HostedService}", nameof(ManageSubscriptionsHostedService));
        return Task.CompletedTask;
    }
}
