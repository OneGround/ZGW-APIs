using Microsoft.EntityFrameworkCore;
using Roxit.ZGW.Besluiten.DataModel.Authorization;
using Roxit.ZGW.DataAccess;
using Roxit.ZGW.DataAccess.AuditTrail;
using Roxit.ZGW.DataAccess.Migrations;
using Roxit.ZGW.DataAccess.NumberGenerator;

namespace Roxit.ZGW.Besluiten.DataModel;

public class BrcDbContext : BaseDbContext, IDbContextWithAuditTrail, IDataMigrationsDbContext, IDbContextWithNummerGenerator
{
    public BrcDbContext(DbContextOptions<BrcDbContext> options, IDbUserContext dbUserContext = null)
        : base(options, dbUserContext) { }

    public DbSet<FinishedDataMigration> FinishedDataMigrations { get; set; }
    public DbSet<Besluit> Besluiten { get; set; }
    public DbSet<BesluitInformatieObject> BesluitInformatieObjecten { get; set; }
    public DbSet<AuditTrailRegel> AuditTrailRegels { get; set; }
    public DbSet<OrganisatieNummer> OrganisatieNummers { get; set; }
    public DbSet<TempBesluitAuthorization> TempBesluitAuthorization { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder
            .Entity<TempBesluitAuthorization>()
            .ToTable(nameof(Authorization.TempBesluitAuthorization), t => t.ExcludeFromMigrations())
            .ToView(nameof(Authorization.TempBesluitAuthorization))
            .HasKey(x => x.BesluitType);

        modelBuilder.Entity<Besluit>().HasIndex(p => new { p.VerantwoordelijkeOrganisatie, p.Identificatie }).IsUnique();

        modelBuilder.Entity<Besluit>().HasIndex(p => p.VerantwoordelijkeOrganisatie);

        modelBuilder.Entity<Besluit>().HasIndex(p => p.Identificatie);

        modelBuilder.Entity<Besluit>().HasIndex(p => p.BesluitType);

        modelBuilder.Entity<Besluit>().HasIndex(p => p.Zaak);

        modelBuilder.Entity<Besluit>().HasIndex(p => new { p.Owner, p.Identificatie });

        modelBuilder.Entity<BesluitInformatieObject>().HasIndex(p => p.BesluitId);

        modelBuilder.Entity<BesluitInformatieObject>().HasIndex(p => p.InformatieObject);

        modelBuilder.Entity<AuditTrailRegel>().HasIndex(p => p.HoofdObjectId);

        // Source: https://www.npgsql.org/efcore/misc/collations-and-case-sensitivity.html?tabs=data-annotations
        // And:    https://dba.stackexchange.com/questions/255780/case-insensitive-collation-still-comparing-case-sensitive
        // And:    https://stackoverflow.com/questions/70739480/change-postgres-to-case-insensitive
        modelBuilder.HasCollation("ci_collation", locale: "@colStrength=secondary", provider: "icu", deterministic: false);

        modelBuilder.Entity<Besluit>().Property(c => c.BesluitType).UseCollation("ci_collation");

        modelBuilder.Entity<Besluit>().Property(c => c.Zaak).UseCollation("ci_collation");

        modelBuilder.Entity<BesluitInformatieObject>().Property(c => c.InformatieObject).UseCollation("ci_collation");
    }
}
