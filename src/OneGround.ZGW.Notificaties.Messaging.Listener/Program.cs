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
    app.UseHangfireDashboard("/hangfire", new DashboardOptions());
}

await app.RunAsync();
