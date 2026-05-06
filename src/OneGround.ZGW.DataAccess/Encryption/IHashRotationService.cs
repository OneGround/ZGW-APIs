using System.Collections.Generic;

namespace OneGround.ZGW.DataAccess.Encryption;

/// <summary>
/// Service that provides hash rotation support for key versioning scenarios.
/// </summary>
public interface IHashRotationService
{
    /// <summary>
    /// Computes an HMAC hash of <paramref name="plaintext"/> using the latest configured key
    /// and returns both the hash value and the key version used.
    /// </summary>
    /// <param name="plaintext">The plaintext value to hash.</param>
    /// <returns>A <see cref="HashResult"/> containing the computed hash and the key version.</returns>
    HashResult CreateLatestHash(string plaintext);

    /// <summary>
    /// Computes HMAC hashes of <paramref name="plaintext"/> using all configured keys.
    /// Used for multi-key search during a key rotation window, where data may have been
    /// hashed with any previously active key.
    /// </summary>
    /// <param name="plaintext">The plaintext value to hash.</param>
    /// <returns>A list of hash values, one per configured key version.</returns>
    List<string> GetAllPossibleHashes(string plaintext);
}
