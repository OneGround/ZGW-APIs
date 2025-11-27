using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Autorisaties.ServiceAgent;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Web.Authentication;
using OneGround.ZGW.Common.Web.HealthChecks;
using OneGround.ZGW.Documenten.Messaging;
using OneGround.ZGW.Documenten.Messaging.Listener.Services;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureHostDefaults(ServiceRoleName.DRC_LISTENER);

var serviceConfiguration = new ServiceConfiguration(builder.Configuration);
serviceConfiguration.ConfigureServices(builder.Services);

builder.Services.AddZGWSecretManager(builder.Configuration);

builder.Services.AddZgwAuthentication<AutorisatiesServiceAgentAuthorizationResolver>(builder.Configuration, builder.Environment);
builder.Services.RegisterZgwTokenClient(builder.Configuration, builder.Environment);

builder.Services.AddHostedService<ManageSubscriptionsHostedService>();

var app = builder.Build();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapOneGroundHealthChecks();

if (app.Environment.IsLocal())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions { Authorization = new[] { new HangfireLocalAuthFilter() } });
}

app.MapControllers();

await app.RunAsync();
