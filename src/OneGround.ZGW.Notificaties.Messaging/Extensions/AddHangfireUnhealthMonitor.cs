using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Services;
using OneGround.ZGW.Notificaties.Messaging.UI;

namespace OneGround.ZGW.Notificaties.Messaging.Extensions;

public static class WebApplicationExtension
{
    const string UnhealthMonitorUrl = "unhealthmonitor";

    public static void AddHangfireUnhealthMonitor(this WebApplication app)
    {
        // Additionally add an extra custom Hangfire menu for health monitor
        DashboardRoutes.Routes.Add(
            $"/{UnhealthMonitorUrl}",
            new UnhealthMonitorDashboardPage(app.Services.GetRequiredService<ICircuitBreakerSubscriberHealthTracker>())
        );

        // The page's client-side script is served from its own route so the page carries no inline script
        DashboardRoutes.Routes.Add(UnhealthMonitorScriptPage.RoutePath, new UnhealthMonitorScriptPage());

        // Add a menu-item to the Hangfire Dashboard navigation bar
        NavigationMenu.Items.Add(page => new MenuItem("Unhealthy endpoints", $"/hangfire/{UnhealthMonitorUrl}"));
    }
}
