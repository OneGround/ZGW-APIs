using Microsoft.AspNetCore.DataProtection;
using OneGround.ZGW.Zaken.DataModel.Encryption;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.Encryption;

public class ZakenDatabaseProtectorTests
{
    private static ZakenDatabaseProtector CreateProtector()
    {
        var provider = new EphemeralDataProtectionProvider();
        return new ZakenDatabaseProtector(provider);
    }

    [Fact]
    public void Protect_ThenUnprotect_ReturnsOriginalValue()
    {
        var protector = CreateProtector();
        const string plaintext = "123456789";

        var ciphertext = protector.Protect(plaintext);
        var result = protector.Unprotect(ciphertext);

        Assert.Equal(plaintext, result);
    }

    [Fact]
    public void Protect_ReturnsDifferentValueEachTime()
    {
        var protector = CreateProtector();
        const string plaintext = "123456789";

        var ciphertext1 = protector.Protect(plaintext);
        var ciphertext2 = protector.Protect(plaintext);

        Assert.NotEqual(ciphertext1, ciphertext2);
    }
}
