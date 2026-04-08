using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Common.DataProtection.DataModel;

public class DataProtectionKeyDbContextFactory : BaseDbContextFactory<DataProtectionKeyDbContext>
{
    public DataProtectionKeyDbContextFactory(IConfiguration configuration)
        : base(configuration, "DataProtectionConnectionString") { }

    public DataProtectionKeyDbContextFactory()
        : base("DataProtectionConnectionString") { }

    public override DataProtectionKeyDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DataProtectionKeyDbContext>();
        optionsBuilder.UseNpgsql(ConnectionString, o => o.MigrationsHistoryTable("__EFMigrationsHistory", "data_protection"));
        return new DataProtectionKeyDbContext(optionsBuilder.Options);
    }
}
