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
        await context.Database.GetDbConnection().OpenAsync(cancellationToken);

        try
        {
            await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch (Exception)
        {
            await context.Database.GetDbConnection().CloseAsync();
            throw;
        }
    }
}
