using Newtonsoft.Json.Linq;

namespace OneGround.ZGW.Common.Web.Expands;

public static class JObjectExtensions
{
    public static T Get<T>(this JObject jObject, string key, T defaultValue = default)
        where T : struct
    {
        return jObject[key]?.ToObject<T>() ?? defaultValue;
    }

    public static bool TryGet<T>(this JObject jObject, string key, out T value)
        where T : struct
    {
        value = default;

        var valueFromJObject = jObject[key]?.ToObject<T>();
        if (valueFromJObject == null)
            return false;

        value = valueFromJObject.Value;

        return true;
    }
}
