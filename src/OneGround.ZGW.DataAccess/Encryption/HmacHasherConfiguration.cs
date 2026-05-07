using System.Collections.Generic;

namespace OneGround.ZGW.DataAccess.Encryption;

public class HmacHasherConfiguration
{
    /// <summary>
    /// The latest key version for new writes (e.g., "v1", "v2").
    /// Required when multiple keys are configured. If null/empty and only a legacy HmacKey is provided, defaults to "v1".
    /// </summary>
    public string Latest { get; set; }

    /// <summary>
    /// Legacy single key fallback. Treated as version "v1" when no HmacKeys dictionary entries are configured.
    /// Base64-encoded HMAC key (min 32 bytes when decoded).
    /// </summary>
    public string HmacKey { get; set; }

    /// <summary>
    /// Versioned HMAC keys for key rotation. Maps version string (e.g., "v1", "v2") to Base64-encoded key.
    /// Example JSON: "HmacKeys": { "v1": "base64key1==", "v2": "base64key2==" }
    /// When provided, these take precedence over the legacy HmacKey property.
    /// </summary>
    public Dictionary<string, string> HmacKeys { get; set; }
}
