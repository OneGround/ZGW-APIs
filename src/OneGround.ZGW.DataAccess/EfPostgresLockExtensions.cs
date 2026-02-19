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
        // Note: The following check fixes the issue when the method LockForUpdate() is called from a UnitTest:
        //   System.InvalidOperationException : Query root of type 'FromSqlQueryRootExpression' wasn't handled by provider code.
        //   This issue happens when using a provider specific method on a different provider where it is not supported.
        if (context.IsInMemory())
        {
            // Simply return the set without applying any locking, as InMemory provider does not support raw SQL or locking semantics.
            return set;
        }

        var entityType = context.Model.FindEntityType(typeof(TEntity));
        if (entityType == null)
        {
            throw new InvalidOperationException($"Entity type '{typeof(TEntity).Name}' was not found in the database model.");
        }

        var tableName = entityType.GetTableName();
        if (string.IsNullOrEmpty(tableName))
        {
            throw new InvalidOperationException($"Table name for entity type '{typeof(TEntity).Name}' could not be determined.");
        }

        var schema = entityType.GetSchema() ?? "public";

        // Safely extract the property name from the key selector expression
        var propertyName = GetPropertyName(keySelector);

        var property = entityType.FindProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException(
                $"Property '{propertyName}' was not found on entity type '{typeof(TEntity).Name}'. "
                    + $"Ensure the key selector expression references a valid property."
            );
        }

        var keyColumn = property.GetColumnName();
        if (string.IsNullOrEmpty(keyColumn))
        {
            throw new InvalidOperationException(
                $"Column name for property '{propertyName}' on entity type '{typeof(TEntity).Name}' could not be determined."
            );
        }

        var skip = skipLocked ? " SKIP LOCKED" : "";

        // Include xmin for concurrency token support
        // Use PostgreSQL's ANY array syntax for efficient parameter passing
        var sql =
            $@"
            SELECT *, xmin
            FROM ""{schema}"".""{tableName}""
            WHERE ""{keyColumn}"" = ANY(@ids)
            ORDER BY ""{keyColumn}""
            FOR UPDATE{skip}";

        return set.FromSqlRaw(sql, new NpgsqlParameter("ids", ids.ToArray()));
    }

    /// <summary>
    /// Extracts the property name from a lambda expression, handling UnaryExpression (conversions/boxing).
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The property type</typeparam>
    /// <param name="expression">The lambda expression selecting the property</param>
    /// <returns>The property name</returns>
    /// <exception cref="ArgumentException">Thrown when the expression is not a valid property selector</exception>
    private static string GetPropertyName<TEntity, TKey>(Expression<Func<TEntity, TKey>> expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression), "Key selector expression cannot be null.");
        }

        // Unwrap the lambda body
        Expression body = expression.Body;

        // Handle UnaryExpression (e.g., boxing, implicit conversions like int to object)
        if (body is UnaryExpression unaryExpression)
        {
            body = unaryExpression.Operand;
        }

        // The body should now be a MemberExpression
        if (body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        // If we still don't have a MemberExpression, the selector is invalid
        throw new ArgumentException(
            $"Key selector expression must be a simple property accessor (e.g., 'x => x.Id'). "
                + $"Received expression type: {expression.Body.GetType().Name}",
            nameof(expression)
        );
    }

    /// <summary>
    /// Determines if the DbContext is using the InMemory provider, which does not support raw SQL or locking semantics.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <returns>True if running within in-memory context (UnitTest), false oterwise</returns>
    private static bool IsInMemory(this DbContext context)
    {
        return context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
    }
}
