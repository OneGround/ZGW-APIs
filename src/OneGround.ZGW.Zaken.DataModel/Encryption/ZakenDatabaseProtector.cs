using Microsoft.AspNetCore.DataProtection;

namespace OneGround.ZGW.Zaken.DataModel.Encryption;

public class ZakenDatabaseProtector : IDatabaseProtector
{
    private const string DataProtectionPurpose = "ZakenDatabaseProtection";

    private readonly IDataProtector _protector;

    public ZakenDatabaseProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(DataProtectionPurpose);
    }

    public string Protect(string plaintext)
    {
        return _protector.Protect(plaintext);
    }

    public string Unprotect(string ciphertext)
    {
        return _protector.Unprotect(ciphertext);
    }
}
