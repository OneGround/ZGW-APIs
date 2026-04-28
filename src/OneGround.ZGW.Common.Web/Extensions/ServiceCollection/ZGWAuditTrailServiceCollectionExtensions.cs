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
        services.AddTransient<AuditTrailService>();
        services.AddTransient<DeltaBasedAuditTrail>();
        services.AddTransient<IDeltaBasedAuditTrailWithImporter, DeltaBasedAuditTrailWithImporter>();
        services.AddScoped<IAuditTrailFactory, AuditTrailFactory>();

        services.AddScoped<IAuditTrailMigrator, AuditTrailMigrator>();
#if DEBUG
        services.AddScoped<IAuditTrailExporter, AuditTrailExporter>(); // Note: only for testing (after migration)
#endif
        services.AddScoped(typeof(IDbContextWithAuditTrail), (f) => f.GetService<TDbContext>());
    }
}
