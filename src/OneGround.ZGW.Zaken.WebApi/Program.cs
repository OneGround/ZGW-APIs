using Microsoft.AspNetCore.Builder;
using OneGround.ZGW.Autorisaties.ServiceAgent;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Web.Authentication;
using OneGround.ZGW.Common.Web.Configuration;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureZgwWebHostDefaults(ServiceRoleName.ZRC);

var startup = new Startup(builder.Configuration);

builder.Services.AddZgwAuthentication<AutorisatiesServiceAgentAuthorizationResolver>(builder.Configuration, builder.Environment);
builder.Services.RegisterZgwTokenClient(builder.Configuration, builder.Environment);

startup.ConfigureServices(builder.Services);

// OSS-only: register DataProtection DbContext and migration initializer (Internal uses Consul key storage)
builder.Services.AddZGWDbContext<DataProtectionKeyDbContext>(builder.Configuration);
builder.Services.AddDatabaseInitializerService<DataProtectionKeyDbContext, DataProtectionKeyDbContextFactory>();

var app = builder.Build();

Startup.Configure(app, builder.Environment);

app.Run();
