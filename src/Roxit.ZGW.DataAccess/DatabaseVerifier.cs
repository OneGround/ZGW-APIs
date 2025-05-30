using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Roxit.ZGW.DataAccess;

public interface IDatabaseVerifier
{
    void VerifyConnection<TDbContext>(TDbContext context)
        where TDbContext : DbContext;

    void VerifyConnection<TDbContext>()
        where TDbContext : DbContext;
}

public class DatabaseVerifier : IDatabaseVerifier
{
    private readonly ILogger<DatabaseVerifier> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public DatabaseVerifier(ILogger<DatabaseVerifier> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void VerifyConnection<TDbContext>()
        where TDbContext : DbContext
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        VerifyConnection(context);
    }

    public void VerifyConnection<TDbContext>(TDbContext context)
        where TDbContext : DbContext
    {
        var contextName = context.GetType().Name;
        var connectionString = GetConnectionString(context);

        if (!context.Database.CanConnect())
        {
            _logger.LogError(
                "{contextName}: Failed to connect to database using connection string: {connectionString}",
                contextName,
                connectionString
            );
            throw new Exception("Cannot verify connection to database.");
        }
    }

    private static string GetConnectionString<TDbContext>(TDbContext context)
        where TDbContext : DbContext
    {
        var connection = context.Database.GetDbConnection();
        var connectionStringBuilder = DbProviderFactories.GetFactory(connection).CreateConnectionStringBuilder();
        connectionStringBuilder.ConnectionString = connection.ConnectionString;

        var components = new Dictionary<string, object>();
        foreach (KeyValuePair<string, object> component in connectionStringBuilder)
        {
            if (component.Key.Equals("password", StringComparison.OrdinalIgnoreCase))
            {
                components[component.Key] = "<hidden>";
            }
            else
            {
                components[component.Key] = component.Value;
            }
        }

        connectionStringBuilder.Clear();
        foreach (var component in components.Reverse())
        {
            connectionStringBuilder.Add(component.Key, component.Value);
        }

        return connectionStringBuilder.ConnectionString;
    }
}
