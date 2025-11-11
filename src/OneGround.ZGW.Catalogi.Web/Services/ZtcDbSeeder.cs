using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Configuration;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Catalogi.Web.Services;

public class ZtcDbSeeder : IDatabaseSeeder<ZtcDbContext>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IUpsertSeeder _seeder;
    private readonly SeedDataConfiguration _seedDataConfiguration;

    public ZtcDbSeeder(IServiceScopeFactory scopeFactory, IConfiguration configuration, IUpsertSeeder seeder)
    {
        _scopeFactory = scopeFactory;
        _seeder = seeder;
        _seedDataConfiguration = configuration.GetSection("SeedData").Get<SeedDataConfiguration>();
    }

    public async Task SeedDataAsync()
    {
        using var serviceScope = _scopeFactory.CreateScope();
        using var context = serviceScope.ServiceProvider.GetService<ZtcDbContext>();

        await SeedCatalogussenAsync(context);
        await SeedZaakTypenAsync(context);
        await SeedStatusTypenAsync(context);
        await SeedRolTypenAsync(context);
        await SeedResultaattypenAsync(context);
        await SeedBesluitTypenAsync(context);
        await SeedInformatieObjectTypenAsync(context);
        await SeedZaakTypeInformatieObjectTypenAsync(context);
        await SeedReferentieProcessenAsync(context);
    }

    private async Task SeedZaakTypeInformatieObjectTypenAsync(ZtcDbContext context)
    {
        var data = JsonSerializationHelper.ReadAndDeserializeJsonFromFileOrDefault<IList<ZaakTypeInformatieObjectType>>(
            _seedDataConfiguration?.ZaakTypeInformatieObjectTypen
        );

        _seeder.Upsert(context.ZaakTypeInformatieObjectTypen, data);

        // TODO: ENABLE!!!!
        // await context.SaveChangesAsync();
    }

    private async Task SeedInformatieObjectTypenAsync(ZtcDbContext context)
    {
        var data = JsonSerializationHelper.ReadAndDeserializeJsonFromFileOrDefault<IList<InformatieObjectType>>(
            _seedDataConfiguration?.InformatieObjectTypen
        );

        _seeder.Upsert(context.InformatieObjectTypen, data);

        await context.SaveChangesAsync();
    }

    private async Task SeedBesluitTypenAsync(ZtcDbContext context)
    {
        var data = JsonSerializationHelper.ReadAndDeserializeJsonFromFileOrDefault<IList<BesluitType>>(_seedDataConfiguration?.BesluitTypen);

        _seeder.Upsert(context.BesluitTypen, data);

        await context.SaveChangesAsync();
    }

    private async Task SeedZaakTypenAsync(ZtcDbContext context)
    {
        var data = JsonSerializationHelper.ReadAndDeserializeJsonFromFileOrDefault<IList<ZaakType>>(_seedDataConfiguration?.Zaaktypen);

        _seeder.Upsert(context.ZaakTypen, data);

        await context.SaveChangesAsync();
    }

    private async Task SeedStatusTypenAsync(ZtcDbContext context)
    {
        var data = JsonSerializationHelper.ReadAndDeserializeJsonFromFileOrDefault<IList<StatusType>>(_seedDataConfiguration?.Statustypen);

        _seeder.Upsert(context.StatusTypen, data);

        await context.SaveChangesAsync();
    }

    private async Task SeedRolTypenAsync(ZtcDbContext context)
    {
        var data = JsonSerializationHelper.ReadAndDeserializeJsonFromFileOrDefault<IList<RolType>>(_seedDataConfiguration?.Roltypen);

        _seeder.Upsert(context.RolTypen, data);

        await context.SaveChangesAsync();
    }

    private async Task SeedCatalogussenAsync(ZtcDbContext context)
    {
        var data = JsonSerializationHelper.ReadAndDeserializeJsonFromFileOrDefault<IList<Catalogus>>(_seedDataConfiguration?.Catalogussen);

        _seeder.Upsert(context.Catalogussen, data);

        await context.SaveChangesAsync();
    }

    private async Task SeedResultaattypenAsync(ZtcDbContext context)
    {
        var data = JsonSerializationHelper.ReadAndDeserializeJsonFromFileOrDefault<IList<ResultaatType>>(_seedDataConfiguration?.Resultaattypen);

        _seeder.Upsert(context.ResultaatTypen, data);

        await context.SaveChangesAsync();
    }

    private async Task SeedReferentieProcessenAsync(ZtcDbContext context)
    {
        var data = JsonSerializationHelper.ReadAndDeserializeJsonFromFileOrDefault<IList<ReferentieProces>>(
            _seedDataConfiguration?.ReferentieProcessen
        );

        _seeder.Upsert(context.ReferentieProcessen, data);

        // TODO: ENABLE!!!!
        //    await context.SaveChangesAsync();
    }
}
