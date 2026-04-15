using Microsoft.Extensions.Configuration;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.DataAccess.Encryption;

namespace OneGround.ZGW.Zaken.DataModel;

public class ZrcDbContextFactory : BaseDbContextFactory<ZrcDbContext>
{
    private readonly IDatabaseProtector<ZrcDbContext> _databaseProtector;
    private readonly IHmacHasher _hmacHasher;

    public ZrcDbContextFactory(IConfiguration configuration, IDatabaseProtector<ZrcDbContext> databaseProtector, IHmacHasher hmacHasher)
        : base(configuration)
    {
        _databaseProtector = databaseProtector;
        _hmacHasher = hmacHasher;
    }

    public ZrcDbContextFactory()
        : base()
    {
        _databaseProtector = new DesignTimeDatabaseProtector();
        _hmacHasher = new DesignTimeHmacHasher();
    }

    private sealed class DesignTimeDatabaseProtector : IDatabaseProtector<ZrcDbContext>
    {
        public string Protect(string plaintext) => plaintext;

        public string Unprotect(string ciphertext) => ciphertext;
    }

    private sealed class DesignTimeHmacHasher : IHmacHasher
    {
        public string ComputeHash(string plaintext) => plaintext;
    }

    public override ZrcDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = CreateDbContextOptionsBuilder();
        return new ZrcDbContext(optionsBuilder.Options, _databaseProtector, _hmacHasher);
    }
}
