using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OneGround.ZGW.DataAccess;

public class DatabaseInitializer<TDbContext> : IDatabaseInitializer<TDbContext>
    where TDbContext : DbContext
{
    private readonly ILogger<DatabaseInitializer<TDbContext>> _logger;
    private readonly IConfiguration _configuration;
    private readonly IDatabaseSeeder<TDbContext> _databaseSeeder;
    private readonly IDatabaseVerifier _databaseVerifier;
    private readonly IDesignTimeDbContextFactory<TDbContext> _contextFactory;

    public DatabaseInitializer(
        ILogger<DatabaseInitializer<TDbContext>> logger,
        IConfiguration configuration,
        IDatabaseVerifier databaseVerifier,
        IDesignTimeDbContextFactory<TDbContext> contextFactory,
        IDatabaseSeeder<TDbContext> databaseSeeder
    )
        : this(logger, configuration, databaseVerifier, contextFactory)
    {
        _databaseSeeder = databaseSeeder;
    }

    public DatabaseInitializer(
        ILogger<DatabaseInitializer<TDbContext>> logger,
        IConfiguration configuration,
        IDatabaseVerifier databaseVerifier,
        IDesignTimeDbContextFactory<TDbContext> contextFactory
    )
    {
        _logger = logger;
        _configuration = configuration;
        _databaseVerifier = databaseVerifier;
        _contextFactory = contextFactory;
    }

    public async Task InitializeAsync()
    {
        using var context = _contextFactory.CreateDbContext([]);

        // verify admin db context connection
        await _databaseVerifier.VerifyAsync(context);

        await MigrateAsync(context);
        await SeedAsync(context);

        // verify user db context connection
        await _databaseVerifier.VerifyAsync<TDbContext>();
    }

    private async Task MigrateAsync(TDbContext context)
    {
        var contextName = context.GetType().Name;

        var skipMigrationsAtStartup = _configuration.GetValue<bool>("Application:SkipMigrationsAtStartup");
        if (skipMigrationsAtStartup)
        {
            _logger.LogWarning("{ContextName}: Database migration is skipped!", contextName);
            return;
        }

        try
        {
            var pendingMigrations = context.Database.GetPendingMigrations().ToList();
            if (pendingMigrations.Count == 0)
            {
                _logger.LogInformation("{contextName}: No pending migrations", contextName);
            }

            foreach (var migration in pendingMigrations)
            {
                _logger.LogInformation("{ContextName}: Applying migration {Migration}", contextName, migration);
            }

            context.Database.SetCommandTimeout(TimeSpan.FromMinutes(60));
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ContextName}: An error occurred while migrating the database", contextName);
            throw;
        }
    }

    private async Task SeedAsync(TDbContext context)
    {
        var contextName = context.GetType().Name;

        try
        {
            var applyFixtures = _configuration.GetValue<bool>("Application:ApplyFixturesAtStartup");
            if (!applyFixtures)
            {
                return;
            }

            if (_databaseSeeder == null)
            {
                _logger.LogWarning(
                    "{ContextName}: ApplyFixturesAtStartup is set to true, but application has no database seeder provided",
                    contextName
                );
            }
            else
            {
                _logger.LogInformation("{ContextName}: Starting data seeding", contextName);
                await _databaseSeeder.SeedDataAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ContextName}: An error occurred while seeding the database", contextName);
            throw;
        }
    }
}
