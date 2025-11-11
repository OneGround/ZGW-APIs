using Microsoft.AspNetCore.Builder;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Web.Authentication;
using OneGround.ZGW.Documenten.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureHostDefaults(ServiceRoleName.DRC_LISTENER);

var serviceConfiguration = new ServiceConfiguration(builder.Configuration);
serviceConfiguration.ConfigureServices(builder.Services);
builder.Services.AddZGWSecretManager(builder.Configuration);
builder.Services.RegisterZgwTokenClient(builder.Configuration, builder.Environment);

var app = builder.Build();

app.MapHealthChecks("/health");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
