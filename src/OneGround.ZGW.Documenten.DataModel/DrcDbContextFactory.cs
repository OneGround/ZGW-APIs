using Microsoft.Extensions.Configuration;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Documenten.DataModel;

public class DrcDbContextFactory : BaseDbContextFactory<DrcDbContext>
{
    public DrcDbContextFactory(IConfiguration configuration)
        : base(configuration) { }

    public DrcDbContextFactory()
        : base() { }

    public override DrcDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = CreateDbContextOptionsBuilder();
        return new DrcDbContext(optionsBuilder.Options);
    }
}

public class DrcDbContextFactory2 : BaseDbContextFactory<DrcDbContext2>
{
    public DrcDbContextFactory2(IConfiguration configuration)
        : base(configuration) { }

    public DrcDbContextFactory2()
        : base() { }

    public override DrcDbContext2 CreateDbContext(string[] args)
    {
        var optionsBuilder = CreateDbContextOptionsBuilder();
        return new DrcDbContext2(optionsBuilder.Options);
    }
}
