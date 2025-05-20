using Microsoft.EntityFrameworkCore;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.DataAccess.Migrations;

namespace OneGround.ZGW.Notificaties.DataModel;

public partial class NrcDbContext : BaseDbContext, IDataMigrationsDbContext
{
    public NrcDbContext(DbContextOptions<NrcDbContext> options, IDbUserContext dbUserContext = null)
        : base(options, dbUserContext) { }

    public DbSet<FinishedDataMigration> FinishedDataMigrations { get; set; }

    public virtual DbSet<Abonnement> Abonnementen { get; set; }
    public virtual DbSet<AbonnementKanaal> AbonnementKanalen { get; set; }
    public virtual DbSet<Kanaal> Kanalen { get; set; }
    public virtual DbSet<FilterValue> FilterValues { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AbonnementKanaal>().HasIndex(e => e.AbonnementId);

        modelBuilder.Entity<AbonnementKanaal>().HasIndex(e => e.KanaalId);

        modelBuilder.Entity<Kanaal>().HasIndex(e => e.Naam).IsUnique(true);

        // in-memory database context does not support array of primitive types,
        // so we ignore these right here
        if (Database.IsInMemory())
        {
            IgnorePrimitiveArrayTypes(modelBuilder);
        }
    }

    private static void IgnorePrimitiveArrayTypes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Kanaal>().Ignore(s => s.Filters);
    }
}
