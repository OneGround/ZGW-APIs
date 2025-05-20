using Microsoft.Extensions.Configuration;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Catalogi.DataModel;

public class ZtcDbContextFactory : BaseDbContextFactory<ZtcDbContext>
{
    public ZtcDbContextFactory(IConfiguration configuration)
        : base(configuration) { }

    public ZtcDbContextFactory()
        : base() { }

    public override ZtcDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = CreateDbContextOptionsBuilder();
        return new ZtcDbContext(optionsBuilder.Options);
    }
}
