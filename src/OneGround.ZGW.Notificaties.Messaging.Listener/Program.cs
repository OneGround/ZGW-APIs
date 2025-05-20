using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Notificaties.Messaging;
using OneGround.ZGW.Notificaties.Messaging.Consumers;

var builder = Host.CreateDefaultBuilder();
builder.ConfigureZgwListenerHostDefaults(ServiceRoleName.NRC_LISTENER);

builder.ConfigureServices(
    (hostContext, services) =>
    {
        var serviceConfiguration = new ServiceConfiguration(hostContext.Configuration);
        serviceConfiguration.ConfigureServices(services);

        services.AddSingleton<INotificationFilterService, NotificationFilterService>();
        services.AddZGWSecretManager(hostContext.Configuration);
    }
);

var app = builder.Build();

app.Run();
