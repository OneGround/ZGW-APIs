using Hangfire;
using Hangfire.Console;
using Hangfire.Server;

namespace OneGround.ZGW.Notificaties.Messaging.Jobs.Notificatie;

public interface INotificatieManagementJob
{
    void ExpireFailedJobsScanAt(TimeSpan maxAgeFailedJob, PerformContext context = null);
}

[Queue(Constants.NrcListenerQueue)]
public class NotificatieManagementJob : INotificatieManagementJob
{
    public void ExpireFailedJobsScanAt(TimeSpan maxAgeFailedJob, PerformContext context = null)
    {
        const int pageSize = 100;

        var monitor = JobStorage.Current.GetMonitoringApi();
        var total = monitor.FailedCount();

        var jobsToDelete = new List<string>();

        for (int from = 0; from < total; from += pageSize)
        {
            int count = (int)Math.Min(pageSize, total - from);
            if (count <= 0)
                break;

            var failedJobsBatch = monitor.FailedJobs(from, count);

            var filteredFailedJobsBatch = failedJobsBatch.Where(kvp => kvp.Value.Job?.Type == typeof(NotificatieJob));

            foreach (var kv in filteredFailedJobsBatch)
            {
                var jobId = kv.Key;
                var dto = kv.Value;

                if (dto.FailedAt.HasValue && dto.FailedAt.Value < DateTime.UtcNow - maxAgeFailedJob)
                {
                    jobsToDelete.Add(jobId);
                }
            }
        }

        foreach (var jobId in jobsToDelete)
        {
            BackgroundJob.Delete(jobId); // Set Job state Deleted -> will expire afterward automatically
        }

        if (jobsToDelete.Any())
        {
            context.WriteLineColored(ConsoleTextColor.Yellow, $"{jobsToDelete.Count} failed Notificatie jobs older than {maxAgeFailedJob} deleted.");
        }
        else
        {
            context.WriteLineColored(ConsoleTextColor.Yellow, $"No Failed Notificatie jobs older than {maxAgeFailedJob} found to delete.");
        }
    }
}
