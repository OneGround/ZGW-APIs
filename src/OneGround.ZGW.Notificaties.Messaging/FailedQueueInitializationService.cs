using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OneGround.ZGW.Notificaties.Messaging.Configuration;
using RabbitMQ.Client;

namespace OneGround.ZGW.Notificaties.Messaging;

public class FailedQueueInitializationService : IHostedService
{
    private readonly NotificatiesEventBusConfiguration _eventBusConfiguration;

    public FailedQueueInitializationService(IConfiguration configuration)
    {
        _eventBusConfiguration =
            configuration.GetSection("Eventbus").Get<NotificatiesEventBusConfiguration>() ?? new NotificatiesEventBusConfiguration();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        const string queueAndExchangeName = "notificatie-subscriber-dlq";

        var factory = new ConnectionFactory
        {
            HostName = _eventBusConfiguration.HostName,
            VirtualHost = _eventBusConfiguration.VirtualHost,
            UserName = _eventBusConfiguration.UserName,
            Password = _eventBusConfiguration.Password,
        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);

        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: queueAndExchangeName,
            durable: true,
            autoDelete: false,
            arguments: null,
            type: "fanout",
            cancellationToken: cancellationToken
        );

        await channel.QueueDeclareAsync(
            queue: queueAndExchangeName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );

        await channel.QueueBindAsync(
            queue: queueAndExchangeName,
            exchange: queueAndExchangeName,
            routingKey: "",
            cancellationToken: cancellationToken
        );
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
