using Newtonsoft.Json;
using Roxit.ZGW.Common.JsonConverters;

namespace Roxit.ZGW.Common;

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
