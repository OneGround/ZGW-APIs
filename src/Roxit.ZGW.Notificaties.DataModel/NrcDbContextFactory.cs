using Microsoft.Extensions.Configuration;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Notificaties.DataModel;

public class NrcDbContextFactory : BaseDbContextFactory<NrcDbContext>
{
    public NrcDbContextFactory(IConfiguration configuration)
        : base(configuration) { }

    public NrcDbContextFactory()
        : base() { }

    public override NrcDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = CreateDbContextOptionsBuilder();
        return new NrcDbContext(optionsBuilder.Options);
    }
}
