using Microsoft.EntityFrameworkCore;
using OneGround.ZGW.Autorisaties.DataModel;

namespace OneGround.ZGW.Autorisaties.WebApi.UnitTests;

public class UnitTestAcDbContext : AcDbContext
{
    public UnitTestAcDbContext(DbContextOptions<AcDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // in-memory database context does not support array of primitive types,
        // so we ignore these right here for unit tests
        modelBuilder.Entity<Autorisatie>().Ignore(a => a.Scopes);
        modelBuilder.Entity<FutureAutorisatie>().Ignore(a => a.Scopes);
    }
}
