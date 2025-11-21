using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Notificaties.Messaging;
using OneGround.ZGW.Notificaties.Messaging.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureHostDefaults(ServiceRoleName.NRC_LISTENER);

builder.Services.AddHealthChecks();

var serviceConfiguration = new ServiceConfiguration(builder.Configuration);
serviceConfiguration.ConfigureServices(builder.Services);

builder.Services.AddSingleton<INotificationFilterService, NotificationFilterService>();
builder.Services.AddZGWSecretManager(builder.Configuration);

var app = builder.Build();

app.MapHealthChecks("/health");

if (app.Environment.IsLocal())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions { Authorization = new[] { new HangfireDashboardAuthorizationFilter() } });
}

await app.RunAsync();

// Hangfire Dashboard Authorization Filter
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // TODO: We can make a feature-toggle: UseHangfireDashbord in .env, Default.env and/or appsettings.json

        // Allow authenticated users
        //return httpContext.User.Identity?.IsAuthenticated ?? false;

        return true;
    }
}
