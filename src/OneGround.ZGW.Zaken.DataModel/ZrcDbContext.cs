using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.DataAccess.AuditTrail;
using OneGround.ZGW.DataAccess.Migrations;
using OneGround.ZGW.DataAccess.NumberGenerator;
using OneGround.ZGW.Zaken.DataModel.Authorization;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;

namespace OneGround.ZGW.Zaken.DataModel;

public partial class ZrcDbContext : BaseDbContext, IDbContextWithAuditTrail, IDataMigrationsDbContext, IDbContextWithNummerGenerator
{
    public ZrcDbContext(DbContextOptions<ZrcDbContext> options, IDbUserContext dbUserContext = null)
        : base(options, dbUserContext) { }

    public DbSet<FinishedDataMigration> FinishedDataMigrations { get; set; }

    public DbSet<Zaak> Zaken { get; set; }
    public DbSet<AuditTrailRegel> AuditTrailRegels { get; set; }
    public DbSet<ZaakObject.ZaakObject> ZaakObjecten { get; set; }
    public DbSet<RelevanteAndereZaak> RelevanteAndereZaken { get; set; }
    public DbSet<ZaakKenmerk> ZaakKenmerken { get; set; }
    public DbSet<ZaakEigenschap> ZaakEigenschappen { get; set; }
    public DbSet<ZaakBesluit> ZaakBesluiten { get; set; }
    public DbSet<ZaakStatus> ZaakStatussen { get; set; }
    public DbSet<ZaakInformatieObject> ZaakInformatieObjecten { get; set; }
    public DbSet<ZaakRol.ZaakRol> ZaakRollen { get; set; }
    public DbSet<ZaakResultaat> ZaakResultaten { get; set; }
    public DbSet<KlantContact> KlantContacten { get; set; } // Deprecated in >= v1.5
    public DbSet<ZaakContactmoment> ZaakContactmomenten { get; set; }
    public DbSet<ZaakVerzoek> ZaakVerzoeken { get; set; }
    public DbSet<OrganisatieNummer> OrganisatieNummers { get; set; }

    public DbSet<NotAnEntity> WithoutEntity { get; set; }
    public DbSet<TempZaakAuthorization> TempZaakAuthorization { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder
            .Entity<TempZaakAuthorization>()
            .ToTable(nameof(Authorization.TempZaakAuthorization), t => t.ExcludeFromMigrations())
            .ToView(nameof(Authorization.TempZaakAuthorization))
            .HasKey(x => x.ZaakType);

        modelBuilder.Entity<ZaakInformatieObject>().HasIndex(p => p.InformatieObject);

        modelBuilder.Entity<ZaakInformatieObject>().HasIndex(p => p.Owner);

        modelBuilder.Entity<ZaakObject.ZaakObject>().HasIndex(p => p.Object);

        modelBuilder.Entity<ZaakObject.ZaakObject>().HasIndex(p => p.ObjectType);

        modelBuilder.Entity<ZaakObject.ZaakObject>().HasIndex(p => p.Owner);

        modelBuilder.Entity<ZaakRol.ZaakRol>().HasIndex(p => p.Betrokkene);

        modelBuilder.Entity<ZaakRol.ZaakRol>().HasIndex(p => p.BetrokkeneType);

        modelBuilder.Entity<ZaakRol.ZaakRol>().HasIndex(p => p.Omschrijving);

        modelBuilder.Entity<ZaakRol.ZaakRol>().HasIndex(p => p.OmschrijvingGeneriek);

        modelBuilder.Entity<ZaakRol.ZaakRol>().HasIndex(p => p.RolType);

        modelBuilder.Entity<ZaakRol.ZaakRol>().HasIndex(p => p.Owner);

        modelBuilder.Entity<NatuurlijkPersoonZaakRol>().HasIndex(p => p.InpBsn);

        modelBuilder.Entity<NatuurlijkPersoonZaakRol>().HasIndex(p => p.AnpIdentificatie);

        modelBuilder.Entity<NatuurlijkPersoonZaakRol>().HasIndex(p => p.InpANummer);

        modelBuilder.Entity<NietNatuurlijkPersoonZaakRol>().HasIndex(p => p.AnnIdentificatie);

        modelBuilder.Entity<NietNatuurlijkPersoonZaakRol>().HasIndex(p => p.InnNnpId);

        modelBuilder.Entity<MedewerkerZaakRol>().HasIndex(p => p.Identificatie);

        modelBuilder.Entity<OrganisatorischeEenheidZaakRol>().HasIndex(p => p.Identificatie);

        modelBuilder.Entity<VestigingZaakRol>().HasIndex(p => p.VestigingsNummer);

        modelBuilder.Entity<ZaakStatus>().HasIndex(p => p.StatusType);

        modelBuilder.Entity<ZaakStatus>().HasIndex(p => p.Owner);

        modelBuilder.Entity<ZaakEigenschap>().HasIndex(p => p.Owner);

        modelBuilder.Entity<ZaakContactmoment>().HasIndex(p => p.Owner);

        modelBuilder.Entity<ZaakVerzoek>().HasIndex(p => p.Owner);

        modelBuilder.Entity<Zaak>().Property(p => p.Archiefstatus).HasDefaultValue(ArchiefStatus.nog_te_archiveren);

        modelBuilder.Entity<Zaak>().Property(p => p.BetalingsIndicatie).HasDefaultValue(BetalingsIndicatie.nvt);

        modelBuilder.Entity<Zaak>().Property(p => p.VertrouwelijkheidAanduiding).HasDefaultValue(VertrouwelijkheidAanduiding.openbaar);

        modelBuilder.Entity<Zaak>().HasIndex(p => p.Archiefactiedatum);

        modelBuilder.Entity<Zaak>().HasIndex(p => p.Archiefnominatie);

        modelBuilder.Entity<Zaak>().HasIndex(p => p.Archiefstatus);

        modelBuilder.Entity<Zaak>().HasIndex(p => new { p.Bronorganisatie, p.Identificatie }).IsUnique();

        modelBuilder.Entity<Zaak>().HasIndex(p => p.Archiefstatus);

        modelBuilder.Entity<Zaak>().HasIndex(p => new { p.Id, p.HoofdzaakId });

        modelBuilder.Entity<Zaak>().HasIndex(p => p.Zaaktype);

        modelBuilder.Entity<Zaak>().HasIndex(p => p.Zaakgeometrie).HasMethod("gist");

        modelBuilder.Entity<Zaak>().HasIndex(p => p.Owner);

        modelBuilder.Entity<Zaak>().HasIndex(p => p.Identificatie);

        modelBuilder.Entity<Zaak>().HasIndex(p => p.Startdatum);

        //modelBuilder.Entity<Zaak>()
        //    .HasIndex(p => p.Einddatum);

        //modelBuilder.Entity<Zaak>()
        //    .HasIndex(p => p.Registratiedatum);

        //modelBuilder.Entity<Zaak>()
        //    .HasIndex(p => p.EinddatumGepland);

        //modelBuilder.Entity<Zaak>()
        //    .HasIndex(p => p.UiterlijkeEinddatumAfdoening);

        //modelBuilder.Entity<Zaak>()
        //    .HasIndex(p => p.VertrouwelijkheidAanduiding);
        modelBuilder.Entity<Zaak>().HasIndex(p => new { p.Owner, p.Identificatie });

        modelBuilder.Entity<ZaakResultaat>().HasIndex(p => p.ResultaatType);

        modelBuilder.Entity<ZaakResultaat>().HasIndex(p => p.Owner);

        modelBuilder.Entity<AuditTrailRegel>().HasIndex(p => p.HoofdObjectId);

        modelBuilder.Entity<ZaakContactmoment>().HasIndex(p => p.Contactmoment);

        modelBuilder.Entity<ZaakVerzoek>().HasIndex(p => p.Verzoek);

        // Note: To get ST_Transform working to do a tranfomation from one Geometry to another Geometry (without using real tables)
        modelBuilder.Entity<NotAnEntity>(x =>
        {
            x.HasNoKey();
            x.ToView(null); // Does not create table via migrations
            x.ToSqlQuery("SELECT 1"); // Does not require .FromSql when using the "entity"
        });

        // Source: https://www.npgsql.org/efcore/misc/collations-and-case-sensitivity.html?tabs=data-annotations
        // And:    https://dba.stackexchange.com/questions/255780/case-insensitive-collation-still-comparing-case-sensitive
        // And:    https://stackoverflow.com/questions/70739480/change-postgres-to-case-insensitive
        modelBuilder.HasCollation("ci_collation", locale: "@colStrength=secondary", provider: "icu", deterministic: false);

        modelBuilder.Entity<Zaak>().Property(c => c.Zaaktype).UseCollation("ci_collation");

        modelBuilder.Entity<ZaakObject.ZaakObject>().Property(c => c.Object).UseCollation("ci_collation");

        modelBuilder.Entity<ZaakInformatieObject>().Property(c => c.InformatieObject).UseCollation("ci_collation");

        modelBuilder.Entity<ZaakContactmoment>().Property(c => c.Contactmoment).UseCollation("ci_collation");

        modelBuilder.Entity<ZaakRol.ZaakRol>().Property(c => c.Betrokkene).UseCollation("ci_collation");

        modelBuilder.Entity<ZaakRol.ZaakRol>().Property(c => c.RolType).UseCollation("ci_collation");

        modelBuilder.Entity<ZaakResultaat>().Property(c => c.ResultaatType).UseCollation("ci_collation");

        modelBuilder.Entity<ZaakStatus>().Property(c => c.StatusType).UseCollation("ci_collation");

        modelBuilder.Entity<ZaakVerzoek>().Property(c => c.Verzoek).UseCollation("ci_collation");
    }

    // Note: To get ST_Transform working to do a tranfomation from one Geometry to another Geometry (without using real tables)
    public async Task<Geometry> ST_TransformAsync(Geometry geometry, int srid, CancellationToken cancellationToken = default)
    {
        return await WithoutEntity.Select(_ => ST_Transform(geometry, srid)).FirstAsync(cancellationToken);
    }

    [DbFunction("ST_Transform", IsBuiltIn = true)]
    public virtual Geometry ST_Transform(Geometry geom, int srid) => throw new NotSupportedException();
}
