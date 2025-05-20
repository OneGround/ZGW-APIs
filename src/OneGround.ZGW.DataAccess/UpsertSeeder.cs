using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace OneGround.ZGW.DataAccess;

public class UpsertSeeder : IUpsertSeeder
{
    private readonly ILogger<UpsertSeeder> _logger;

    public UpsertSeeder(ILogger<UpsertSeeder> logger)
    {
        _logger = logger;
    }

    public void Upsert<T>(DbSet<T> dbEntities, IEnumerable<T> seedEntities)
        where T : class, IBaseEntity
    {
        if (seedEntities == null)
        {
            return;
        }

        var entityName = typeof(T).Name;
        var existingIds = dbEntities.Select(s => s.Id).ToArray();

        // Note: We can fix and support UPSERT later. For now adding is supported only
        //var updateEntities = seedEntities.Where(s => existingIds.Contains(s.Id)).ToArray();
        //_logger.LogInformation($"Updating {updateEntities.Length} existing entities '{entityName}'.");
        //dbEntities.UpdateRange(updateEntities);

        var newEntities = seedEntities.Where(s => !existingIds.Contains(s.Id)).ToArray();
        _logger.LogInformation("Adding {Length} new entities '{entityName}'.", newEntities.Length, entityName);
        dbEntities.AddRange(newEntities);
    }
}
