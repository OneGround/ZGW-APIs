using System;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using Roxit.ZGW.Common.Exceptions;
using Serilog;

namespace Roxit.ZGW.Common.JsonConverters;

public class GeometryJsonConverter : JsonConverter
{
    private readonly JsonSerializer _geoJsonSerializer;

    public GeometryJsonConverter()
    {
        var geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        _geoJsonSerializer = GeoJsonSerializer.Create(geometryFactory);
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType.Namespace == "NetTopologySuite.Geometries";
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        try
        {
            return _geoJsonSerializer.Deserialize(reader, objectType);
        }
        catch (JsonReaderException ex)
        {
            Log.Error("Error deserializing JSON: " + ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            Log.Error("Unexpected error during JSON deserialization: " + ex.Message);
            throw new JsonSerializationException("Unexpected error occurred during deserialization.");
        }
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        try
        {
            _geoJsonSerializer.Serialize(writer, value);
        }
        catch (Exception ex)
        {
            throw new InvalidGeometryException(writer.Path, ex.Message);
        }
    }
}
