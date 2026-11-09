using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.DataAccess.Migrations;
using OneGround.ZGW.DataAccess.NumberGenerator;

namespace OneGround.ZGW.Gesprekken.DataModel;

public class GrcDbContext : BaseDbContext, IDataMigrationsDbContext, IDbContextWithNummerGenerator
{
    public GrcDbContext(DbContextOptions<GrcDbContext> options, IDbUserContext dbUserContext = null)
        : base(options, dbUserContext) { }

    public DbSet<FinishedDataMigration> FinishedDataMigrations { get; set; }
    public DbSet<Gesprek> Gesprekken { get; set; }
    public DbSet<Gespreksdeelname> Gespreksdeelnames { get; set; }
    public DbSet<Gespreksbijdrage> Gespreksbijdragen { get; set; }
    public DbSet<Bijlage> Bijlagen { get; set; }
    public DbSet<Bestand> Bestanden { get; set; }
    public DbSet<GelezenGespreksbijdrage> GelezenGespreksbijdragen { get; set; }
    public DbSet<OrganisatieNummer> OrganisatieNummers { get; set; }
    public DbSet<GesprekEventRecord> Events { get; set; }

    // Any constant identifies the lock; it only has to be the same for every writer of the event log.
    private const string LockEventLogSql = "SELECT pg_advisory_xact_lock(7301843251)";

    /// <summary>
    /// Saves the changes together with the events they appended. Appends are serialized with a transaction-scoped
    /// advisory lock, so events become visible in sequence order: a reader that has seen sequence N never later
    /// finds a committed event below N, and a cursor can safely be "the last sequence seen".
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var appendsEvents = ChangeTracker.Entries<GesprekEventRecord>().Any(e => e.State == EntityState.Added);
        if (!appendsEvents || !Database.IsRelational() || Database.CurrentTransaction != null)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
        await Database.ExecuteSqlRawAsync(LockEventLogSql, cancellationToken);
        var result = await base.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<Gesprek>().HasIndex(e => new { e.Owner, e.Referentie }).IsUnique();
        modelBuilder.Entity<Gesprek>().HasIndex(e => e.AanleidingZaak);
        modelBuilder.Entity<Gesprek>().HasIndex(e => e.AanvangsmomentGesprek);
        modelBuilder.Entity<Gesprek>().HasIndex(e => e.AanleidingPlan);

        modelBuilder.Entity<Gespreksdeelname>().HasOne(e => e.Gesprek).WithMany(e => e.Deelnames).HasForeignKey(e => e.GesprekId);
        modelBuilder.Entity<Gespreksdeelname>().HasIndex(e => e.Partij);
        modelBuilder.Entity<Gespreksdeelname>().HasIndex(e => e.Actor);

        modelBuilder.Entity<Gespreksbijdrage>().HasOne(e => e.Gesprek).WithMany(e => e.Bijdragen).HasForeignKey(e => e.GesprekId);
        modelBuilder
            .Entity<Gespreksbijdrage>()
            .HasOne(e => e.Verzender)
            .WithMany()
            .HasForeignKey(e => e.VerzenderId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Gespreksbijdrage>().HasMany(e => e.Bijlagen).WithOne().HasForeignKey(e => e.GespreksbijdrageId);
        modelBuilder.Entity<Gespreksbijdrage>().HasIndex(e => new { e.GesprekId, e.Leveringsmoment });

        modelBuilder.Entity<Bijlage>().HasIndex(e => e.BestandId).IsUnique();

        modelBuilder.Entity<GelezenGespreksbijdrage>().HasIndex(e => new { e.GespreksbijdrageId, e.GespreksdeelnameId }).IsUnique();

        modelBuilder.Entity<GesprekEventRecord>().HasIndex(e => new { e.Owner, e.Sequence });
        modelBuilder.Entity<GesprekEventRecord>().HasIndex(e => e.EventId).IsUnique();
        modelBuilder.Entity<GesprekEventRecord>().HasIndex(e => e.AanleidingZaak);

        // Case-insensitive comparison of URL references, as in the other APIs.
        modelBuilder.HasCollation("ci_collation", locale: "@colStrength=secondary", provider: "icu", deterministic: false);
        modelBuilder.Entity<Gesprek>().Property(c => c.AanleidingZaak).UseCollation("ci_collation");
        modelBuilder.Entity<Gespreksdeelname>().Property(c => c.Actor).UseCollation("ci_collation");
        modelBuilder.Entity<Gespreksdeelname>().Property(c => c.Partij).UseCollation("ci_collation");
    }
}
