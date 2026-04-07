using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace OneGround.ZGW.DataAccess;

public abstract class BaseDbContextFactory<TDbContext> : IDesignTimeDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    protected readonly IConfiguration Configuration;
    protected readonly string ConnectionString;

    /// <summary>
    /// Used when constructing from running host.
    /// </summary>
    public BaseDbContextFactory(IConfiguration configuration, string connectionStringName = "AdminConnectionString")
    {
        Configuration = configuration;

        ConnectionString = configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException($"No valid {connectionStringName} specified in appsettings.json section: ConnectionStrings.");
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
    }

    protected DbContextOptionsBuilder<TDbContext> CreateDbContextOptionsBuilder()
    {
        var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();
        optionsBuilder.UseNpgsql(
            ConnectionString,
            x =>
            {
                x.UseNetTopologySuite().UseNodaTime();
            }
        );

        return optionsBuilder;
    }

    public abstract TDbContext CreateDbContext(string[] args);
}
