using Hangfire.PostgreSql;
using Microsoft.Extensions.Options;
using Npgsql;

namespace OneGround.ZGW.Notificaties.Messaging.Jobs;

public class NotificatiesHangfireConnectionFactory : IConnectionFactory
{
    private readonly IOptions<NotificatiesJobsOptions> _options;

    public NotificatiesHangfireConnectionFactory(IOptions<NotificatiesJobsOptions> options)
    {
        _options = options;
    }

    public NpgsqlConnection GetOrCreateConnection()
    {
        var connection = new NpgsqlConnection(_options.Value.ConnectionString);
        connection.Open();
        return connection;
    }
}
