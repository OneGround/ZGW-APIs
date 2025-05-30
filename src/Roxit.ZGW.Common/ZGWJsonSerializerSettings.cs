using Newtonsoft.Json;
using Roxit.ZGW.Common.JsonConverters;

namespace Roxit.ZGW.Common;

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
