using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace OneGround.ZGW.Documenten.Messaging;

public class DocumentenHangfireConnectionFactory : IConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;

    public DocumentenHangfireConnectionFactory([FromKeyedServices("hangfire-documenten")] NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    NpgsqlConnection IConnectionFactory.GetOrCreateConnection()
    {
        return _dataSource.OpenConnection();
    }
}
