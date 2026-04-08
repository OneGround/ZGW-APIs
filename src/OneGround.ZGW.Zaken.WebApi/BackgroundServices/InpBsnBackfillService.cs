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

public class InpBsnBackfillService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InpBsnBackfillService> _logger;

    public InpBsnBackfillService(IServiceProvider serviceProvider, ILogger<InpBsnBackfillService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();

        var records = await db.Set<NatuurlijkPersoonZaakRol>().Where(r => r.InpBsn != null && r.InpBsnHash == null).ToListAsync(cancellationToken);

        if (records.Count == 0)
            return;

        foreach (var record in records)
        {
            record.InpBsnHash = record.InpBsn;
            record.InpBsnEncrypted = record.InpBsn;
        }

        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("InpBsn backfill complete. Migrated {Count} records.", records.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
