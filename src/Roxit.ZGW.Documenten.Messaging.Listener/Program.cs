using Microsoft.Extensions.Hosting;
using Roxit.ZGW.Common.Configuration;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Extensions;
using Roxit.ZGW.Documenten.Messaging;

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
