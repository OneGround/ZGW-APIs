using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OneGround.ZGW.Common.Web.Expands;

public static class JObjectHelper
{
    public static JObject FromObjectOrDefault(object @object, JsonSerializer configuredSerializer = null)
    {
        if (@object == null)
        {
            return default(JObject);
        }

        return configuredSerializer == null ? JObject.FromObject(@object) : JObject.FromObject(@object, configuredSerializer);
    }
}
