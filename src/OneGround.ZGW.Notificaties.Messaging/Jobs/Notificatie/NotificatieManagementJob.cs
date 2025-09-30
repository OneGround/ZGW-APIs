using Hangfire;

namespace OneGround.ZGW.Notificaties.Messaging.Jobs.Notificatie;

public interface INotificatieManagementJob
{
    void ExpireFailedJobsScanAt(TimeSpan maxAgeFailedJob);
}

[DisableConcurrentExecution(10)]
[Queue(Constants.NrcListenerQueue)]
public class NotificatieManagementJob : INotificatieManagementJob
{
    public void ExpireFailedJobsScanAt(TimeSpan maxAgeFailedJob)
    {
        const int pageSize = 100;

        var monitor = JobStorage.Current.GetMonitoringApi();
        var total = monitor.FailedCount();

        var jobsToDelete = new List<string>();

        for (int from = 0; from < total; from += pageSize)
        {
            var failedJobsBatch = monitor.FailedJobs(from, (int)Math.Min(pageSize, total - from));

            var filteredFailedJobsBatch = failedJobsBatch.Where(kvp => kvp.Value.Job.Type == typeof(NotificatieJob));

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
    }
}
