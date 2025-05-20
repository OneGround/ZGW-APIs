using Microsoft.Extensions.Configuration;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Besluiten.DataModel;

public class BrcDbContextFactory : BaseDbContextFactory<BrcDbContext>
{
    public BrcDbContextFactory(IConfiguration configuration)
        : base(configuration) { }

    public BrcDbContextFactory()
        : base() { }

    public override BrcDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = CreateDbContextOptionsBuilder();
        return new BrcDbContext(optionsBuilder.Options);
    }
}
