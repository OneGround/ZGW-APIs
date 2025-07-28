using Microsoft.AspNetCore.Builder;
using OneGround.ZGW.Autorisaties.Web;
using OneGround.ZGW.Autorisaties.Web.Services;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Web.Configuration;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureZgwWebHostDefaults(ServiceRoleName.AC);

var startup = new Startup(builder.Configuration);

builder.Services.AddZgwAuthentication<DbContextAuthorizationResolver>(builder.Configuration, builder.Environment);
builder.Services.RegisterZgwTokenClient(builder.Configuration, builder.Environment);

startup.ConfigureServices(builder.Services);

var app = builder.Build();

Startup.Configure(app, builder.Environment);

app.Run();
