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
    }
}
