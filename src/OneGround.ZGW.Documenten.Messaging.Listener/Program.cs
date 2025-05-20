using Microsoft.Extensions.Hosting;
using OneGround.ZGW.Common.Configuration;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Extensions;
using OneGround.ZGW.Documenten.Messaging;

var builder = Host.CreateDefaultBuilder();
builder.ConfigureZgwListenerHostDefaults(ServiceRoleName.DRC_LISTENER);

builder.ConfigureServices(
    (hostContext, services) =>
    {
        var serviceConfiguration = new ServiceConfiguration(hostContext.Configuration);
        serviceConfiguration.ConfigureServices(services);
        services.AddZGWSecretManager(hostContext.Configuration);
    }
);

var app = builder.Build();

app.Run();
