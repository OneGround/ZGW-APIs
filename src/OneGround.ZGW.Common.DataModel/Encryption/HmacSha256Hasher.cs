using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace OneGround.ZGW.Common.DataModel.Encryption;

public class HmacSha256Hasher : IHmacHasher
{
    private readonly byte[] _key;

    public HmacSha256Hasher(IOptions<HmacHasherConfiguration> options)
    {
        var config = options.Value;

        if (string.IsNullOrEmpty(config.HmacKey))
            throw new InvalidOperationException(
                $"{nameof(HmacHasherConfiguration)}.{nameof(HmacHasherConfiguration.HmacKey)} must be a non-empty Base64-encoded HMAC key."
            );

        _key = Convert.FromBase64String(config.HmacKey);

        if (_key.Length < 32)
            throw new InvalidOperationException(
                $"{nameof(HmacHasherConfiguration)}.{nameof(HmacHasherConfiguration.HmacKey)} must decode to at least 32 bytes (got {_key.Length}). Generate a key with: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))."
            );
    }

    public string ComputeHash(string plaintext)
    {
        var inputBytes = Encoding.UTF8.GetBytes(plaintext);

        using var hmac = new HMACSHA256(_key);
        var hashBytes = hmac.ComputeHash(inputBytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
