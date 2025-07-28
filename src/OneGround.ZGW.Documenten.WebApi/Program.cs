using Microsoft.AspNetCore.Builder;
using OneGround.ZGW.Autorisaties.ServiceAgent;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Web.Configuration;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection;
using OneGround.ZGW.Documenten.Web;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureZgwWebHostDefaults(ServiceRoleName.DRC);

var startup = new Startup(builder.Configuration);

builder.Services.AddZgwAuthentication<AutorisatiesServiceAgentAuthorizationResolver>(builder.Configuration, builder.Environment);
builder.Services.RegisterZgwTokenClient(builder.Configuration, builder.Environment);

startup.ConfigureServices(builder.Services);

var app = builder.Build();

Startup.Configure(app, builder.Environment);

app.Run();
