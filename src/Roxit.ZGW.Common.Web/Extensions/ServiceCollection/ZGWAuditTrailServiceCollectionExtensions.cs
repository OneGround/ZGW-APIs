using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.DataAccess.AuditTrail;

namespace Roxit.ZGW.Common.Web.Extensions.ServiceCollection;

public static class ZGWAuditTrailServiceCollectionExtensions
{
    public static void AddZGWAuditTrail<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext, IDbContextWithAuditTrail
    {
        services.AddTransient<IAuditTrailService, AuditTrailService>();
        services.AddScoped<IAuditTrailFactory, AuditTrailFactory>();
        services.AddScoped(typeof(IDbContextWithAuditTrail), (f) => f.GetService<TDbContext>());
    }
}
