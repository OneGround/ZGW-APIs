using Microsoft.Extensions.Configuration;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Zaken.DataModel;

public class DataProtectionKeyDbContextFactory : BaseDbContextFactory<DataProtectionKeyDbContext>
{
    public DataProtectionKeyDbContextFactory(IConfiguration configuration)
        : base(configuration) { }

    public DataProtectionKeyDbContextFactory()
        : base() { }

    public override DataProtectionKeyDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = CreateDbContextOptionsBuilder();
        return new DataProtectionKeyDbContext(optionsBuilder.Options);
    }
}
