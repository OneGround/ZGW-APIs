using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.DataAccess.AuditTrail;

namespace OneGround.ZGW.Common.Web.Extensions.ServiceCollection;

public static class ZGWAuditTrailServiceCollectionExtensions
{
    public static void AddZGWAuditTrail<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext, IDbContextWithAuditTrail
    {
        services.AddTransient<IAuditTrailService, AuditTrailService>();
        services.AddTransient<IAuditTrailService, DeltaBasedAuditTrail>();
        services.AddTransient<IDeltaBasedAuditTrailWithImporter, DeltaBasedAuditTrailWithImporter>(); // Note: Only needed if we want to have Import functionality (which DataMigrator does)
        services.AddScoped<IAuditTrailFactory, AuditTrailFactory>();

        services.AddScoped<IAuditTrailMigrator, AuditTrailMigrator>();
        services.AddScoped<IAuditTrailExporter, AuditTrailExporter>(); // Note: This only for testing purposes and is not used in Production.

        services.AddScoped(typeof(IDbContextWithAuditTrail), (f) => f.GetService<TDbContext>());
    }
}
