using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using OneGround.ZGW.Common.Configuration;

namespace OneGround.ZGW.DataAccess;

public static class DbContextExtensions
{
    public static void AddZGWDbContext<TDbContext>(this IServiceCollection services, IConfiguration configuration, string connectionStringName = null)
        where TDbContext : DbContext
    {
        var usedConnectionString = connectionStringName ?? "UserConnectionString";
        var userConnectionString = configuration.GetConnectionString(usedConnectionString);
        if (string.IsNullOrWhiteSpace(userConnectionString))
            throw new InvalidOperationException($"No valid '{usedConnectionString}' specified in appSettings.json section: ConnectionStrings.");

        var databaseConfiguration = configuration.GetSection("Database").Get<DatabaseConfiguration>() ?? new DatabaseConfiguration();
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(userConnectionString)
        {
            ApplicationName = typeof(TDbContext).Name,
            Pooling = databaseConfiguration.EnablePooling,
            MinPoolSize = databaseConfiguration.MinPoolSize,
            MaxPoolSize = databaseConfiguration.MaxPoolSize,
            ConnectionIdleLifetime = databaseConfiguration.ConnectionIdleLifetime,
            ConnectionPruningInterval = databaseConfiguration.ConnectionPruningInterval,
        };

        var npgsqlDataSourceBuilder = new NpgsqlDataSourceBuilder(connectionStringBuilder.ToString());
        npgsqlDataSourceBuilder.EnableDynamicJson().UseNodaTime().UseNetTopologySuite();

        var npgsqlDataSource = npgsqlDataSourceBuilder.Build();

        services.AddDbContext<TDbContext>(
            (serviceProvider, dbContextOptionsBuilder) =>
            {
                dbContextOptionsBuilder.UseNpgsql(
                    npgsqlDataSource,
                    o =>
                    {
                        o.UseNodaTime();
                        o.UseNetTopologySuite();
                    }
                );
                var hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
                if (hostEnvironment.IsLocal())
                {
                    dbContextOptionsBuilder.EnableSensitiveDataLogging();
                }
            }
        );

        services.AddTransient<ITemporaryTableProvider, TemporaryTableProvider>();
    }

    public static void AddDatabaseInitializerService<TDbContext, TDbContextFactory>(this IServiceCollection services)
        where TDbContextFactory : class, IDesignTimeDbContextFactory<TDbContext>
        where TDbContext : DbContext
    {
        services.AddSingleton<IDatabaseVerifier, DatabaseVerifier>();
        services.AddSingleton<IDatabaseInitializer<TDbContext>, DatabaseInitializer<TDbContext>>();
        services.AddSingleton<IDesignTimeDbContextFactory<TDbContext>, TDbContextFactory>();

        services.AddHostedService<DatabaseInitializerService<TDbContext>>();
    }

    public static void AddDatabaseInitializerService<TDbContext, TDbContextFactory, TDbSeeder>(this IServiceCollection services)
        where TDbSeeder : class, IDatabaseSeeder<TDbContext>
        where TDbContextFactory : class, IDesignTimeDbContextFactory<TDbContext>
        where TDbContext : DbContext
    {
        services.AddDatabaseInitializerService<TDbContext, TDbContextFactory>();

        services.AddSingleton<IUpsertSeeder, UpsertSeeder>();
        services.AddSingleton<IDatabaseSeeder<TDbContext>, TDbSeeder>();
    }
}
