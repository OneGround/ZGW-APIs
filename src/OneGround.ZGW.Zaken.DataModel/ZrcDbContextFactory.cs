using Microsoft.Extensions.Configuration;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel;

public class ZrcDbContextFactory : BaseDbContextFactory<ZrcDbContext>
{
    public ZrcDbContextFactory(IConfiguration configuration)
        : base(configuration) { }

    public ZrcDbContextFactory()
        : base() { }

    public override ZrcDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = CreateDbContextOptionsBuilder();
        return new ZrcDbContext(optionsBuilder.Options);
    }
}
