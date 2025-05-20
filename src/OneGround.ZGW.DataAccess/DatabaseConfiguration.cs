namespace OneGround.ZGW.DataAccess;

public class DatabaseConfiguration
{
    /// <summary>
    /// Whether connection pooling should be used. Default: True
    /// </summary>
    public bool EnablePooling { get; set; } = true;

    /// <summary>
    /// The minimum connection pool size. Default: 0
    /// </summary>
    public int MinPoolSize { get; set; } = 0;

    /// <summary>
    /// The maximum connection pool size. Default: 100
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// The time to wait before closing unused connections in the pool if the count of all connections exceeds MinPoolSize. Default: 300
    /// </summary>
    public int ConnectionIdleLifetime { get; set; } = 60;

    /// <summary>
    /// How many seconds the pool waits before attempting to prune idle connections that are beyond idle lifetime. Default: 10
    /// </summary>
    public int ConnectionPruningInterval { get; set; } = 10;

    /// <summary>
    /// When specified, overrides the port of the connection string. Default null
    /// </summary>
    public int? PgBouncerPort { get; set; } = null;
}
