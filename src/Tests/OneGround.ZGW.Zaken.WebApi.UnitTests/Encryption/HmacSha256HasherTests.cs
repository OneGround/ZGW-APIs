using System;
using Microsoft.Extensions.Options;
using OneGround.ZGW.DataAccess.Encryption;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.Encryption;

public class HmacSha256HasherTests
{
    private static HmacSha256Hasher CreateHasher(string hmacKey = null)
    {
        hmacKey ??= Convert.ToBase64String(new byte[32]);
        var options = Options.Create(new HmacHasherConfiguration { HmacKey = hmacKey });
        return new HmacSha256Hasher(options);
    }

    [Fact]
    public void ComputeHash_SameInput_ReturnsSameHash()
    {
        var hasher = CreateHasher();

        var hash1 = hasher.ComputeHash("123456789");
        var hash2 = hasher.ComputeHash("123456789");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DifferentInput_ReturnsDifferentHash()
    {
        var hasher = CreateHasher();

        var hash1 = hasher.ComputeHash("123456789");
        var hash2 = hasher.ComputeHash("987654321");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_Returns64CharHexString()
    {
        var hasher = CreateHasher();

        var hash = hasher.ComputeHash("123456789");

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_NullOrEmptyHmacKey_ThrowsInvalidOperationException(string hmacKey)
    {
        var options = Options.Create(new HmacHasherConfiguration { HmacKey = hmacKey });

        Assert.Throws<InvalidOperationException>(() => new HmacSha256Hasher(options));
    }

    [Fact]
    public void Constructor_KeyShorterThan32Bytes_ThrowsInvalidOperationException()
    {
        // A Base64-encoded key that decodes to only 16 bytes (below the 32-byte minimum)
        var shortKey = Convert.ToBase64String(new byte[16]);
        var options = Options.Create(new HmacHasherConfiguration { HmacKey = shortKey });

        Assert.Throws<InvalidOperationException>(() => new HmacSha256Hasher(options));
    }
}
