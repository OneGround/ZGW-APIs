using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Roxit.ZGW.DataAccess.AuditTrail;
using Roxit.ZGW.Documenten.DataModel;

namespace Roxit.ZGW.Documenten.WebApi.UnitTests.BusinessRulesTests.v1;

class UnitTestDrcDbContext : DrcDbContext
{
    public UnitTestDrcDbContext(DbContextOptions<DrcDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EnkelvoudigInformatieObject>().HasIndex(e => e.InformatieObjectType);

        modelBuilder.Entity<EnkelvoudigInformatieObjectVersie>().HasIndex(e => e.Bronorganisatie);

        modelBuilder.Entity<EnkelvoudigInformatieObjectVersie>().HasIndex(e => e.Identificatie);

        modelBuilder
            .Entity<EnkelvoudigInformatieObjectVersie>()
            .HasIndex(e => new
            {
                e.Bronorganisatie,
                e.Identificatie,
                e.Versie,
            })
            .IsUnique(true);

        modelBuilder.Entity<EnkelvoudigInformatieObjectVersie>().HasIndex(e => e.Inhoud); // Note: In Inhoud the DMS urn's are stored to the underlying documentservice providers (Ceph, MongoDB, etc). To count the organisation total storage size efficient an index is added therefore

        modelBuilder.Entity<ObjectInformatieObject>().HasIndex(e => e.InformatieObjectId);

        modelBuilder.Entity<ObjectInformatieObject>().HasIndex(e => e.Object);

        modelBuilder.Entity<AuditTrailRegel>().HasIndex(p => p.HoofdObjectId);

        modelBuilder.Entity<Verzending>().HasIndex(e => e.AardRelatie);

        modelBuilder.Entity<EnkelvoudigInformatieObjectVersie>().Property(a => a.Trefwoorden);

        modelBuilder.Entity<Verzending>().HasIndex(e => e.Betrokkene);

        modelBuilder.Ignore<BinnenlandsCorrespondentieAdres>();
        modelBuilder.Ignore<BuitenlandsCorrespondentieAdres>();
        modelBuilder.Ignore<CorrespondentiePostadres>();

        modelBuilder.Entity<EnkelvoudigInformatieObjectVersie>().Ignore(e => e.Trefwoorden);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }
}
