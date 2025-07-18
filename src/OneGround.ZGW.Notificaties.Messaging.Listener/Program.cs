using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Notificaties.Messaging;
using OneGround.ZGW.Notificaties.Messaging.Consumers;

var builder = Host.CreateApplicationBuilder();
builder.ConfigureHostDefaults(ServiceRoleName.NRC_LISTENER);

var serviceConfiguration = new ServiceConfiguration(builder.Configuration);
serviceConfiguration.ConfigureServices(builder.Services);

builder.Services.AddSingleton<INotificationFilterService, NotificationFilterService>();
builder.Services.AddZGWSecretManager(builder.Configuration);

var app = builder.Build();

app.Run();
