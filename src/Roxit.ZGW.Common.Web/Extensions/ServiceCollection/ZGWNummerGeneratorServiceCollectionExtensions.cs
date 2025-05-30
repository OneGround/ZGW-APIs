using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Roxit.ZGW.Common.Contracts;
using Roxit.ZGW.Common.Web.Services.NumberGenerator;
using Roxit.ZGW.DataAccess.NumberGenerator;

namespace Roxit.ZGW.Common.Web.Extensions.ServiceCollection;

public static class ZGWNummerGeneratorServiceCollectionExtensions
{
    public static void AddZGWNummerGenerator<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext, IDbContextWithNummerGenerator
    {
        services.AddScoped<ISqlCommandExecutor, SqlCommandExecutor<TDbContext>>();
        services.AddScoped<INummerGenerator, NummerGenerator<TDbContext>>();
    }
}
