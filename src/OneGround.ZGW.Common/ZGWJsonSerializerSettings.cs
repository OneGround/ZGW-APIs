using Newtonsoft.Json;
using OneGround.ZGW.Common.JsonConverters;

namespace OneGround.ZGW.Common;

/// <summary>
/// Implements JsonSerializerSettings which is compatible with NetTopologySuite.Geometry type.
/// </summary>
public class ZGWJsonSerializerSettings : JsonSerializerSettings
{
    public ZGWJsonSerializerSettings()
    {
        Converters.Add(new DateOnlyJsonConverter());
        Converters.Add(new NullableDateOnlyJsonConverter());
        Converters.Add(new GeometryJsonConverter());
    }
}
