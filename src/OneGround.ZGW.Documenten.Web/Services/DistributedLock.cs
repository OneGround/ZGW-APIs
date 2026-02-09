using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace OneGround.ZGW.Documenten.Services;

/// <summary>
/// Represents a distributed lock that can be acquired and released.
/// </summary>
public interface IDistributedLock : IAsyncDisposable
{
    /// <summary>
    /// Attempts to acquire the lock within the given timeout period.
    /// </summary>
    /// <param name="timeout">Maximum time to wait for acquiring the lock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the lock was acquired, false otherwise.</returns>
    Task<bool> AcquireAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
}

/// <summary>
/// Factory for creating distributed locks.
/// </summary>
public interface IDistributedLockFactory
{
    /// <summary>
    /// Creates a distributed lock using a GUID as the lock key.
    /// </summary>
    IDistributedLock Create(Guid lockGuid);

    /// <summary>
    /// Creates a distributed lock using a long integer as the lock key.
    /// </summary>
    IDistributedLock Create(long lockKey);
}

/// <summary>
/// PostgreSQL advisory lock-based distributed lock implementation.
/// </summary>
public sealed class DistributedLock : IDistributedLock
{
    private readonly string _connectionString;
    private readonly long _lockKey;
    private readonly TimeSpan _retryDelay;
    private NpgsqlConnection _connection;
    private bool _lockAcquired;

    public DistributedLock(string connectionString, long lockKey, TimeSpan? retryDelay = null)
    {
        _connectionString = connectionString;
        _lockKey = lockKey;
        _retryDelay = retryDelay ?? TimeSpan.FromMilliseconds(250);
    }

    public DistributedLock(string connectionString, Guid lockGuid, TimeSpan? retryDelay = null)
    {
        _connectionString = connectionString;
        _retryDelay = retryDelay ?? TimeSpan.FromMilliseconds(250);

        // Convert GUID to a long for use as a lock key (collision possible, but acceptable for many scenarios)
        _lockKey = ToAdvisoryLockKey(lockGuid);
    }

    private static long ToAdvisoryLockKey(Guid guid)
    {
        Span<byte> bytes = stackalloc byte[16];
        guid.TryWriteBytes(bytes);

        // XOR-fold to 64 bits
        return BitConverter.ToInt64(bytes[..8]) ^ BitConverter.ToInt64(bytes[8..]);
    }

    /// <summary>
    /// Attempts to acquire the lock within the given timeout.
    /// </summary>
    public async Task<bool> AcquireAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        _connection = new NpgsqlConnection(_connectionString);
        await _connection.OpenAsync(cancellationToken);

        while (sw.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var cmd = new NpgsqlCommand("SELECT pg_try_advisory_lock(@key)", _connection);

            cmd.Parameters.AddWithValue("key", _lockKey);

            var acquired = (bool)await cmd.ExecuteScalarAsync(cancellationToken);
            if (acquired)
            {
                _lockAcquired = true;
                return true;
            }

            await Task.Delay(_retryDelay, cancellationToken);
        }

        return false;
    }

    public async ValueTask DisposeAsync()
    {
        if (_lockAcquired && _connection != null)
        {
            try
            {
                await using var cmd = new NpgsqlCommand("SELECT pg_advisory_unlock(@key)", _connection);
                cmd.Parameters.AddWithValue("key", _lockKey);
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Best effort cleanup – lock is automatically released on crash
            }
        }
        if (_connection != null)
            await _connection.DisposeAsync();
    }
}

/// <summary>
/// Disabled lock implementation that always succeeds (for when distributed locking is turned off).
/// </summary>
public class DisabledDistributedLock : IDistributedLock
{
    public Task<bool> AcquireAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Factory for creating distributed locks, with configuration support.
/// </summary>
public class DistributedLockFactory : IDistributedLockFactory
{
    private readonly string _connectionString;
    private readonly TimeSpan _retryDelay;
    private readonly bool _useLocking;

    public DistributedLockFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("UserConnectionString");
        _retryDelay = configuration.GetValue<TimeSpan?>("DistributedLock:AcquireLockRetryDelay") ?? TimeSpan.FromMilliseconds(250);

        _useLocking = configuration.GetValue<bool?>("DistributedLock:UseDistributedLocking") ?? true;
    }

    public IDistributedLock Create(Guid lockGuid) =>
        _useLocking ? new DistributedLock(_connectionString, lockGuid, _retryDelay) : new DisabledDistributedLock();

    public IDistributedLock Create(long lockKey) =>
        _useLocking ? new DistributedLock(_connectionString, lockKey, _retryDelay) : new DisabledDistributedLock();
}
