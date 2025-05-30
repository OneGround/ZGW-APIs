using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Roxit.ZGW.DataAccess;

public class DatabaseInitializerService<TDbContext> : IHostedService
    where TDbContext : DbContext
{
    private readonly IDatabaseInitializer<TDbContext> _databaseInitializer;

    public DatabaseInitializerService(IDatabaseInitializer<TDbContext> databaseInitializer)
    {
        _databaseInitializer = databaseInitializer;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _databaseInitializer.InitializeAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
