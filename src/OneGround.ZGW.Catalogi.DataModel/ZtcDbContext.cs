using Microsoft.EntityFrameworkCore;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.DataAccess.AuditTrail;
using OneGround.ZGW.DataAccess.Migrations;

namespace OneGround.ZGW.Catalogi.DataModel;

public class ZtcDbContext : BaseDbContext, IDbContextWithAuditTrail, IDataMigrationsDbContext
{
    public ZtcDbContext(DbContextOptions<ZtcDbContext> options, IDbUserContext dbUserContext = null)
        : base(options, dbUserContext) { }

    public DbSet<AuditTrailRegel> AuditTrailRegels { get; set; }
    public DbSet<FinishedDataMigration> FinishedDataMigrations { get; set; }

    public DbSet<ZaakType> ZaakTypen { get; set; }
    public DbSet<StatusType> StatusTypen { get; set; }
    public DbSet<RolType> RolTypen { get; set; }
    public DbSet<Catalogus> Catalogussen { get; set; }
    public DbSet<ResultaatType> ResultaatTypen { get; set; }
    public DbSet<InformatieObjectType> InformatieObjectTypen { get; set; }
    public DbSet<Eigenschap> Eigenschappen { get; set; }
    public DbSet<BesluitType> BesluitTypen { get; set; }
    public DbSet<ReferentieProces> ReferentieProcessen { get; set; }
    public DbSet<StatusTypeVerplichteEigenschap> StatusTypeVerplichteEigenschappen { get; set; }
    public DbSet<ZaakObjectType> ZaakObjectTypen { get; set; }

    public DbSet<ZaakTypeDeelZaakType> ZaakTypeDeelZaakTypen { get; set; }
    public DbSet<ZaakTypeGerelateerdeZaakType> ZaakTypeGerelateerdeZaakTypen { get; set; }
    public DbSet<ZaakTypeBesluitType> ZaakTypeBesluitTypen { get; set; }
    public DbSet<ZaakTypeInformatieObjectType> ZaakTypeInformatieObjectTypen { get; set; }
    public DbSet<BesluitTypeInformatieObjectType> BesluitTypeInformatieObjectTypen { get; set; }
    public DbSet<ResultaatTypeBesluitType> ResultaatTypeBesluitTypen { get; set; }
    public DbSet<BronDatumArchiefProcedure> BronDatumArchiefProcedures { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Catalogus>().HasIndex(c => new { c.Rsin, c.Domein }).IsUnique();

        modelBuilder.Entity<Catalogus>().HasIndex(c => new { c.Owner, c.Domein }).IsUnique();

        ConfigureStatusTypeVerplichteEigenschappenManyToMany(modelBuilder);

        modelBuilder.Entity<InformatieObjectType>().HasIndex(b => b.Owner);

        modelBuilder.Entity<InformatieObjectType>().HasIndex(b => b.Concept);

        modelBuilder.Entity<InformatieObjectType>().HasIndex(b => b.Omschrijving);

        modelBuilder.Entity<InformatieObjectType>().HasIndex(b => b.CreationTime);

        modelBuilder
            .Entity<InformatieObjectType>()
            .HasIndex(b => new
            {
                b.Concept,
                b.Omschrijving,
                b.BeginGeldigheid,
            });

        modelBuilder.Entity<ZaakTypeBesluitType>().HasIndex(b => new { b.ZaakTypeId, b.BesluitTypeOmschrijving }).IsUnique();

        modelBuilder.Entity<ZaakTypeGerelateerdeZaakType>().HasIndex(b => new { b.ZaakTypeId, b.GerelateerdeZaakTypeIdentificatie }).IsUnique();

        modelBuilder.Entity<ZaakTypeDeelZaakType>().HasIndex(b => new { b.ZaakTypeId, b.DeelZaakTypeIdentificatie }).IsUnique();

        modelBuilder.Entity<BesluitTypeInformatieObjectType>().HasIndex(b => new { b.BesluitTypeId, b.InformatieObjectTypeOmschrijving }).IsUnique();

        modelBuilder
            .Entity<ZaakTypeInformatieObjectType>()
            .HasIndex(i => new
            {
                i.ZaakTypeId,
                i.InformatieObjectTypeOmschrijving,
                i.VolgNummer,
                i.Richting,
            })
            .IsUnique();

        modelBuilder.Entity<ZaakObjectType>().HasIndex(z => z.Owner);

        modelBuilder.Entity<ZaakObjectType>().HasIndex(z => z.AnderObjectType);

        modelBuilder.Entity<ZaakObjectType>().HasIndex(z => z.BeginGeldigheid);

        modelBuilder.Entity<ZaakObjectType>().HasIndex(z => z.EindeGeldigheid);

        modelBuilder.Entity<ZaakObjectType>().HasIndex(z => z.ObjectType);

        modelBuilder.Entity<ZaakObjectType>().HasIndex(z => z.RelatieOmschrijving);

        modelBuilder.Entity<ZaakObjectType>().HasIndex(z => z.ZaakTypeId);

        modelBuilder.Entity<ZaakType>().HasIndex(z => z.CreationTime);
    }

    private static void ConfigureStatusTypeVerplichteEigenschappenManyToMany(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StatusTypeVerplichteEigenschap>().HasKey(zi => new { zi.StatusTypeId, zi.EigenschapId });

        modelBuilder
            .Entity<StatusTypeVerplichteEigenschap>()
            .HasOne(zi => zi.StatusType)
            .WithMany(i => i.StatusTypeVerplichteEigenschappen)
            .HasForeignKey(zi => zi.StatusTypeId);

        modelBuilder
            .Entity<StatusTypeVerplichteEigenschap>()
            .HasOne(zi => zi.Eigenschap)
            .WithMany(z => z.StatusTypeVerplichtEigenschappen)
            .HasForeignKey(zi => zi.EigenschapId);
    }
}
