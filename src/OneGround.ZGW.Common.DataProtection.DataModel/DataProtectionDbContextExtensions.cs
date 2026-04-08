using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OneGround.ZGW.Common.DataProtection.DataModel;

public static class DataProtectionDbContextExtensions
{
    public static IServiceCollection AddDataProtectionDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DataProtectionConnectionString");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("No valid 'DataProtectionConnectionString' specified in ConnectionStrings.");

        services.AddDbContext<DataProtectionKeyDbContext>(dbContextOptionsBuilder =>
        {
            dbContextOptionsBuilder.UseNpgsql(connectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", "data_protection"));
        });

        return services;
    }

    /// <summary>
    /// Migrates the DataProtection database schema eagerly during startup.
    /// Must be called before app.Run() to ensure the schema exists before ASP.NET DataProtection reads keys.
    /// </summary>
    public static async Task MigrateDataProtectionDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DataProtectionKeyDbContext>().Database.MigrateAsync();
    }
}
