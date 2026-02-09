using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace OneGround.ZGW.DataAccess;

public static class EfPostgresLockExtensions
{
    /// <summary>
    /// Applies PostgreSQL row-level locking (FOR UPDATE [SKIP LOCKED]) to prevent concurrent modifications.
    /// This locks the rows matching the specified IDs for the duration of the transaction.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being queried</typeparam>
    /// <typeparam name="TKey">The type of the primary key</typeparam>
    /// <param name="set">The DbSet to query</param>
    /// <param name="context">The database context</param>
    /// <param name="keySelector">Expression to select the primary key property</param>
    /// <param name="ids">The IDs of rows to lock</param>
    /// <param name="skipLocked">If true, skips locked rows instead of waiting</param>
    /// <returns>A query with row-level locking applied</returns>
    public static IQueryable<TEntity> LockForUpdate<TEntity, TKey>(
        this DbSet<TEntity> set,
        DbContext context,
        Expression<Func<TEntity, TKey>> keySelector,
        IEnumerable<TKey> ids,
        bool skipLocked = true
    )
        where TEntity : class
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity)) ?? throw new InvalidOperationException("Entity not found");

        var tableName = entityType.GetTableName();
        var schema = entityType.GetSchema() ?? "public";
        var keyColumn = entityType.FindProperty(((MemberExpression)keySelector.Body).Member.Name)!.GetColumnName();

        var skip = skipLocked ? " SKIP LOCKED" : "";

        // Include xmin for concurrency token support
        // Use PostgreSQL's ANY array syntax for efficient parameter passing
        var sql =
            $@"
            SELECT *, xmin
            FROM ""{schema}"".""{tableName}""
            WHERE ""{keyColumn}"" = ANY(@ids)
            ORDER BY ""{keyColumn}""
            FOR UPDATE{skip}
        ";
        return set.FromSqlRaw(sql, new NpgsqlParameter("ids", ids.ToArray()));
    }
}
