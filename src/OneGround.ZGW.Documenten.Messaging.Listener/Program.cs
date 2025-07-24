using Microsoft.Extensions.Hosting;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection;
using OneGround.ZGW.Documenten.Messaging;

var builder = Host.CreateApplicationBuilder();
builder.ConfigureHostDefaults(ServiceRoleName.DRC_LISTENER);

var serviceConfiguration = new ServiceConfiguration(builder.Configuration);
serviceConfiguration.ConfigureServices(builder.Services);
builder.Services.AddZGWSecretManager(builder.Configuration);
builder.Services.RegisterZgwTokenClient(builder.Configuration, builder.Environment);

var app = builder.Build();

app.Run();
