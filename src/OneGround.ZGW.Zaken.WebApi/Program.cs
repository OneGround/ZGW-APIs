using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Autorisaties.ServiceAgent;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.DataProtection.DataModel;
using OneGround.ZGW.Common.Web.Authentication;
using OneGround.ZGW.Common.Web.Configuration;
using OneGround.ZGW.Zaken.Web;
using OneGround.ZGW.Zaken.WebApi.BackgroundServices;
using OneGround.ZGW.Zaken.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureZgwWebHostDefaults(ServiceRoleName.ZRC);

var startup = new Startup(builder.Configuration);

builder.Services.AddZakenDataProtection(builder.Configuration);
builder.Services.AddZgwAuthentication<AutorisatiesServiceAgentAuthorizationResolver>(builder.Configuration, builder.Environment);
builder.Services.RegisterZgwTokenClient(builder.Configuration, builder.Environment);

startup.ConfigureServices(builder.Services);

builder.Services.AddDataProtectionDbContext(builder.Configuration);
builder.Services.AddHostedService<InpBsnBackfillService>();

var app = builder.Build();

await app.MigrateDataProtectionDatabaseAsync();

Startup.Configure(app, builder.Environment);

app.Run();
