using System;
using System.Collections.Generic;
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
        // Check whether the legacy inpbsn column still exists. After migration
        // 20260413000000_remove_inpbsn_plain_column_from_zaakrollen has run, there
        // is nothing left to backfill and this service exits immediately.
        bool columnExists;

        await using (var scope = _serviceProvider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();

            columnExists = await db
                .Database.SqlQuery<bool>(
                    $"""
                    SELECT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name  = 'zaakrollen_natuurlijk_personen'
                        AND column_name = 'inpbsn'
                    ) AS "Value"
                    """
                )
                .SingleAsync(stoppingToken);
        }
        if (!columnExists)
        {
            _logger.LogInformation("InpBsnBackfillService: inpbsn column no longer exists, nothing to backfill.");
            return;
        }

        const int batchSize = 5_000;
        int totalMigrated = 0;
        int batchCount;

        do
        {
            await using var batchScope = _serviceProvider.CreateAsyncScope();
            var batchDb = batchScope.ServiceProvider.GetRequiredService<ZrcDbContext>();

            // Read raw inpbsn values via SQL because the entity property has been removed.
            var rawRecords = await batchDb
                .Database.SqlQuery<InpBsnRawRecord>(
                    $"""
                    SELECT id, inpbsn AS bsn
                    FROM zaakrollen_natuurlijk_personen
                    WHERE inpbsn IS NOT NULL
                      AND inpbsn_hash IS NULL
                    LIMIT {batchSize}
                    """
                )
                .ToListAsync(stoppingToken);

            batchCount = rawRecords.Count;

            if (batchCount > 0)
            {
                var ids = new List<Guid>(batchCount);
                var bsnById = new Dictionary<Guid, string>(batchCount);
                foreach (var r in rawRecords)
                {
                    ids.Add(r.Id);
                    bsnById[r.Id] = r.Bsn;
                }

                var entities = await batchDb.Set<NatuurlijkPersoonZaakRol>().Where(e => ids.Contains(e.Id)).ToListAsync(stoppingToken);

                foreach (var entity in entities)
                {
                    var bsn = bsnById[entity.Id];
                    entity.InpBsnHash = bsn;
                    entity.InpBsnEncrypted = bsn;
                }

                await batchDb.SaveChangesAsync(stoppingToken);
                batchDb.ChangeTracker.Clear();

                totalMigrated += batchCount;

                _logger.LogInformation(
                    "Migrated {BatchCount} InpBsn records to Hash and Encrypted (total: {TotalMigrated})",
                    batchCount,
                    totalMigrated
                );

                await Task.Delay(50, stoppingToken);
            }
        } while (batchCount > 0);

        _logger.LogInformation("Completed InpBsn backfill. Total records migrated: {TotalMigrated}", totalMigrated);
    }

    // Keyless projection type for EF8 SqlQuery<T>
    private sealed class InpBsnRawRecord
    {
        public Guid Id { get; init; }
        public string Bsn { get; init; }
    }
}
