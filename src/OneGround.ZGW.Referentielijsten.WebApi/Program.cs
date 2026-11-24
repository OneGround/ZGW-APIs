using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Web.Configuration;
using OneGround.ZGW.Referentielijsten.Web;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureZgwWebHostDefaults(ServiceRoleName.RL);

var startup = new Startup(builder.Configuration);

startup.ConfigureServices(builder.Services);

var app = builder.Build();

try
{
    Startup.Configure(app, builder.Environment);
}
catch (Exception ex)
{
    // Fail fast, but make sure the reason ends up in the log (e.g. invalid reference data); the exception is rethrown so hosts and tests still see it.
    app.Logger.LogCritical(ex, "The application failed to start: {Message}", ex.Message);
    await app.DisposeAsync(); // flushes the loggers
    throw;
}

app.Run();
