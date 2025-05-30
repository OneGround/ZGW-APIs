using Microsoft.Extensions.Configuration;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Autorisaties.DataModel;

public class AcDbContextFactory : BaseDbContextFactory<AcDbContext>
{
    public AcDbContextFactory(IConfiguration configuration)
        : base(configuration) { }

    public AcDbContextFactory()
        : base() { }

    public override AcDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = CreateDbContextOptionsBuilder();
        return new AcDbContext(optionsBuilder.Options);
    }
}
