namespace OneGround.ZGW.DataAccess.Encryption;

/// <summary>
/// Represents the result of an HMAC hash computation, including the hash value and the key version used.
/// </summary>
public record HashResult(string Hash, string Version);
