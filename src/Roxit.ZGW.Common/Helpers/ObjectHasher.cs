using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Roxit.ZGW.Common.Helpers;

public static class ObjectHasher
{
    public static string ComputeSha1Hash(object obj)
    {
        // Serialize the object to JSON
        var json = JsonConvert.SerializeObject(obj);

        // Convert to bytes
        var bytes = Encoding.UTF8.GetBytes(json);

        // Compute SHA-1 hash
        var hashBytes = SHA1.HashData(bytes);

        // Convert to hex string (the cha1sum like: "57562e54e5c4f834cc9d672cc290bea105be6326")
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
