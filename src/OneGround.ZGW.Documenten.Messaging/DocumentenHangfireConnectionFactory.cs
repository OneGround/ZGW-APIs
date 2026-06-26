using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace OneGround.ZGW.Documenten.Messaging;

public class DocumentenHangfireConnectionFactory : IConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;

    public DocumentenHangfireConnectionFactory([FromKeyedServices(HangfireServiceKeys.DataSource)] NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    NpgsqlConnection IConnectionFactory.GetOrCreateConnection()
    {
        return _dataSource.OpenConnection();
    }
}
