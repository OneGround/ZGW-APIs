using System.IO;
using Newtonsoft.Json;

namespace OneGround.ZGW.Common.Helpers;

public static class JsonSerializationHelper
{
    public static T ReadAndDeserializeJsonFromFileOrDefault<T>(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return default;
        }

        var data = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<T>(data, new ZGWJsonSerializerSettings());
    }
}
