using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace OneGround.ZGW.DataAccess;

public interface ITemporaryTableProvider
{
    Task CreateAsync(BaseDbContext context, string sql, CancellationToken cancellationToken = default);
}

public class TemporaryTableProvider : ITemporaryTableProvider
{
    public async Task CreateAsync(BaseDbContext context, string sql, CancellationToken cancellationToken = default)
    {
        var connection = context.Database.GetDbConnection();
        bool connectionWasClosed = connection.State == System.Data.ConnectionState.Closed;

        await context.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch (Exception)
        {
            if (connectionWasClosed)
            {
                await context.Database.CloseConnectionAsync();
            }
            throw;
        }
    }
}
