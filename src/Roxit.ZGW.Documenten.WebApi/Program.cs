using Microsoft.AspNetCore.Builder;
using Roxit.ZGW.Autorisaties.ServiceAgent;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Web.Configuration;
using Roxit.ZGW.Common.Web.Extensions.ServiceCollection;
using Roxit.ZGW.Documenten.Web;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureZgwWebHostDefaults(ServiceRoleName.DRC);

var startup = new Startup(builder.Configuration);

builder.Services.AddZGWAuthentication<AutorisatiesServiceAgentAuthorizationResolver>(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

Startup.Configure(app, builder.Environment);

app.Run();
