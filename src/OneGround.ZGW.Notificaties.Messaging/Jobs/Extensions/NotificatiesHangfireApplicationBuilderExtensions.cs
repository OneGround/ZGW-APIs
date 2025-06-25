using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace OneGround.ZGW.Notificaties.Messaging.Jobs.Extensions;

public static class NotificatiesHangfireApplicationBuilderExtensions
{
    public static void UseNotificatiesHangfireDashboard(
        this IApplicationBuilder app,
        string pathMatch = "/hangfire",
        Action<DashboardOptions> optionsBuilder = null
    )
    {
        var options = new DashboardOptions
        {
            DashboardTitle = "DRC Hangfire Dashboard",
            DisplayStorageConnectionString = true,
            IgnoreAntiforgeryToken = true,
        };

        optionsBuilder?.Invoke(options);

        app.UseHangfireDashboard(
            pathMatch,
            options,
            new PostgreSqlStorage(
                app.ApplicationServices.GetRequiredService<NotificatiesHangfireConnectionFactory>(),
                new PostgreSqlStorageOptions() { PrepareSchemaIfNecessary = false }
            )
        );
    }
}
