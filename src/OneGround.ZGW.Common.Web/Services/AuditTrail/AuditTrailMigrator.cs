using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace OneGround.ZGW.Common.Web.Services.AuditTrail;

public interface IAuditTrailMigrator
{
    Task MigrateAsync(Guid hoofdObjectId, CancellationToken cancellationToken);
}

public class AuditTrailMigrator : IAuditTrailMigrator
{
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IServiceProvider _serviceProvider;

    public AuditTrailMigrator(IAuditTrailFactory auditTrailFactory, IServiceProvider serviceProvider)
    {
        _auditTrailFactory = auditTrailFactory;
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync(Guid hoofdObjectId, CancellationToken cancellationToken)
    {
        using var legacyAuditTrail = _auditTrailFactory.Create(new AuditTrailOptions(), legacy: true);

        foreach (var entries in await legacyAuditTrail.GetAuditTrailEntriesAsync(hoofdobjectId: hoofdObjectId))
        {
            using var scope = _serviceProvider.CreateScope();

            using var importer = scope.ServiceProvider.GetRequiredService<IDeltaBasedAuditTrailWithImporter>();

            await importer.ImportAsync(entries, cancellationToken: cancellationToken);
        }
    }
}

/*
   USAGE: (Later stadium probably ZTC due 3-level hierarchy (eg. catalogus + zaaktype + statustype) instead of 2 of ZRC, DRC, BRC)

    // Note: Run AuditTrailMigrator....

    var hoofdobjectId = new Guid("f767ccce-43a0-4ff4-84b8-13b7b502af63");

    await _auditTrailMigrator.MigrateAsync(hoofdobjectId, cancellationToken);

    //
    // Note: Run optional AuditTrailExporter (two exports: one legacy and one of migrated one)....

    await _auditTrailExporter.ExportAsync(hoofdobjectId, legacy: true, cancellationToken);

    await _auditTrailExporter.ExportAsync(hoofdobjectId, legacy: false, cancellationToken);

    // Make a comparison between two generated export files
*/
