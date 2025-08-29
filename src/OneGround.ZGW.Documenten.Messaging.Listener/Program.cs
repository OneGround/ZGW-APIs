using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection;
using OneGround.ZGW.Documenten.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureHostDefaults(ServiceRoleName.DRC_LISTENER);

builder.Services.AddHealthChecks();

var serviceConfiguration = new ServiceConfiguration(builder.Configuration);
serviceConfiguration.ConfigureServices(builder.Services);
builder.Services.AddZGWSecretManager(builder.Configuration);
builder.Services.RegisterZgwTokenClient(builder.Configuration, builder.Environment);

var app = builder.Build();

app.MapHealthChecks("/health");

await app.RunAsync();
