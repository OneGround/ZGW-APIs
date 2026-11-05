using Microsoft.Extensions.Configuration;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Gesprekken.DataModel;

public class GrcDbContextFactory : BaseDbContextFactory<GrcDbContext>
{
    public GrcDbContextFactory(IConfiguration configuration)
        : base(configuration) { }

    public GrcDbContextFactory()
        : base() { }

    public override GrcDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = CreateDbContextOptionsBuilder();
        return new GrcDbContext(optionsBuilder.Options);
    }
}
