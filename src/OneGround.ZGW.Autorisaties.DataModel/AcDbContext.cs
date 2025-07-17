using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.DataAccess.Migrations;

namespace OneGround.ZGW.Autorisaties.DataModel;

public class AcDbContext : BaseDbContext, IDataMigrationsDbContext
{
    public AcDbContext(DbContextOptions<AcDbContext> options, IDbUserContext dbUserContext = null)
        : base(options, dbUserContext) { }

    public DbSet<FinishedDataMigration> FinishedDataMigrations { get; set; }

    public DbSet<Applicatie> Applicaties { get; set; }
    public DbSet<ApplicatieClient> ApplicatieClients { get; set; }
    public DbSet<Autorisatie> Autorisaties { get; set; }
    public DbSet<FutureAutorisatie> FutureAutorisaties { get; set; }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SortScopes();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SortScopes()
    {
        foreach (var entry in ChangeTracker.Entries<Autorisatie>())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                var sortedScopes = entry.Entity.Scopes?.OrderBy(s => s, StringComparer.Ordinal).ToArray();
                if (!entry.Entity.Scopes.SequenceEqual(sortedScopes, StringComparer.Ordinal))
                {
                    entry.Entity.Scopes = sortedScopes;
                }
            }
        }
        foreach (var entry in ChangeTracker.Entries<FutureAutorisatie>())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                var sortedScopes = entry.Entity.Scopes?.OrderBy(s => s, StringComparer.Ordinal).ToArray();
                if (!entry.Entity.Scopes.SequenceEqual(sortedScopes, StringComparer.Ordinal))
                {
                    entry.Entity.Scopes = sortedScopes;
                }
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<Autorisatie>()
            .HasIndex(e => new
            {
                e.Component,
                e.MaxVertrouwelijkheidaanduiding,
                e.Scopes,
                e.ApplicatieId,
                e.ZaakType,
                e.BesluitType,
                e.InformatieObjectType,
                e.Owner,
            })
            .IsUnique();

        modelBuilder
            .Entity<FutureAutorisatie>()
            .HasIndex(e => new
            {
                e.Component,
                e.MaxVertrouwelijkheidaanduiding,
                e.Scopes,
                e.ApplicatieId,
                e.Owner,
            })
            .IsUnique()
            .IsCreatedConcurrently();
    }
}
