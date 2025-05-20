using Microsoft.EntityFrameworkCore;
using OneGround.ZGW.Catalogi.DataModel;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests;

class UnitTestZtcDbContext : ZtcDbContext
{
    public UnitTestZtcDbContext(DbContextOptions<ZtcDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ignoring NodaTime
        modelBuilder.Entity<BronDatumArchiefProcedure>().Ignore(z => z.ProcesTermijn);

        modelBuilder.Entity<ResultaatType>().Ignore(z => z.ArchiefActieTermijn);

        modelBuilder.Entity<BesluitType>().Ignore(z => z.ReactieTermijn).Ignore(z => z.PublicatieTermijn);

        modelBuilder.Entity<ZaakType>().Ignore(z => z.VerlengingsTermijn).Ignore(z => z.Servicenorm).Ignore(z => z.Doorlooptijd);

        // in-memory database context does not support array of primitive types,
        // so we ignore these right here for unit tests
        modelBuilder.Entity<EigenschapSpecificatie>().Ignore(s => s.Waardenverzameling);

        modelBuilder
            .Entity<ZaakType>()
            .Ignore(z => z.Trefwoorden)
            .Ignore(z => z.ProductenOfDiensten)
            .Ignore(z => z.Verantwoordingsrelatie)
            .Ignore(z => z.BronCatalogus)
            .Ignore(z => z.BronZaaktype);

        modelBuilder.Entity<InformatieObjectType>().Ignore(z => z.Trefwoord).Ignore(z => z.OmschrijvingGeneriek);

        modelBuilder.Entity<StatusType>().Ignore(z => z.Doorlooptijd).Ignore(z => z.CheckListItemStatustypes);

        modelBuilder.Entity<ResultaatType>().Ignore(z => z.ProcesTermijn);
    }
}
