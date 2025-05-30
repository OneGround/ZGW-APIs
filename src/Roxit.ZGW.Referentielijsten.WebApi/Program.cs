using Microsoft.AspNetCore.Builder;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Web.Configuration;
using Roxit.ZGW.Referentielijsten.Web;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureZgwWebHostDefaults(ServiceRoleName.RL);

var startup = new Startup(builder.Configuration);

startup.ConfigureServices(builder.Services);

var app = builder.Build();

Startup.Configure(app, builder.Environment);

app.Run();
