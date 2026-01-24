using Microsoft.EntityFrameworkCore;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.DataAccess.AuditTrail;
using OneGround.ZGW.DataAccess.Migrations;
using OneGround.ZGW.DataAccess.NumberGenerator;
using OneGround.ZGW.Documenten.DataModel.Authorization;

namespace OneGround.ZGW.Documenten.DataModel;

public class DrcDbContext2 : BaseDbContext, IDbContextWithAuditTrail, IDataMigrationsDbContext, IDbContextWithNummerGenerator
{
    public DrcDbContext2(DbContextOptions<DrcDbContext2> options, IDbUserContext dbUserContext = null)
        : base(options, dbUserContext) { }

    public DbSet<FinishedDataMigration> FinishedDataMigrations { get; set; }

    public DbSet<EnkelvoudigInformatieObject2> EnkelvoudigInformatieObjecten { get; set; }
    public DbSet<EnkelvoudigInformatieObjectLock2> EnkelvoudigInformatieObjectLocks { get; set; }
    public DbSet<AuditTrailRegel> AuditTrailRegels { get; set; }
    public DbSet<BestandsDeel> BestandsDelen { get; set; }
    public DbSet<ObjectInformatieObject> ObjectInformatieObjecten { get; set; }
    public DbSet<GebruiksRecht> GebruiksRechten { get; set; }
    public DbSet<Verzending> Verzendingen { get; set; }
    public DbSet<OrganisatieNummer> OrganisatieNummers { get; set; }
    public DbSet<TempInformatieObjectAuthorization> TempInformatieObjectAuthorization { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder
            .Entity<TempInformatieObjectAuthorization>()
            .ToTable(nameof(TempInformatieObjectAuthorization), t => t.ExcludeFromMigrations())
            .ToView(nameof(TempInformatieObjectAuthorization))
            .HasKey(t => t.InformatieObjectType);

        modelBuilder.Entity<EnkelvoudigInformatieObject2>().HasIndex(e => e.InformatieObjectType);

        modelBuilder.Entity<EnkelvoudigInformatieObject2>().HasIndex(e => e.Owner);

        modelBuilder.Entity<EnkelvoudigInformatieObject2>().HasIndex(e => e.Bronorganisatie);

        modelBuilder.Entity<EnkelvoudigInformatieObject2>().HasIndex(e => e.Identificatie);

        modelBuilder.Entity<EnkelvoudigInformatieObject2>().HasIndex(e => e.Inhoud); // Note: In Inhoud the DMS urn's are stored to the underlying documentservice providers (Ceph, MongoDB, etc). To count the organisation total storage size efficient an index is added therefore

        modelBuilder
            .Entity<EnkelvoudigInformatieObject2>()
            .HasIndex(e => new
            {
                e.Owner,
                e.Inhoud,
                e.Vertrouwelijkheidaanduiding,
            })
            .IsDescending(false, false, true);

        modelBuilder
            .Entity<EnkelvoudigInformatieObject2>()
            .HasIndex(e => new
            {
                e.Owner,
                e.Inhoud,
                e.Vertrouwelijkheidaanduiding,
                e.EnkelvoudigInformatieObjectId,
            })
            .IsDescending(false, false, true, false)
            .IncludeProperties(e => e.Bestandsomvang)
            .HasFilter($"{nameof(EnkelvoudigInformatieObject2.Bestandsomvang)} IS NOT NULL");

        modelBuilder.Entity<EnkelvoudigInformatieObject2>().HasIndex(b => b.EnkelvoudigInformatieObjectId);

        // Note: We sould have versie DESC in the index but it is not possible in EF right now
        modelBuilder
            .Entity<EnkelvoudigInformatieObject2>()
            .HasIndex(b => new
            {
                b.Owner,
                b.EnkelvoudigInformatieObjectId,
                b.Versie,
                b.Vertrouwelijkheidaanduiding,
            })
            .IsDescending(false, false, true, false);

        modelBuilder.Entity<ObjectInformatieObject>().HasIndex(e => e.InformatieObjectId);

        modelBuilder.Entity<ObjectInformatieObject>().HasIndex(e => e.Object);

        modelBuilder.Entity<ObjectInformatieObject>().HasIndex("Object", "InformatieObjectId", "ObjectType").IsUnique();

        modelBuilder.Entity<AuditTrailRegel>().HasIndex(p => p.HoofdObjectId);

        modelBuilder.Entity<Verzending>().HasIndex(e => e.AardRelatie);

        modelBuilder.Entity<Verzending>().HasIndex(e => e.Betrokkene);

        //modelBuilder
        //    .Entity<EnkelvoudigInformatieObject>()
        //    .HasIndex(e => new
        //    {
        //        e.Owner,
        //        e.InformatieObjectType,
        //        e.LatestEnkelvoudigInformatieObjectVersieId,
        //    });

        modelBuilder
            .Entity<EnkelvoudigInformatieObject2>()
            .HasIndex(e => new
            {
                e.Owner,
                e.Id,
                e.Vertrouwelijkheidaanduiding,
            });

        modelBuilder
            .Entity<EnkelvoudigInformatieObject2>()
            .HasIndex(e => e.Id)
            .HasDatabaseName("idx_e0_light_covering")
            .IncludeProperties(e => new
            {
                e.Owner,
                e.Vertrouwelijkheidaanduiding,
                e.EnkelvoudigInformatieObjectId,
            });

        modelBuilder
            .Entity<EnkelvoudigInformatieObject2>()
            .HasIndex(e => new
            {
                e.Vertrouwelijkheidaanduiding,
                e.Id,
                e.Owner,
            });

        // Define 1:N relation between EnkelvoudigInformatieObjectLock and EnkelvoudigInformatieObjecten
        modelBuilder
            .Entity<EnkelvoudigInformatieObject2>()
            .HasOne(e => e.EnkelvoudigInformatieObjectLock)
            .WithMany(e => e.EnkelvoudigInformatieObjecten)
            .HasForeignKey(e => e.EnkelvoudigInformatieObjectLockId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Define 1:1 relation between LatestEnkelvoudigInformatieObjectVersie and EnkelvoudigInformatieObject
        //modelBuilder
        //    .Entity<EnkelvoudigInformatieObjectVersie>()
        //    .HasOne(e => e.LatestInformatieObject)
        //    .WithOne(e => e.LatestEnkelvoudigInformatieObjectVersie)
        //    .HasForeignKey<EnkelvoudigInformatieObject>(e => e.LatestEnkelvoudigInformatieObjectVersieId);

        // Source: https://www.npgsql.org/efcore/misc/collations-and-case-sensitivity.html?tabs=data-annotations
        // And:    https://dba.stackexchange.com/questions/255780/case-insensitive-collation-still-comparing-case-sensitive
        // And:    https://stackoverflow.com/questions/70739480/change-postgres-to-case-insensitive
        modelBuilder.HasCollation("ci_collation", locale: "@colStrength=secondary", provider: "icu", deterministic: false);

        modelBuilder.Entity<ObjectInformatieObject>().Property(c => c.Object).UseCollation("ci_collation");

        modelBuilder.Entity<Verzending>().Property(c => c.Betrokkene).UseCollation("ci_collation");
    }
}
