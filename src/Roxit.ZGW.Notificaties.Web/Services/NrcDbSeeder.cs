using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roxit.ZGW.Common.Helpers;
using Roxit.ZGW.DataAccess;
using Roxit.ZGW.Notificaties.DataModel;
using Roxit.ZGW.Notificaties.Web.Configuration;

namespace Roxit.ZGW.Notificaties.Web.Services;

public class NrcDbSeeder : IDatabaseSeeder<NrcDbContext>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IUpsertSeeder _seeder;
    private readonly SeedDataConfiguration _seedDataConfiguration;

    public NrcDbSeeder(IServiceScopeFactory scopeFactory, IConfiguration configuration, IUpsertSeeder seeder)
    {
        _scopeFactory = scopeFactory;
        _seeder = seeder;
        _seedDataConfiguration = configuration.GetSection("SeedData").Get<SeedDataConfiguration>();
    }

    public async Task SeedDataAsync()
    {
        using var serviceScope = _scopeFactory.CreateScope();
        using var context = serviceScope.ServiceProvider.GetService<NrcDbContext>();

        await SeedKanalenAsync(context);
        await SeedAbonnementenAsync(context);
        await SeedAbonnementKanalenAsync(context);
        await SeedFilterValuesAsync(context);
    }

    private async Task SeedKanalenAsync(NrcDbContext context)
    {
        var data = JsonSerializationHelper.ReadAndDeserializeJsonFromFileOrDefault<IList<Kanaal>>(_seedDataConfiguration?.kanalen);

        _seeder.Upsert(context.Kanalen, data);

        await context.SaveChangesAsync();
    }

    private async Task SeedFilterValuesAsync(NrcDbContext context)
    {
        var data = JsonSerializationHelper.ReadAndDeserializeJsonFromFileOrDefault<IList<FilterValue>>(_seedDataConfiguration?.FilterValues);

        _seeder.Upsert(context.FilterValues, data);

        await context.SaveChangesAsync();
    }

    private async Task SeedAbonnementKanalenAsync(NrcDbContext context)
    {
        var data = JsonSerializationHelper.ReadAndDeserializeJsonFromFileOrDefault<IList<AbonnementKanaal>>(
            _seedDataConfiguration?.AbonnementKanalen
        );

        _seeder.Upsert(context.AbonnementKanalen, data);

        await context.SaveChangesAsync();
    }

    private async Task SeedAbonnementenAsync(NrcDbContext context)
    {
        var data = JsonSerializationHelper.ReadAndDeserializeJsonFromFileOrDefault<IList<Abonnement>>(_seedDataConfiguration?.Abonnementen);

        _seeder.Upsert(context.Abonnementen, data);

        await context.SaveChangesAsync();
    }
}
