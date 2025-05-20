using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Configuration;

namespace OneGround.ZGW.Zaken.Web.Services;

public class ZrcDbSeeder : IDatabaseSeeder<ZrcDbContext>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IUpsertSeeder _seeder;
    private readonly ApplicationConfiguration _applicationConfiguration;

    public ZrcDbSeeder(IServiceScopeFactory scopeFactory, IConfiguration configuration, IUpsertSeeder seeder)
    {
        _scopeFactory = scopeFactory;
        _seeder = seeder;
        _applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();
    }

    public async Task SeedDataAsync()
    {
        using var serviceScope = _scopeFactory.CreateScope();
        using var context = serviceScope.ServiceProvider.GetService<ZrcDbContext>();

        var data = JsonSerializationHelper.ReadAndDeserializeJsonFromFileOrDefault<IList<Zaak>>(_applicationConfiguration?.FixturesSource);

        _seeder.Upsert(context.Zaken, data);

        await context.SaveChangesAsync();
    }
}
