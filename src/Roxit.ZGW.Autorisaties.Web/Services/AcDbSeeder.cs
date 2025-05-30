using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roxit.ZGW.Autorisaties.DataModel;
using Roxit.ZGW.Autorisaties.Web.Configuration;
using Roxit.ZGW.Common.Helpers;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Autorisaties.Web.Services;

public class AcDbSeeder : IDatabaseSeeder<AcDbContext>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SeedDataConfiguration _seedDataConfiguration;
    private readonly IUpsertSeeder _seeder;

    public AcDbSeeder(IServiceScopeFactory scopeFactory, IConfiguration configuration, IUpsertSeeder seeder)
    {
        _scopeFactory = scopeFactory;
        _seeder = seeder;
        _seedDataConfiguration = configuration.GetSection("SeedData").Get<SeedDataConfiguration>();
    }

    public async Task SeedDataAsync()
    {
        using var serviceScope = _scopeFactory.CreateScope();
        using var context = serviceScope.ServiceProvider.GetService<AcDbContext>();
        await SeedApplicatiesAsync(context);
    }

    private async Task SeedApplicatiesAsync(AcDbContext context)
    {
        var data = JsonSerializationHelper.ReadAndDeserializeJsonFromFileOrDefault<IList<Applicatie>>(_seedDataConfiguration?.Applicaties);

        _seeder.Upsert(context.Applicaties, data);

        await context.SaveChangesAsync();
    }
}
