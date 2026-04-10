using Microsoft.AspNetCore.DataProtection;

namespace OneGround.ZGW.DataAccess.Encryption;

public class DatabaseProtector<TContext> : IDatabaseProtector<TContext>
    where TContext : class
{
    private readonly IDataProtector _protector;

    public DatabaseProtector(IDataProtectionProvider provider, string purpose)
    {
        _protector = provider.CreateProtector(purpose);
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
}
