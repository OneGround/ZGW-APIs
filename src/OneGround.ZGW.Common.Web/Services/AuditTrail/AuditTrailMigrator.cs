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
        var legacyAuditTrail = _auditTrailFactory.Create(new AuditTrailOptions(), legacy: true);

        foreach (var entities in await legacyAuditTrail.GetAuditTrailEntriesAsync(hoofdobjectId: hoofdObjectId))
        {
            using var scope = _serviceProvider.CreateScope();

            using var importer = scope.ServiceProvider.GetRequiredService<IDeltaBasedAuditTrailWithImporter>();

            await importer.ImportAsync(entities, cancellationToken: cancellationToken);
        }
    }
}
