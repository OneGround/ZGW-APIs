using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Autorisaties.DataModel;
using Xunit;

namespace OneGround.ZGW.Autorisaties.WebApi.IntegrationTests;

[Collection(AutorisatiesApiCollection.Name)]
public class StartupTests
{
    private readonly AutorisatiesWebApplicationFactory _factory;

    public StartupTests(AutorisatiesApiFixture fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task Host_boots_and_the_schema_is_created_by_the_AcDbContext_migrations()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AcDbContext>();

        // EnsureCreated would create the tables without this history, so rows here mean the migrations ran
        var history = await context.Database.SqlQuery<string>($"""SELECT "MigrationId" AS "Value" FROM "__EFMigrationsHistory" """).ToListAsync();

        Assert.NotEmpty(history);
        Assert.Equal(context.Database.GetMigrations().OrderBy(m => m), history.OrderBy(m => m));
    }
}
