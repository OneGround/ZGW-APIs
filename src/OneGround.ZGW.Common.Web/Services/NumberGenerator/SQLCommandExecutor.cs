using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace OneGround.ZGW.Common.Web.Services.NumberGenerator;

public class SqlCommandExecutor<TContext> : ISqlCommandExecutor
    where TContext : DbContext
{
    private readonly TContext _context;

    public SqlCommandExecutor(TContext context)
    {
        _context = context;
    }

    public async Task ExecuteSqlRawAsync(string sql, CancellationToken cancellationToken)
    {
        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}

public interface ISqlCommandExecutor
{
    Task ExecuteSqlRawAsync(string sql, CancellationToken cancellationToken);
}
