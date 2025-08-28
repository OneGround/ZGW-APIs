using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection;
using OneGround.ZGW.Documenten.Messaging;

var cts = new CancellationTokenSource();

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureHostDefaults(ServiceRoleName.DRC_LISTENER);

builder.Services.AddControllers();

builder.WebHost.ConfigureKestrel(
    (_, options) =>
    {
        options.AddServerHeader = false;
    }
);

builder.Services.AddHealthChecks();

var serviceConfiguration = new ServiceConfiguration(builder.Configuration);
serviceConfiguration.ConfigureServices(builder.Services);
builder.Services.AddZGWSecretManager(builder.Configuration);
builder.Services.RegisterZgwTokenClient(builder.Configuration, builder.Environment);

var app = builder.Build();

app.MapHealthChecks("/health");

app.UseRouting();

app.MapControllers();

await app.RunAsync(cts.Token);
