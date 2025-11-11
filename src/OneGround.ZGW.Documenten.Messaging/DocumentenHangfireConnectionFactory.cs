using Hangfire.PostgreSql;
using Microsoft.Extensions.Options;
using Npgsql;

namespace OneGround.ZGW.Documenten.Messaging;

public class DocumentenHangfireConnectionFactory : IConnectionFactory
{
    private readonly IOptions<DocumentenJobsOptions> _options;

    public DocumentenHangfireConnectionFactory(IOptions<DocumentenJobsOptions> options)
    {
        _options = options;
    }

    NpgsqlConnection IConnectionFactory.GetOrCreateConnection()
    {
        var connection = new NpgsqlConnection(_options.Value.ConnectionString);
        connection.Open();
        return connection;
    }
}
