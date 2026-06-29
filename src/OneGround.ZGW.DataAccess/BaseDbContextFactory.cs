using System;
using System.IO;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace OneGround.ZGW.DataAccess;

public abstract class BaseDbContextFactory<TDbContext> : IDesignTimeDbContextFactory<TDbContext>, IDisposable
    where TDbContext : DbContext
{
    protected readonly IConfiguration Configuration;
    protected readonly string ConnectionString;

    // An NpgsqlDataSource owns a connection pool and is meant to be long-lived. This factory is
    // registered as a singleton, so the data source is built once and shared by every CreateDbContext
    // call, then disposed with the factory. Building one per call would leak a pool on each call,
    // because EF does not dispose an externally-supplied NpgsqlDataSource.
    private readonly Lazy<NpgsqlDataSource> _dataSource;

    /// <summary>
    /// Used when constructing from running host.
    /// </summary>
    public BaseDbContextFactory(IConfiguration configuration, string connectionStringName = "AdminConnectionString")
    {
        Configuration = configuration;

        ConnectionString = configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException($"No valid {connectionStringName} specified in appsettings.json section: ConnectionStrings.");

        _dataSource = CreateLazyDataSource();
    }

    /// <summary>
    /// Used when constructing using EF migration tools
    /// </summary>
    public BaseDbContextFactory(string connectionStringName = "AdminConnectionString")
    {
        var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.Local.json").Build();

        ConnectionString = configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException($"No valid {connectionStringName} specified in appsettings.json section: ConnectionStrings.");

        _dataSource = CreateLazyDataSource();
    }

    protected DbContextOptionsBuilder<TDbContext> CreateDbContextOptionsBuilder()
    {
        var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();
        optionsBuilder.UseNpgsql(_dataSource.Value, x => x.UseNetTopologySuite().UseNodaTime());

        return optionsBuilder;
    }

    public abstract TDbContext CreateDbContext(string[] args);

    public void Dispose()
    {
        if (_dataSource.IsValueCreated)
            _dataSource.Value.Dispose();

        GC.SuppressFinalize(this);
    }

    private Lazy<NpgsqlDataSource> CreateLazyDataSource()
    {
        // ExecutionAndPublication: the singleton factory can be called concurrently (e.g. per-request
        // gateway lookups), so the data source must be built exactly once.
        return new Lazy<NpgsqlDataSource>(
            () =>
            {
                var builder = new NpgsqlDataSourceBuilder(ConnectionString);
                builder.UseNodaTime();
                return builder.Build();
            },
            LazyThreadSafetyMode.ExecutionAndPublication
        );
    }
}
