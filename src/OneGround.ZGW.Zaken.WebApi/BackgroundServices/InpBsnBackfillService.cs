using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;

namespace OneGround.ZGW.Zaken.WebApi.BackgroundServices;

public class InpBsnBackfillService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InpBsnBackfillService> _logger;

    public InpBsnBackfillService(IServiceProvider serviceProvider, ILogger<InpBsnBackfillService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int batchSize = 5_000;
        int totalMigrated = 0;
        int batchCount;

        do
        {
            using var scope = _serviceProvider.CreateScope();
            await using var db = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();

            var batch = await db.Set<NatuurlijkPersoonZaakRol>()
                .Where(r => r.InpBsn != null && r.InpBsnHash == null)
                .Take(batchSize)
                .ToListAsync(stoppingToken);

            batchCount = batch.Count;

            foreach (var record in batch)
            {
                record.InpBsnHash = record.InpBsn;
                record.InpBsnEncrypted = record.InpBsn;
            }

            await db.SaveChangesAsync(stoppingToken);
            db.ChangeTracker.Clear();

            totalMigrated += batchCount;

            _logger.LogInformation("Migrated {BatchCount} InpBsn records to Hash and Encrypted (total: {TotalMigrated})", batchCount, totalMigrated);

            if (batchCount > 0)
            {
                await Task.Delay(50, stoppingToken);
            }
        } while (batchCount > 0);

        _logger.LogInformation("Completed InpBsn migration. Total records migrated: {TotalMigrated}", totalMigrated);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
