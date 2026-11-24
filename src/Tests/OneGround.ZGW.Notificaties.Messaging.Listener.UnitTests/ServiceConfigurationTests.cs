using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Notificaties.Messaging;
using OneGround.ZGW.Notificaties.Messaging.Consumers;
using Xunit;

namespace OneGround.ZGW.Notificaties.Listener.UnitTests;

public class ServiceConfigurationTests
{
    [Fact]
    public void ConfigureServices_Registers_All_Dependencies_Of_Listener_Services()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string>
                {
                    ["ConnectionStrings:UserConnectionString"] = "Host=localhost;Database=nrc",
                    ["ConnectionStrings:HangfireConnectionString"] = "Host=localhost;Database=nrc",
                    ["PollyConfig:NotificatiesSender:Timeout:Timeout"] = "00:00:30",
                    ["Redis:ConnectionString"] = "localhost:6379,abortConnect=false",
                }
            )
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        new ServiceConfiguration(configuration).ConfigureServices(services);
        services.AddSingleton<INotificationFilterService, NotificationFilterService>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}
