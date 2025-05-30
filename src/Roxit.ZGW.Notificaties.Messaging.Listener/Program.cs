using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Roxit.ZGW.Common.Configuration;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Extensions;
using Roxit.ZGW.Notificaties.Messaging;
using Roxit.ZGW.Notificaties.Messaging.Consumers;

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
