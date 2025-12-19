using Newtonsoft.Json.Linq;

namespace OneGround.ZGW.Zaken.Contracts.Helpers;

public static class JObjectExtensions
{
    public static bool TryGetObject<T>(this JObject jObject, string key, out T value)
        where T : class
    {
        value = jObject[key]?.ToObject<T>();

        return value != null;
    }
}
