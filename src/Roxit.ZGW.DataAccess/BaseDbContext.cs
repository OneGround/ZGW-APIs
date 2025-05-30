using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Roxit.ZGW.DataAccess;

public class BaseDbContext : DbContext
{
    private readonly IDbUserContext _dbUserContext;

    public BaseDbContext(DbContextOptions options, IDbUserContext dbUserContext)
        : base(options)
    {
        _dbUserContext = dbUserContext;
    }

    public override int SaveChanges()
    {
        throw new NotSupportedException("Use SaveChangesAsync instead");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (!DoNotWriteSystemValues)
        {
            EnsureCreationAndModifiedSystemValuesSet();
        }
        return base.SaveChangesAsync(cancellationToken);
    }

    public bool DoNotWriteSystemValues { get; set; }

    private void EnsureCreationAndModifiedSystemValuesSet()
    {
        // Note: Be sure the CreationTime, CreatedBy, ModificationTime and ModifiedBy is set
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is IAuditableEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        var now = DateTime.UtcNow;

        foreach (var entityEntry in entries)
        {
            if (entityEntry.State == EntityState.Added)
            {
                ((IAuditableEntity)entityEntry.Entity).CreationTime = now;
                if (_dbUserContext != null)
                    ((IAuditableEntity)entityEntry.Entity).CreatedBy = _dbUserContext.UserId;
            }

            ((IAuditableEntity)entityEntry.Entity).ModificationTime = now;
            if (_dbUserContext != null)
                ((IAuditableEntity)entityEntry.Entity).ModifiedBy = _dbUserContext.UserId;
        }
    }
}
