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
