using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Contracts;
using OneGround.ZGW.Common.Web.Services.NumberGenerator;
using OneGround.ZGW.DataAccess.NumberGenerator;

namespace OneGround.ZGW.Common.Web.Extensions.ServiceCollection;

public static class ZGWNummerGeneratorServiceCollectionExtensions
{
    public static void AddZGWNummerGenerator<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext, IDbContextWithNummerGenerator
    {
        services.AddScoped<ISqlCommandExecutor, SqlCommandExecutor<TDbContext>>();
        services.AddScoped<INummerGenerator, NummerGenerator<TDbContext>>();
    }
}
