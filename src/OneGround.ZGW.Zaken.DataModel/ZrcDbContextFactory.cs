using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.DataAccess.Encryption;

namespace OneGround.ZGW.Zaken.DataModel;

public class ZrcDbContextFactory : BaseDbContextFactory<ZrcDbContext>
{
    private readonly IDatabaseProtector<ZrcDbContext> _databaseProtector;
    private readonly IVersionedHmacHasher _versionedHasher;

    public ZrcDbContextFactory(IConfiguration configuration, IDatabaseProtector<ZrcDbContext> databaseProtector, IVersionedHmacHasher versionedHasher)
        : base(configuration)
    {
        _databaseProtector = databaseProtector;
        _versionedHasher = versionedHasher;
    }

    public ZrcDbContextFactory()
        : base()
    {
        _databaseProtector = new DesignTimeDatabaseProtector();
        _versionedHasher = new DesignTimeVersionedHmacHasher();
    }

    private sealed class DesignTimeDatabaseProtector : IDatabaseProtector<ZrcDbContext>
    {
        public string Protect(string plaintext) => plaintext;

        public string Unprotect(string ciphertext) => ciphertext;
    }

    private sealed class DesignTimeVersionedHmacHasher : IVersionedHmacHasher
    {
        public string ComputeHash(string plaintext) => plaintext;

        public string ComputeHash(string plaintext, string keyVersion) => plaintext;

        public string Latest => "v1";

        public IReadOnlyDictionary<string, string> ComputeAllHashes(string plaintext) => new Dictionary<string, string> { { "v1", plaintext } };
    }

    public override ZrcDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = CreateDbContextOptionsBuilder();
        return new ZrcDbContext(optionsBuilder.Options, _databaseProtector, _versionedHasher);
    }
}
