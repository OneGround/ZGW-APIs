using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Autorisaties.ServiceAgent;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Web.Authentication;
using OneGround.ZGW.Documenten.Messaging;
using Roxit.ZGW.Documenten.Jobs.Subscription;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureHostDefaults(ServiceRoleName.DRC_LISTENER);

var serviceConfiguration = new ServiceConfiguration(builder.Configuration);
serviceConfiguration.ConfigureServices(builder.Services);

builder.Services.AddZGWSecretManager(builder.Configuration); // ???

// For OSS
builder.Services.AddZgwAuthentication<AutorisatiesServiceAgentAuthorizationResolver>(builder.Configuration, builder.Environment);
builder.Services.RegisterZgwTokenClient(builder.Configuration, builder.Environment);

var app = builder.Build();

app.MapHealthChecks("/health");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var recurringJobManager = app.Services.GetService<IRecurringJobManager>();

recurringJobManager.AddOrUpdate<CreateOrPatchSubscriptionJob>("refresh-token", job => job.ExecuteAsync("000000000"), Cron.Minutely);

await app.RunAsync();
