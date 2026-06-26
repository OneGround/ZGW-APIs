using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace OneGround.ZGW.Notificaties.Messaging.Jobs;

public class NotificatiesHangfireConnectionFactory : IConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;

    public NotificatiesHangfireConnectionFactory([FromKeyedServices("hangfire-notificaties")] NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public NpgsqlConnection GetOrCreateConnection()
    {
        return _dataSource.OpenConnection();
    }
}
