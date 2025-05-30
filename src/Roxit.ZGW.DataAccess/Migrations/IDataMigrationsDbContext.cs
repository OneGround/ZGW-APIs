using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Roxit.ZGW.DataAccess.Migrations;

public interface IDataMigrationsDbContext : IDisposable
{
    DbSet<FinishedDataMigration> FinishedDataMigrations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
