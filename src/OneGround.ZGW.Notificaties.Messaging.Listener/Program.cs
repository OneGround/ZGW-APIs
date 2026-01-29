using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Web.HealthChecks;
using OneGround.ZGW.Notificaties.Messaging;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Services;
using OneGround.ZGW.Notificaties.Messaging.Consumers;
using OneGround.ZGW.Notificaties.Messaging.UI;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureHostDefaults(ServiceRoleName.NRC_LISTENER);

var serviceConfiguration = new ServiceConfiguration(builder.Configuration);
serviceConfiguration.ConfigureServices(builder.Services);

builder.Services.AddSingleton<INotificationFilterService, NotificationFilterService>();
builder.Services.AddZGWSecretManager(builder.Configuration);

var app = builder.Build();

app.MapOneGroundHealthChecks();

if (app.Environment.IsLocal())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions { Authorization = new[] { new HangfireLocalAuthFilter() } });
}

// Additional add a extra Custom Hangfire menu for health monitor
DashboardRoutes.Routes.Add(
    "/unhealthmonitor",
    new UnhealthMonitorDashboardPage(
        app.Services.GetRequiredService<ICircuitBreakerSubscriberHealthTracker>())
);

// Add a menu-item to the Hangfire Dashboard navigation bar
NavigationMenu.Items.Add(page => new MenuItem("Unhealth Monitor", "/hangfire/unhealthmonitor"));

await app.RunAsync();
