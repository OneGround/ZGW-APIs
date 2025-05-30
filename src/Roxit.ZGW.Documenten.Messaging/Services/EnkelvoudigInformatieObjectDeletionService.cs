using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using Roxit.ZGW.Common.Contracts.v1;

namespace Roxit.ZGW.Documenten.Messaging.Services;

public interface IEnkelvoudigInformatieObjectDeletionService
{
    Task<IEnumerable<DistinctEnkelvoudigInformatieObjectDeletion>> GetDistinctAsync(
        TimeSpan duration,
        int limit,
        string enabledForRsins,
        CancellationToken cancellationToken = default
    );
    Task DeleteAsync(IEnumerable<string> enkelvoudiginformatieobjectUrls, CancellationToken cancellationToken = default);
    Task DeleteReferencedObjectsAsync(CancellationToken cancellationToken = default);
    Task MarkAsErrorsAsync(Dictionary<string, ErrorResponse> enkelvoudiginformatieobjectUrls, CancellationToken cancellationToken);
    Task QueueAsync(EnkelvoudigInformatieObjectDeletion enkelvoudigInformatieObjectDeletion, CancellationToken cancellationToken = default);
}

public class EnkelvoudigInformatieObjectDeletionService : IEnkelvoudigInformatieObjectDeletionService
{
    private const string EnkelvoudigInformatieObjectDeletionsTableName = "__EnkelvoudigInformatieObjectDeletions";

    private readonly ConcurrentDictionary<string, bool> _cache = new(); // Note: The containing provider class should be registered as Singleton!
    private readonly ILogger<EnkelvoudigInformatieObjectDeletionService> _logger;
    private readonly IConfiguration _configuration;

    public EnkelvoudigInformatieObjectDeletionService(ILogger<EnkelvoudigInformatieObjectDeletionService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<IEnumerable<DistinctEnkelvoudigInformatieObjectDeletion>> GetDistinctAsync(
        TimeSpan duration,
        int limit,
        string enabledForRsins,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("Getting from {TableName}", EnkelvoudigInformatieObjectDeletionsTableName);

        if (string.IsNullOrWhiteSpace(enabledForRsins))
        {
            return [];
        }

        // Convert the string like "000001375;000000000;813264571" into string "'000001375','000000000','813264571'"  (which can be used in SQL IN (...) statement)
        var enabledForRsinsSplitted = enabledForRsins.Trim(';').Split(';');

        enabledForRsins = string.Join(',', enabledForRsinsSplitted.Select(r => $"'{r}'"));

        EnsureTableExist();

        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        // TODO: Add index on Rsin (combined with status)

        var query =
            @$"
SELECT DISTINCT rsin, enkelvoudiginformatieobject_url EnkelvoudigInformatieObjectUrl
FROM ""{EnkelvoudigInformatieObjectDeletionsTableName}""
WHERE status = 'pending' 
AND rsin IN ({enabledForRsins})
AND (now()-creationtime) > @Duration
LIMIT @Limit
";

        return await connection.QueryAsync<DistinctEnkelvoudigInformatieObjectDeletion>(query, new { Limit = limit, Duration = duration });
    }

    public async Task DeleteAsync(IEnumerable<string> enkelvoudiginformatieobjectUrls, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting from {TableName}", EnkelvoudigInformatieObjectDeletionsTableName);

        EnsureTableExist();

        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        var urlsToDelete = enkelvoudiginformatieobjectUrls.Select(d => $"'{d}'").ToList();
        if (urlsToDelete.Count != 0)
        {
            var urlsJoined = string.Join(',', urlsToDelete);

            var sql =
                @$"
DELETE FROM ""{EnkelvoudigInformatieObjectDeletionsTableName}"" 
WHERE status != 'error'
AND enkelvoudiginformatieobject_url IN ({urlsJoined})
";

            await connection.ExecuteScalarAsync(sql);
        }
    }

    public async Task DeleteReferencedObjectsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting (referenced objects) from {TableName}", EnkelvoudigInformatieObjectDeletionsTableName);

        EnsureTableExist();

        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        var sql =
            @$"
DELETE FROM ""{EnkelvoudigInformatieObjectDeletionsTableName}""
WHERE id IN (
   SELECT eid.id
   FROM ""{EnkelvoudigInformatieObjectDeletionsTableName}"" eid
   LEFT JOIN objectinformatieobjecten oio ON CAST(RIGHT(eid.enkelvoudiginformatieobject_url,36) AS uuid) = oio.informatieobject_id
   WHERE oio.informatieobject_id IS NOT NULL
);";

        await connection.ExecuteScalarAsync(sql);
    }

    public async Task MarkAsErrorsAsync(Dictionary<string, ErrorResponse> enkelvoudiginformatieobjectUrls, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting from {TableName}", EnkelvoudigInformatieObjectDeletionsTableName);

        EnsureTableExist();

        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        foreach (var url in enkelvoudiginformatieobjectUrls.Keys)
        {
            var sql =
                @$"
UPDATE ""{EnkelvoudigInformatieObjectDeletionsTableName}"" 
SET status = 'error', details = @Details
WHERE enkelvoudiginformatieobject_url = '{url}'
";

            string details = JsonConvert.SerializeObject(enkelvoudiginformatieobjectUrls[url]);

            await connection.ExecuteScalarAsync(sql, new { Details = details });
        }
    }

    public async Task QueueAsync(
        EnkelvoudigInformatieObjectDeletion enkelvoudigInformatieObjectDeletion,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug(
            "Inserting record in {TableName} with values: {enkelvoudiginformatieobjectUrl} {objectUrl}",
            EnkelvoudigInformatieObjectDeletionsTableName,
            enkelvoudigInformatieObjectDeletion.EnkelvoudigInformatieObjectUrl,
            enkelvoudigInformatieObjectDeletion.ObjectUrl
        );

        EnsureTableExist();

        await using var connection = new NpgsqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        using var command = new NpgsqlCommand(
            @$"
INSERT INTO ""{EnkelvoudigInformatieObjectDeletionsTableName}"" (id, creationtime, status, rsin, enkelvoudiginformatieobject_url, object_url) 
VALUES (@Id, NOW(), 'pending', @Rsin, @EnkelvoudiginformatieobjectUrl, @ObjectUrl)",
            connection
        );

        command.Parameters.AddWithValue("Id", NpgsqlDbType.Uuid, Guid.NewGuid());
        command.Parameters.AddWithValue("Rsin", NpgsqlDbType.Varchar, enkelvoudigInformatieObjectDeletion.Rsin);
        command.Parameters.AddWithValue(
            "EnkelvoudiginformatieobjectUrl",
            NpgsqlDbType.Varchar,
            enkelvoudigInformatieObjectDeletion.EnkelvoudigInformatieObjectUrl
        );
        command.Parameters.AddWithValue("ObjectUrl", NpgsqlDbType.Varchar, enkelvoudigInformatieObjectDeletion.ObjectUrl);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private void EnsureTableExist()
    {
        _cache.GetOrAdd(
            EnkelvoudigInformatieObjectDeletionsTableName,
            _ =>
            {
                // So this will be called once per table during lifetime of running process
                using var connection = new NpgsqlConnection(GetConnectionString());
                connection.Open();

                var createTableSql =
                    @$"
CREATE TABLE IF NOT EXISTS ""{EnkelvoudigInformatieObjectDeletionsTableName}"" (
    id uuid NOT NULL,
    creationtime timestamp with time zone NOT NULL,
    status VARCHAR(10) NOT NULL,
    details TEXT NULL,
    rsin VARCHAR(9) NOT NULL,
    enkelvoudiginformatieobject_url VARCHAR(500) NOT NULL,
    object_url VARCHAR(500) NOT NULL,
    CONSTRAINT ""PK_{EnkelvoudigInformatieObjectDeletionsTableName}"" PRIMARY KEY (id)
);
ALTER TABLE ""{EnkelvoudigInformatieObjectDeletionsTableName}"" SET UNLOGGED;
CREATE INDEX IF NOT EXISTS index_enkelvoudiginformatieobject_url ON ""{EnkelvoudigInformatieObjectDeletionsTableName}"" (enkelvoudiginformatieobject_url);
CREATE INDEX IF NOT EXISTS index_status ON ""{EnkelvoudigInformatieObjectDeletionsTableName}"" (status);
CREATE INDEX IF NOT EXISTS index_rsin ON ""{EnkelvoudigInformatieObjectDeletionsTableName}"" (rsin);";

                connection.Execute(createTableSql);

                return true;
            }
        );
    }

    private string GetConnectionString()
    {
        var connectionStringName = "AdminConnectionString";
        var connectionString = _configuration.GetConnectionString(connectionStringName);

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException($"Connection string {connectionStringName} is not available in configuration.");
        }

        return connectionString;
    }
}

public class EnkelvoudigInformatieObjectDeletion
{
    public DateTime Creationtime { get; set; }
    public string Rsin { get; set; }
    public string EnkelvoudigInformatieObjectUrl { get; set; }
    public string ObjectUrl { get; set; }
}

public class DistinctEnkelvoudigInformatieObjectDeletion
{
    public string Rsin { get; set; }
    public string EnkelvoudigInformatieObjectUrl { get; set; }
}
