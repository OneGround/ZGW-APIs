using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Hosting;
using OneGround.ZGW.Documenten.Jobs.Subscription;

namespace OneGround.ZGW.Documenten.Messaging.Listener.Services;

public class ManageSubscriptionsHostedService : IHostedService
{
    private const string LockResource = "manage-subscriptions-init-lock";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var jobId = ManageSubscriptionsJob.GetJobId();
        // Use distributed lock to ensure only one instance initializes the job
        using (var connection = JobStorage.Current.GetConnection())
        {
            // Check if job already exists to avoid re-triggering
            var existingJob = connection.GetRecurringJobs().FirstOrDefault(j => j.Id == jobId);

            if (existingJob == null || existingJob.LastExecution == null)
            {
                RecurringJob.AddOrUpdate<ManageSubscriptionsJob>(jobId, job => job.ExecuteAsync(), Cron.Never);
                RecurringJob.TriggerJob(jobId);
            }
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
