using Microsoft.AspNetCore.Builder;
using Roxit.ZGW.Autorisaties.Web;
using Roxit.ZGW.Autorisaties.Web.Services;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Web.Configuration;
using Roxit.ZGW.Common.Web.Extensions.ServiceCollection;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureZgwWebHostDefaults(ServiceRoleName.AC);

var startup = new Startup(builder.Configuration);

builder.Services.AddZGWAuthentication<DbContextAuthorizationResolver>(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

Startup.Configure(app, builder.Environment);

app.Run();
