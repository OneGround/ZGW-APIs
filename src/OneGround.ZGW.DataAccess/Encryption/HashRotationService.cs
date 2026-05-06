using System.Collections.Generic;
using System.Linq;

namespace OneGround.ZGW.DataAccess.Encryption;

/// <summary>
/// Adapter over <see cref="IVersionedHmacHasher"/> that provides hash rotation support.
/// All hashing logic is delegated to the underlying hasher — this class adds no crypto logic.
/// </summary>
public class HashRotationService : IHashRotationService
{
    private readonly IVersionedHmacHasher _hasher;

    public HashRotationService(IVersionedHmacHasher hasher)
    {
        _hasher = hasher;
    }

    /// <inheritdoc />
    public HashResult CreateLatestHash(string plaintext)
    {
        var hash = _hasher.ComputeHash(plaintext);
        return new HashResult(hash, _hasher.Latest);
    }

    /// <inheritdoc />
    public List<string> GetAllPossibleHashes(string plaintext)
    {
        return _hasher.ComputeAllHashes(plaintext).Values.ToList();
    }
}
