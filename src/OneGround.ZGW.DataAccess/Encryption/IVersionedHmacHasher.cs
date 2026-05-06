using System.Collections.Generic;

namespace OneGround.ZGW.DataAccess.Encryption;

/// <summary>
/// HMAC hasher with versioned key support for key rotation scenarios.
/// Uses string-based versions (e.g., "v1", "v2") to support naming convention configuration.
/// </summary>
public interface IVersionedHmacHasher
{
    /// <summary>
    /// Computes an HMAC-SHA256 hash of <paramref name="plaintext"/> using the latest key version.
    /// </summary>
    string ComputeHash(string plaintext);

    /// <summary>
    /// Computes an HMAC-SHA256 hash of <paramref name="plaintext"/> using the key
    /// associated with <paramref name="keyVersion"/>.
    /// </summary>
    string ComputeHash(string plaintext, string keyVersion);

    /// <summary>
    /// The latest key version that is currently active for new writes (e.g., "v1", "v2").
    /// </summary>
    string Latest { get; }

    /// <summary>
    /// Computes HMAC-SHA256 hashes for <paramref name="plaintext"/> using every
    /// configured key version. Used for multi-version search during key rotation.
    /// </summary>
    /// <returns>
    /// A dictionary mapping each configured key version to its corresponding hash.
    /// Example: { "v1": "abc123...", "v2": "def456..." }
    /// </returns>
    IReadOnlyDictionary<string, string> ComputeAllHashes(string plaintext);
}
