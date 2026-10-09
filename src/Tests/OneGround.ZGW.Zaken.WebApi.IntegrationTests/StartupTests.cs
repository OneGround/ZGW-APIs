using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Zaken.DataModel;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

[Collection(ZakenApiCollection.Name)]
public class StartupTests
{
    private readonly ZakenWebApplicationFactory _factory;

    public StartupTests(ZakenApiFixture fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task Host_boots_and_the_schema_is_created_by_the_ZrcDbContext_migrations()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();

        // EnsureCreated would create the tables without this history, so rows here mean the migrations ran
        var history = await context.Database.SqlQuery<string>($"""SELECT "MigrationId" AS "Value" FROM "__EFMigrationsHistory" """).ToListAsync();

        Assert.NotEmpty(history);
        Assert.Equal(context.Database.GetMigrations().OrderBy(m => m), history.OrderBy(m => m));
    }
}
