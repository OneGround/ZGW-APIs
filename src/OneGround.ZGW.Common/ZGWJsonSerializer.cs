using Newtonsoft.Json;
using OneGround.ZGW.Common.JsonConverters;

namespace OneGround.ZGW.Common;

/// <summary>
/// Implements JsonSerializer which is compatible with NetTopologySuite.Geometry type.
/// </summary>
public sealed class ZGWJsonSerializer : JsonSerializer
{
    public ZGWJsonSerializer()
    {
        Converters.Add(new DateOnlyJsonConverter());
        Converters.Add(new NullableDateOnlyJsonConverter());
        Converters.Add(new GeometryJsonConverter());
    }
}
