using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace OneGround.ZGW.IntegrationTests.Common.Containers;

/// <summary>
/// Starts the PostgreSQL (with PostGIS) and Redis containers an API needs to boot in-process.
/// Use it as an xUnit collection or class fixture, so the containers are started once and shared by the tests using it.
/// </summary>
public sealed class IntegrationTestContainers : IAsyncLifetime
{
    // Postgres 13 with PostGIS 3, the same major versions as the local development database (localdev/postgresql/Dockerfile)
    public const string PostgreSqlImage = "postgis/postgis:13-3.5";

    public const string RedisImage = "redis:6.0.10-alpine";

    private readonly PostgreSqlContainer _postgreSql = new PostgreSqlBuilder(PostgreSqlImage).Build();

    private readonly RedisContainer _redis = new RedisBuilder(RedisImage).Build();

    /// <summary>
    /// Npgsql connection string of a superuser on the container's database.
    /// </summary>
    public string PostgreSqlConnectionString => _postgreSql.GetConnectionString();

    /// <summary>
    /// StackExchange.Redis configuration string of the Redis container.
    /// </summary>
    public string RedisConnectionString => _redis.GetConnectionString();

    public Task InitializeAsync()
    {
        return Task.WhenAll(_postgreSql.StartAsync(), _redis.StartAsync());
    }

    public async Task DisposeAsync()
    {
        await _postgreSql.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
