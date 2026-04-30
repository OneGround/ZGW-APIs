using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace OneGround.ZGW.DataAccess.Encryption;

public class VersionedHmacSha256Hasher : IVersionedHmacHasher
{
    private readonly Dictionary<string, byte[]> _keys;
    private readonly string _latest;

    public VersionedHmacSha256Hasher(IOptions<HmacHasherConfiguration> options)
    {
        var config = options.Value;
        _keys = new Dictionary<string, byte[]>();

        string? legacyLatest = null;

        if (config.HmacKeys != null && config.HmacKeys.Count > 0)
        {
            foreach (var (version, keyBase64) in config.HmacKeys)
            {
                ValidateAndAddKey(version, keyBase64);
            }
        }
        else if (!string.IsNullOrEmpty(config.HmacKey))
        {
            ValidateAndAddKey("v1", config.HmacKey);
            legacyLatest = "v1";
        }

        if (_keys.Count == 0)
        {
            throw new InvalidOperationException(
                "No HMAC keys configured. Provide HmacKey_v1/HmacKey_v2/etc. or a legacy HmacKey in the configuration section."
            );
        }

        if (legacyLatest != null)
        {
            _latest = legacyLatest;
        }
        else if (!string.IsNullOrEmpty(config.Latest))
        {
            if (!_keys.ContainsKey(config.Latest))
            {
                throw new InvalidOperationException($"Latest key version '{config.Latest}' is not present in configured keys.");
            }
            _latest = config.Latest;
        }
        else
        {
            throw new InvalidOperationException(
                "HMAC 'Latest' key version must be explicitly configured when multiple keys exist. "
                    + "Set 'Latest' in the HmacHasher configuration section, or use a single legacy 'HmacKey' entry."
            );
        }
    }

    private void ValidateAndAddKey(string version, string keyBase64)
    {
        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(keyBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"Key version '{version}' is not a valid Base64-encoded string.", ex);
        }

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                $"Key version '{version}' must decode to at least 32 bytes (got {keyBytes.Length}). Generate a key with: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))."
            );
        }

        _keys[version] = keyBytes;
    }

    public string Latest => _latest;

    public string ComputeHash(string plaintext, string keyVersion)
    {
        if (!_keys.TryGetValue(keyVersion, out var key))
        {
            throw new KeyNotFoundException($"Key version '{keyVersion}' not found in configured keys.");
        }

        var inputBytes = Encoding.UTF8.GetBytes(plaintext);

        using var hmac = new HMACSHA256(key);
        var hashBytes = hmac.ComputeHash(inputBytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public string ComputeHash(string plaintext)
    {
        return ComputeHash(plaintext, _latest);
    }

    public IReadOnlyDictionary<string, string> ComputeAllHashes(string plaintext)
    {
        var result = new Dictionary<string, string>();

        foreach (var version in _keys.Keys)
        {
            result[version] = ComputeHash(plaintext, version);
        }

        return result;
    }
}
