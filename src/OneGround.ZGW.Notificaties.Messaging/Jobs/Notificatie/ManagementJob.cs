using Hangfire;

namespace OneGround.ZGW.Notificaties.Messaging.Jobs.Notificatie;

public interface IManagementJob
{
    void ExpireFailedJobsScanAt(TimeSpan maxAgeFailedJob);
}

[DisableConcurrentExecution(10)]
[Queue(Constants.NrcListenerQueue)]
public class ManagementJob : IManagementJob
{
    public void ExpireFailedJobsScanAt(TimeSpan maxAgeFailedJob)
    {
        const int pageSize = 100;

        var monitor = JobStorage.Current.GetMonitoringApi();
        var total = monitor.FailedCount();

        var jobsToDelete = new List<string>();

        for (int from = 0; from < total; from += pageSize)
        {
            var batch = monitor.FailedJobs(from, (int)Math.Min(pageSize, total - from));
            foreach (var kv in batch)
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
